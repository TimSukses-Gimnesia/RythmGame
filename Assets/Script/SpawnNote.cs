using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Collections;
using TMPro;

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
    [Tooltip("Baseline travel time for speed = 1. Real travel is travelDuration / speedForThisNote")]
    public float travelDuration = 2.0f;
    public float noteSpeed = 1.0f;
    public float holdNoteSpeed = 0.4f;

    [Header("Prefabs")]
    public GameObject notePrefab;
    public GameObject holdNotePrefab;

    [Header("Timing Circle (Helper)")]
    public bool enableTimingCircle = true;
    public GameObject timingCirclePrefab;
    public Transform effectsParent;
    public string timingCircleSortingLayer = "Default";
    public int timingCircleSortingOrder = -5;
    public float timingCircleStartScale = 2.0f;
    public float timingCircleEndScale = 1.0f;
    public Color timingCircleColor = new Color(1f, 1f, 1f, 0.25f);

    [Header("Lanes")]
    public Transform upSpawn, downSpawn, leftSpawn, rightSpawn;
    public Transform upTarget, downTarget, leftTarget, rightTarget;

    [HideInInspector] public double songStartDspTime;
    private AudioSource audioSource;
    private List<OsuBeatmapLoader.OsuNote> notes;
    private float audioLeadInSec;
    private bool isSongReady = false; // ✅ flag untuk memastikan audio sudah siap

    void Start()
    {
        audioSource = GetComponent<AudioSource>();

        // Prefer GameSession (if Retry used), fallback to PlayerPrefs
        osuFilePath = GameSession.SelectedOsuFile;
        if (string.IsNullOrEmpty(osuFilePath) && PlayerPrefs.HasKey("SelectedOsuFile"))
        {
            osuFilePath = PlayerPrefs.GetString("SelectedOsuFile");
            GameSession.SelectedOsuFile = osuFilePath; // store it back for Retry
            GameSession.SelectedBeatmapPath = PlayerPrefs.GetString("SelectedBeatmapPath");
        }

        if (!string.IsNullOrEmpty(osuFilePath) && File.Exists(osuFilePath))
        {
            Debug.Log("🎵 Loading beatmap from file: " + osuFilePath);
            string osuText = File.ReadAllText(osuFilePath);
            osuBeatmap = new TextAsset(osuText);

            var chart = OsuBeatmapLoader.Load(osuBeatmap);
            audioLeadInSec = chart.audioLeadInSec;
            notes = chart.notes;

            string beatmapDir = Path.GetDirectoryName(osuFilePath);
            LoadAudioFromBeatmap(beatmapDir, osuText);
        }
        else
        {
            Debug.LogWarning("⚠️ Beatmap file not found or missing from GameSession/PlayerPrefs.");
        }
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

        if (string.IsNullOrEmpty(audioFileName))
        {
            Debug.LogError("❌ AudioFilename not found in .osu file.");
            return;
        }

        string fullPath = Path.Combine(beatmapDir, audioFileName);

        // Fallback: jika tidak ada ekstensi .mp3 di nama file
        if (!File.Exists(fullPath))
        {
            string mp3Fallback = fullPath + ".mp3";
            if (File.Exists(mp3Fallback))
            {
                fullPath = mp3Fallback;
                Debug.Log("🎶 Using .mp3 fallback: " + fullPath);
            }
            else
            {
                Debug.LogError("❌ Audio file not found at: " + fullPath);
                return;
            }
        }

        Debug.Log("✅ Audio file found at: " + fullPath);
        StartCoroutine(LoadAudioClip(fullPath));
    }

    IEnumerator LoadAudioClip(string path)
    {
        if (countdownText != null)
            countdownText.text = "Loading Audio...";

        string url = "file:///" + path.Replace("\\", "/");
        AudioType type = GetAudioTypeFromExtension(path);

        using (var www = UnityEngine.Networking.UnityWebRequestMultimedia.GetAudioClip(url, type))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                audioSource.clip = UnityEngine.Networking.DownloadHandlerAudioClip.GetContent(www);
                Debug.Log("✅ Audio loaded successfully: " + path);

                if (countdownText != null)
                    countdownText.text = "";

                ScheduleStartAndCountdown();
            }
            else
            {
                if (countdownText != null)
                    countdownText.text = "Failed to load audio.";
                Debug.LogError("❌ Failed to load audio: " + www.error);
            }
        }
    }

    void ScheduleStartAndCountdown()
    {
        if (audioSource.clip == null)
        {
            Debug.LogWarning("⚠️ ScheduleStartAndCountdown() called but clip == null");
            return;
        }

        songStartDspTime = AudioSettings.dspTime + audioLeadInSec + preGameCountdown;
        isSongReady = true; // ✅ tandai bahwa audio sudah siap dan waktu sudah valid

        Debug.Log($"▶️ Scheduling playback at DSP {songStartDspTime:F3} (now {AudioSettings.dspTime:F3})");

        audioSource.PlayScheduled(songStartDspTime);
        StartCoroutine(CountdownRoutine());
    }

    AudioType GetAudioTypeFromExtension(string path)
    {
        string ext = Path.GetExtension(path).ToLower();
        if (ext == ".mp3") return AudioType.MPEG;
        if (ext == ".ogg") return AudioType.OGGVORBIS;
        if (ext == ".wav") return AudioType.WAV;
        return AudioType.UNKNOWN;
    }

    IEnumerator CountdownRoutine()
    {
        float timer = preGameCountdown;
        while (timer > 0f)
        {
            if (countdownText != null)
                countdownText.text = $"Start in : {Mathf.Ceil(timer)}";
            timer -= Time.deltaTime;
            yield return null;
        }

        if (countdownText != null)
            countdownText.text = "";
    }

    void Update()
    {
        // ✅ Jangan spawn note sampai lagu siap (audio sudah load & dijadwalkan)
        if (!isSongReady || notes == null || audioSource.clip == null)
        {
            if (countdownText != null && !string.IsNullOrEmpty(countdownText.text) && countdownText.text != "Loading Audio...")
                countdownText.text = "Waiting for audio...";
            return;
        }

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
    }

    void OnDestroy()
    {
        PlayerPrefs.DeleteKey("SelectedOsuFile");
        PlayerPrefs.DeleteKey("SelectedBeatmapPath");
    }

    void SpawnOne(OsuBeatmapLoader.OsuNote note, float hitTimeSec, float speedForThisNote, float effectiveTravelDuration)
    {
        Transform spawnPos = null, targetPos = null;
        Quaternion spawnRotation = Quaternion.identity;

        switch (note.dir)
        {
            case "up": spawnPos = upSpawn; targetPos = upTarget; spawnRotation = Quaternion.Euler(0, 0, 180); break;
            case "down": spawnPos = downSpawn; targetPos = downTarget; spawnRotation = Quaternion.Euler(0, 0, 0); break;
            case "left": spawnPos = leftSpawn; targetPos = leftTarget; spawnRotation = Quaternion.Euler(0, 0, -90); break;
            case "right": spawnPos = rightSpawn; targetPos = rightTarget; spawnRotation = Quaternion.Euler(0, 0, 90); break;
        }

        if (spawnPos == null || targetPos == null)
        {
            Debug.LogWarning("[SpawnNote] Missing spawn/target for " + note.dir);
            return;
        }

        GameObject prefabToSpawn = (note.type == "hold" && holdNotePrefab != null) ? holdNotePrefab : notePrefab;
        if (prefabToSpawn == null)
        {
            Debug.LogWarning("[SpawnNote] Missing prefab for " + note.type);
            return;
        }

        GameObject obj = Instantiate(prefabToSpawn, spawnPos.position, spawnRotation);
        var n = obj.GetComponent<Note>();
        if (n == null)
        {
            Debug.LogError("[SpawnNote] Prefab missing Note component!");
            return;
        }

        n.hitTime = hitTimeSec;
        n.spawnPos = spawnPos.position;
        n.targetPos = targetPos.position;
        n.travelDuration = travelDuration;
        n.speed = speedForThisNote;
        n.dir = note.dir;
        n.type = note.type;
        n.holdDurationSec = note.holdDurationSec;

        float distance = Vector3.Distance(n.spawnPos, n.targetPos);
        float effectiveDuration = n.travelDuration / Mathf.Max(0.001f, n.speed);
        n.noteMoveSpeed = distance / effectiveDuration;
        n.SetupVisuals();

        // Spawn Timing Circle
        if (enableTimingCircle && timingCirclePrefab != null && note.type != "hold")
        {
            GameObject circleGO = Instantiate(timingCirclePrefab, obj.transform.position, Quaternion.identity, effectsParent);
            circleGO.name = "TimingCircle_" + note.dir + "_" + hitTimeSec.ToString("0.000");

            var sr = circleGO.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sortingLayerName = timingCircleSortingLayer;
                sr.sortingOrder = timingCircleSortingOrder;
                sr.color = timingCircleColor;
            }

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
}
