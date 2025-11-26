using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine.Networking;
using UnityEngine.Audio;
using Random = UnityEngine.Random;

[RequireComponent(typeof(AudioSource))]
public class SpawnNote : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI countdownText;

    [Header("Game Start")]
    public float preGameCountdown = 3f;

    [Header("OSU Beatmap")]
    public TextAsset osuBeatmap;
    private string osuFilePath;
    public float extraOffsetSeconds = 0f;

    [Header("Spawn Settings")]
    public float travelDuration = 2.0f;
    public float noteSpeed = 1.0f;
    public float holdNoteSpeed = 0.4f;

    [Header("Phantom & Ghost Logic")]
    [Range(0f, 1f)] public float phantomChance = 0.3f;
    public float phantomSmoothness = 0.2f;
    [Range(0f, 1f)] public float ghostHoldChance = 0.4f;
    public float ghostFadeSpeed = 5.0f;

    [Header("Decoy (Rhythmic Gap Filler)")]
    public bool enableDecoys = true;
    [Tooltip("Seberapa dekat note decoy boleh muncul sebelum hit point (detik)")]
    public float decoyDespawnOffset = 0.3f;
    [Tooltip("Peluang decoy muncul di celah kosong (0.0 - 1.0)")]
    [Range(0f, 1f)] public float decoyDensity = 0.5f;

    [Header("Prefabs")]
    public GameObject normalNotePrefab;
    public GameObject phantomNotePrefab;
    public GameObject holdNotePrefab;
    public GameObject obstaclePrefab;

    [Header("Timing Circle")]
    public bool enableTimingCircle = true;
    public GameObject timingCirclePrefab;
    public Transform effectsParent;
    public float timingCircleStartScale = 2.0f;
    public float timingCircleEndScale = 1.0f;

    [Header("Lanes")]
    public Transform upSpawn, downSpawn, leftSpawn, rightSpawn;
    public Transform upTarget, downTarget, leftTarget, rightTarget;

    [Header("Audio Mixer")]
    public AudioMixerGroup musicGroup;
    public AudioMixerGroup sfxGroup;

    [HideInInspector] public double songStartDspTime;
    private AudioSource audioSource;
    private List<OsuBeatmapLoader.OsuNote> notes;
    private float audioLeadInSec;
    private bool isSongReady = false;
    public bool isGameOver = false;
    private static SpawnNote instance;
    private int normalNoteCounter = 0;

    void Start()
    {
        instance = this;
        audioSource = GetComponent<AudioSource>();

        // Hubungkan Mixer
        if (musicGroup != null)
        {
            audioSource.outputAudioMixerGroup = musicGroup;
        }

        // Load Session Data
        phantomChance = GameSession.SelectedPhantomChance;
        osuFilePath = GameSession.SelectedOsuFile;

        if (string.IsNullOrEmpty(osuFilePath) && PlayerPrefs.HasKey("SelectedOsuFile"))
        {
            osuFilePath = PlayerPrefs.GetString("SelectedOsuFile");
            GameSession.SelectedOsuFile = osuFilePath;
        }

        if (!string.IsNullOrEmpty(osuFilePath) && File.Exists(osuFilePath))
        {
            string osuText = File.ReadAllText(osuFilePath);
            osuBeatmap = new TextAsset(osuText);

            // 1. Load Chart & Data
            var chart = OsuBeatmapLoader.Load(osuBeatmap);
            audioLeadInSec = chart.audioLeadInSec;
            notes = chart.notes;

            // 2. Kirim Data Kiai (Reff) ke Kiai Manager
            var kiaiManager = FindFirstObjectByType<KiaiEffectManager>();
            if (kiaiManager != null)
            {
                kiaiManager.SetupTiming(chart.timingPoints);
            }

            // 3. Generate Decoys (Hanya di Mode Hard/Phantom tinggi)
            if (enableDecoys && phantomChance >= 0.9f)
            {
                InjectDecoysIntoGaps(chart.timingPoints, notes);
            }

            // 4. Load Audio
            string beatmapDir = Path.GetDirectoryName(osuFilePath);
            LoadAudioFromBeatmap(beatmapDir, osuText);
        }
    }

    // --- Logic Decoy Injection ---
    void InjectDecoysIntoGaps(List<OsuBeatmapLoader.TimingPoint> timingPoints, List<OsuBeatmapLoader.OsuNote> existingNotes)
    {
        if (timingPoints == null || timingPoints.Count == 0) return;

        HashSet<int> occupiedTimes = new HashSet<int>();
        foreach (var n in existingNotes)
        {
            int tMs = Mathf.RoundToInt(n.timeSec * 1000);
            for (int i = -50; i <= 50; i += 10) occupiedTimes.Add(tMs + i);
        }

        List<OsuBeatmapLoader.OsuNote> decoys = new List<OsuBeatmapLoader.OsuNote>();
        string[] dirs = { "up", "down", "left", "right" };

        for (int i = 0; i < timingPoints.Count; i++)
        {
            var tp = timingPoints[i];
            if (tp.beatLengthSec <= 0) continue;

            float endTime = existingNotes[existingNotes.Count - 1].timeSec + 2f;
            for (int j = i + 1; j < timingPoints.Count; j++)
            {
                if (timingPoints[j].beatLengthSec > 0) { endTime = timingPoints[j].timeSec; break; }
            }

            for (float t = tp.timeSec; t < endTime; t += tp.beatLengthSec)
            {
                int checkTimeMs = Mathf.RoundToInt(t * 1000);
                if (!occupiedTimes.Contains(checkTimeMs))
                {
                    if (Random.value < decoyDensity)
                    {
                        var decoy = new OsuBeatmapLoader.OsuNote();
                        decoy.timeSec = t;
                        decoy.type = "decoy";
                        decoy.dir = dirs[Random.Range(0, dirs.Length)];
                        decoy.holdDurationSec = 0;
                        decoys.Add(decoy);
                    }
                }
            }
        }

        notes.AddRange(decoys);
        notes.Sort((a, b) => a.timeSec.CompareTo(b.timeSec));
        Debug.Log($"[SpawnNote] Generated {decoys.Count} Decoys.");
    }

    void LoadAudioFromBeatmap(string beatmapDir, string osuText)
    {
        string audioFileName = null;
        foreach (var line in osuText.Split('\n'))
        {
            if (line.StartsWith("AudioFilename:"))
            {
                audioFileName = line.Substring("AudioFilename:".Length).Trim();
                break;
            }
        }
        if (string.IsNullOrEmpty(audioFileName)) return;

        string fullPath = Path.Combine(beatmapDir, audioFileName);
        if (!File.Exists(fullPath))
        {
            string mp3 = fullPath + ".mp3";
            if (File.Exists(mp3)) fullPath = mp3; else return;
        }
        StartCoroutine(LoadAudioClip(fullPath));
    }

    IEnumerator LoadAudioClip(string path)
    {
        if (countdownText != null) countdownText.text = "Loading...";
        string url = "file:///" + path.Replace("\\", "/");
        using (var www = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.UNKNOWN))
        {
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success)
            {
                audioSource.clip = DownloadHandlerAudioClip.GetContent(www);
                if (countdownText != null) countdownText.text = "";
                ScheduleStartAndCountdown();
            }
        }
    }

    void ScheduleStartAndCountdown()
    {
        if (audioSource.clip == null) return;
        songStartDspTime = AudioSettings.dspTime + audioLeadInSec + preGameCountdown;
        isSongReady = true;
        audioSource.PlayScheduled(songStartDspTime);
        StartCoroutine(CountdownRoutine());
    }

    IEnumerator CountdownRoutine()
    {
        float timer = preGameCountdown;
        while (timer > 0f)
        {
            if (countdownText != null) countdownText.text = $"Start: {Mathf.Ceil(timer)}";
            timer -= Time.deltaTime;
            yield return null;
        }
        if (countdownText != null) countdownText.text = "";
    }

    void Update()
    {
        if (isGameOver || !isSongReady || notes == null || audioSource.clip == null) return;

        double songTime = AudioSettings.dspTime - songStartDspTime;

        for (int i = notes.Count - 1; i >= 0; i--)
        {
            var note = notes[i];
            float hitTimeSec = note.timeSec + extraOffsetSeconds;
            float speedForThisNote = (note.type == "hold") ? holdNoteSpeed : noteSpeed;
            float effectiveTravelDuration = travelDuration / Mathf.Max(0.001f, speedForThisNote);

            if (songTime >= hitTimeSec - effectiveTravelDuration)
            {
                SpawnOne(note, hitTimeSec, speedForThisNote, effectiveTravelDuration);
                notes.RemoveAt(i);
            }
        }
        if (isSongReady && notes.Count == 0 && !audioSource.isPlaying) OnSongComplete();
    }

    void OnSongComplete()
    {
        isSongReady = false;
        var ui = FindFirstObjectByType<LevelCompleteUI>();
        if (ui != null) ui.ShowLevelComplete(HitJudgement.score);
    }

    public static void FreezeGameplay()
    {
        if (instance != null) instance.InternalFreezeGameplay();
    }

    // 🔥 FUNGSI PENTING: Mematikan gameplay dan efek saat Game Over
    private void InternalFreezeGameplay()
    {
        if (isGameOver) return;
        isGameOver = true;
        isSongReady = false;

        if (audioSource != null) audioSource.Stop();

        // Matikan Note Movement
        Note[] allNotes = FindObjectsByType<Note>(FindObjectsSortMode.None);
        foreach (var note in allNotes) note.enabled = false;

        // 🔥 MATIKAN EFEK KIAI/FLASH AGAR LAYAR TIDAK PUTIH TERUS
        var kiaiManager = FindFirstObjectByType<KiaiEffectManager>();
        if (kiaiManager != null)
        {
            kiaiManager.StopKiaiImmediate();
        }
    }

    void SpawnOne(OsuBeatmapLoader.OsuNote note, float hitTimeSec, float speedForThisNote, float effectiveTravelDuration)
    {
        Transform realSpawn = null, realTarget = null;
        switch (note.dir)
        {
            case "up": realSpawn = upSpawn; realTarget = upTarget; break;
            case "down": realSpawn = downSpawn; realTarget = downTarget; break;
            case "left": realSpawn = leftSpawn; realTarget = leftTarget; break;
            case "right": realSpawn = rightSpawn; realTarget = rightTarget; break;
        }
        if (realSpawn == null || realTarget == null) return;

        Quaternion realRotation = GetRotationForSpawn(realSpawn);

        // Logic Note Special
        bool tryGhostHold = (note.type == "hold") && (Random.value < ghostHoldChance);
        bool tryPhantomSlide = (note.type != "hold" && note.type != "decoy") && (Random.value < phantomChance);

        GameObject prefabToSpawn = null;
        Transform fakeSpawnTransform = null;
        Transform fakeTargetTransform = null;
        Quaternion spawnRotation = realRotation;

        if (note.type == "hold") prefabToSpawn = holdNotePrefab;
        else if (tryPhantomSlide && phantomNotePrefab != null) prefabToSpawn = phantomNotePrefab;
        else
        {
            if (note.type == "obstacle") prefabToSpawn = obstaclePrefab;
            else
            {
                if (note.type != "decoy")
                {
                    normalNoteCounter++;
                    if (normalNoteCounter >= 30 && obstaclePrefab != null)
                    {
                        prefabToSpawn = obstaclePrefab;
                        normalNoteCounter = 0;
                    }
                    else prefabToSpawn = normalNotePrefab;
                }
                else
                {
                    prefabToSpawn = normalNotePrefab; // Decoy
                }
            }
        }

        if (tryGhostHold || tryPhantomSlide)
        {
            fakeSpawnTransform = GetRandomOtherSpawn(note.dir);
            fakeTargetTransform = GetCorrespondingTarget(fakeSpawnTransform);
            spawnRotation = GetRotationForSpawn(fakeSpawnTransform);
        }

        GameObject obj = Instantiate(prefabToSpawn, realSpawn.position, spawnRotation);

        if (prefabToSpawn == obstaclePrefab)
        {
            var ob = obj.GetComponent<Obstacle>();
            if (ob != null)
            {
                ob.hitTime = hitTimeSec;
                ob.spawnPos = realSpawn.position;
                ob.targetPos = realTarget.position;
                ob.travelDuration = travelDuration;
                ob.speed = speedForThisNote;
            }
            return;
        }

        var n = obj.GetComponent<Note>();
        if (n == null) return;

        n.targetRotation = realRotation;
        n.hitTime = hitTimeSec;
        n.spawnPos = realSpawn.position;
        n.targetPos = realTarget.position;
        n.travelDuration = travelDuration;
        n.speed = speedForThisNote;
        n.dir = note.dir;
        n.type = note.type;
        n.holdDurationSec = note.holdDurationSec;

        // Setup Decoy Param
        if (note.type == "decoy")
        {
            n.despawnOffset = decoyDespawnOffset;
            n.isPhantom = false;
            n.isGhostHold = false;
        }
        else if (tryGhostHold)
        {
            n.isGhostHold = true;
            n.ghostSwitchPoint = 0.5f;
            n.fadeSpeed = ghostFadeSpeed;
            n.fakeSpawnPos = fakeSpawnTransform.position;
            n.fakeTargetPos = fakeTargetTransform.position;
        }
        else if (tryPhantomSlide)
        {
            n.isPhantom = true;
            n.switchThreshold = Random.Range(0.4f, 0.6f);
            n.transitionDuration = phantomSmoothness;
            n.fakeSpawnPos = fakeSpawnTransform.position;
            n.fakeTargetPos = fakeTargetTransform.position;
        }

        float d = Vector3.Distance(n.spawnPos, n.targetPos);
        n.noteMoveSpeed = d / effectiveTravelDuration;
        n.SetupVisuals();

        if (enableTimingCircle && timingCirclePrefab != null && note.type != "hold" && note.type != "decoy")
        {
            GameObject circleGO = Instantiate(timingCirclePrefab, obj.transform.position, Quaternion.identity, effectsParent);
            var tc = circleGO.GetComponent<TimingCircle>();
            if (tc != null)
            {
                tc.hitTime = hitTimeSec;
                tc.travelDuration = effectiveTravelDuration;
                tc.startDsp = songStartDspTime;
                tc.followTarget = obj.transform;
                float noteScale = obj.transform.localScale.x;
                tc.startScale = timingCircleStartScale * noteScale;
                tc.endScale = timingCircleEndScale * noteScale;
            }
        }
    }

    // Helpers
    Quaternion GetRotationForSpawn(Transform t) { if (t == upSpawn) return Quaternion.Euler(0, 0, 180); if (t == downSpawn) return Quaternion.Euler(0, 0, 0); if (t == leftSpawn) return Quaternion.Euler(0, 0, -90); if (t == rightSpawn) return Quaternion.Euler(0, 0, 90); return Quaternion.identity; }
    Transform GetRandomOtherSpawn(string d) { List<Transform> o = new List<Transform>(); if (d != "up") o.Add(upSpawn); if (d != "down") o.Add(downSpawn); if (d != "left") o.Add(leftSpawn); if (d != "right") o.Add(rightSpawn); if (o.Count == 0) return null; return o[Random.Range(0, o.Count)]; }
    Transform GetCorrespondingTarget(Transform t) { if (t == upSpawn) return upTarget; if (t == downSpawn) return downTarget; if (t == leftSpawn) return leftTarget; if (t == rightSpawn) return rightTarget; return null; }
}