using UnityEngine;
using System.Collections.Generic;
using System.IO;

[RequireComponent(typeof(AudioSource))]
public class SpawnNote : MonoBehaviour
{
    [Header("OSU Beatmap")]
    public TextAsset osuBeatmap;
    public float extraOffsetSeconds = 0f;

    [Header("Spawn Settings")]
    [Tooltip("Baseline travel time for speed = 1. Real travel is travelDuration / speedForThisNote")]
    public float travelDuration = 2.0f;   // detik sebelum hit (untuk speed = 1)
    public float noteSpeed = 1.0f;        // multiplier (visual)
    public float holdNoteSpeed = 0.4f;    // multiplier (visual)

    [Header("Prefabs")]
    public GameObject notePrefab;
    public GameObject holdNotePrefab;

    // -------- NEW: Timing Circle (helper) --------
    [Header("Timing Circle (Helper)")]
    [Tooltip("Enable/disable timing helper circles for normal (tap) notes.")]
    public bool enableTimingCircle = true;

    [Tooltip("Prefab with SpriteRenderer + TimingCircle script. Leave null to disable.")]
    public GameObject timingCirclePrefab;

    [Tooltip("Optional parent to keep effects out of gameplay hierarchy. Can be left empty.")]
    public Transform effectsParent;

    [Tooltip("Sorting Layer for timing circles (should be same layer as notes).")]
    public string timingCircleSortingLayer = "Default";

    [Tooltip("Sorting Order for timing circles. Use a smaller value than the note so circle renders BEHIND the note.")]
    public int timingCircleSortingOrder = -5;

    [Tooltip("Start scale of the circle when it begins shrinking.")]
    public float timingCircleStartScale = 2.0f;

    [Tooltip("End scale of the circle right at the hit line.")]
    public float timingCircleEndScale = 1.0f;

    [Tooltip("Base color (with alpha) of the circle. You can keep it semi-transparent.")]
    public Color timingCircleColor = new Color(1f, 1f, 1f, 0.25f);
    // --------------------------------------------

    [Header("Lanes")]
    public Transform upSpawn, downSpawn, leftSpawn, rightSpawn;
    public Transform upTarget, downTarget, leftTarget, rightTarget;

    [HideInInspector] public double songStartDspTime;
    private AudioSource audioSource;
    private List<OsuBeatmapLoader.OsuNote> notes;
    private float audioLeadInSec;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        var chart = OsuBeatmapLoader.Load(osuBeatmap);
        audioLeadInSec = chart.audioLeadInSec;

        if (!string.IsNullOrEmpty(chart.audioFilename))
        {
            string clipName = Path.GetFileNameWithoutExtension(chart.audioFilename);
            AudioClip clip = Resources.Load<AudioClip>(clipName);
            if (clip != null)
                audioSource.clip = clip;
            else
                Debug.LogWarning("Audio clip '" + clipName + "' tidak ditemukan di Resources.");
        }

        songStartDspTime = AudioSettings.dspTime + audioLeadInSec;
        audioSource.PlayScheduled(songStartDspTime);

        notes = chart.notes;
    }

    // Diperbarui agar note spawn di waktu yang tepat berdasarkan speed-nya
    void Update()
    {
        double songTime = AudioSettings.dspTime - songStartDspTime;

        for (int i = notes.Count - 1; i >= 0; i--)
        {
            OsuBeatmapLoader.OsuNote note = notes[i];
            float hitTimeSec = note.timeSec + extraOffsetSeconds;

            // 1. Tentukan kecepatan note INI
            float speedForThisNote = (note.type == "hold") ? holdNoteSpeed : noteSpeed;

            // 2. Effective travel (hindari div 0)
            float effectiveTravelDuration = travelDuration / Mathf.Max(0.001f, speedForThisNote);

            // 3. Cek kapan spawn
            if (songTime >= hitTimeSec - effectiveTravelDuration)
            {
                SpawnOne(note, hitTimeSec, speedForThisNote, effectiveTravelDuration);
                notes.RemoveAt(i);
            }
        }
    }

    void SpawnOne(OsuBeatmapLoader.OsuNote note, float hitTimeSec, float speedForThisNote, float effectiveTravelDuration)
    {
        Transform spawnPos, targetPos;
        Quaternion spawnRotation = Quaternion.identity;

        switch (note.dir)
        {
            case "up":
                spawnPos = upSpawn; targetPos = upTarget;
                spawnRotation = Quaternion.Euler(0, 0, 0);
                break;
            case "down":
                spawnPos = downSpawn; targetPos = downTarget;
                spawnRotation = Quaternion.Euler(0, 0, 180);
                break;
            case "left":
                spawnPos = leftSpawn; targetPos = leftTarget;
                spawnRotation = Quaternion.Euler(0, 0, 90);
                break;
            case "right":
                spawnPos = rightSpawn; targetPos = rightTarget;
                spawnRotation = Quaternion.Euler(0, 0, -90);
                break;
            default:
                spawnPos = upSpawn; targetPos = upTarget;
                break;
        }

        // Safety: if any transform is missing, bail out gracefully
        if (spawnPos == null || targetPos == null)
        {
            Debug.LogWarning("[SpawnNote] Missing spawn/target transform for dir=" + note.dir);
            return;
        }

        // --- Spawn NOTE object ---
        GameObject prefabToSpawn = (note.type == "hold" && holdNotePrefab != null) ? holdNotePrefab : notePrefab;
        if (prefabToSpawn == null)
        {
            Debug.LogWarning("[SpawnNote] Missing note prefab for type=" + note.type);
            return;
        }

        GameObject obj = Instantiate(prefabToSpawn, spawnPos.position, spawnRotation);

        var n = obj.GetComponent<Note>();
        if (n == null)
        {
            Debug.LogError("[SpawnNote] Spawned prefab does not contain Note component!", obj);
            return;
        }

        n.hitTime = hitTimeSec;
        n.spawnPos = spawnPos.position;
        n.targetPos = targetPos.position;
        n.travelDuration = travelDuration; // baseline
        n.speed = speedForThisNote;
        n.dir = note.dir;
        n.type = note.type;
        n.holdDurationSec = note.holdDurationSec;

        float distance = Vector3.Distance(n.spawnPos, n.targetPos);
        float effectiveDuration = n.travelDuration / Mathf.Max(0.001f, n.speed);
        n.noteMoveSpeed = distance / effectiveDuration;
        n.SetupVisuals();

        // --- NEW: Spawn timing circle for TAP notes only ---
        if (enableTimingCircle && timingCirclePrefab != null && note.type != "hold")
        {
            // World spawn position of the note
            Vector3 spawnWorldPos = obj.transform.position;

            Transform parentForCircle = effectsParent != null ? effectsParent : null;
            GameObject circleGO = Instantiate(timingCirclePrefab, spawnWorldPos, Quaternion.identity, parentForCircle);

            // Optional name for debugging
            circleGO.name = "TimingCircle_" + note.dir + "_" + hitTimeSec.ToString("0.000");

            // Renderer settings
            var sr = circleGO.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sortingLayerName = timingCircleSortingLayer;
                sr.sortingOrder = timingCircleSortingOrder; // behind the note
                sr.color = timingCircleColor; // base color + alpha in inspector
            }

            // Timed behavior configuration
            var tc = circleGO.GetComponent<TimingCircle>();
            if (tc != null)
            {
                tc.hitTime = hitTimeSec;
                tc.travelDuration = effectiveTravelDuration;
                tc.startDsp = songStartDspTime;
                tc.followTarget = obj.transform;

                // <<< AUTO-SCALE TO NOTE SIZE >>>
                float noteScale = obj.transform.localScale.x; // your note is (1,1,1), but this makes it future proof
                tc.startScale = timingCircleStartScale * noteScale;
                tc.endScale = timingCircleEndScale * noteScale;
            }
            else
            {
                Debug.LogWarning("[SpawnNote] TimingCircle prefab has no TimingCircle script.");
            }
        }
    }
}