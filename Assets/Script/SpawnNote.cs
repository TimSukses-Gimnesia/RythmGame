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
    public float travelDuration = 2.0f; // detik sebelum hit (untuk speed = 1)
    public float noteSpeed = 1.0f;      // multiplier (visual)
    public float holdNoteSpeed = 0.4f;  // multiplier (visual)

    [Header("Prefabs")]
    public GameObject notePrefab;
    public GameObject holdNotePrefab;

    [Header("Lanes")]
    public Transform upSpawn, downSpawn, leftSpawn, rightSpawn;
    public Transform upTarget, downTarget, leftTarget, rightTarget;

    [HideInInspector] public double songStartDspTime;
    private AudioSource audioSource;
    private List<OsuBeatmapLoader.OsuNote> notes;
    private float audioLeadInSec;

    void Start()
    {
        // ... (Fungsi Start() Anda tetap sama persis) ...
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

    // --- FUNGSI UPDATE (MODIFIKASI) ---
    // Diperbarui agar note spawn di waktu yang tepat berdasarkan speed-nya
    void Update()
    {
        double songTime = AudioSettings.dspTime - songStartDspTime;
        for (int i = notes.Count - 1; i >= 0; i--)
        {
            OsuBeatmapLoader.OsuNote note = notes[i]; // Ambil data note
            float hitTimeSec = note.timeSec + extraOffsetSeconds;

            // 1. Tentukan kecepatan note INI
            float speedForThisNote;
            if (note.type == "hold")
                speedForThisNote = holdNoteSpeed;
            else
                speedForThisNote = noteSpeed;

            // 2. Hitung durasi perjalanan (effective duration) note INI
            //    (Pastikan speed tidak 0)
            float effectiveTravelDuration = travelDuration / Mathf.Max(0.001f, speedForThisNote);

            // 3. Gunakan durasi yang BENAR untuk mengecek kapan harus spawn
            if (songTime >= hitTimeSec - effectiveTravelDuration)
            {
                SpawnOne(note, hitTimeSec); // Kirim data note-nya
                notes.RemoveAt(i);
            }
        }
    }


    void SpawnOne(OsuBeatmapLoader.OsuNote note, float hitTimeSec)
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

        GameObject prefabToSpawn;
        float speedToUse;

  
        if (note.type == "hold" && holdNotePrefab != null)
        {
            prefabToSpawn = holdNotePrefab;
            speedToUse = holdNoteSpeed;
        }
        else
        {
            prefabToSpawn = notePrefab;
            speedToUse = noteSpeed;
        }

        GameObject obj = Instantiate(prefabToSpawn, spawnPos.position, spawnRotation);

   
        var n = obj.GetComponent<Note>();
        n.hitTime = hitTimeSec;
        n.spawnPos = spawnPos.position;
        n.targetPos = targetPos.position;
        n.travelDuration = travelDuration;
        n.speed = speedToUse; 
        n.dir = note.dir;

     
        n.type = note.type;
        n.holdDurationSec = note.holdDurationSec;

        float distance = Vector3.Distance(n.spawnPos, n.targetPos);
        float effectiveDuration = n.travelDuration / Mathf.Max(0.001f, n.speed);
        n.noteMoveSpeed = distance / effectiveDuration;

        n.SetupVisuals();
    }
}