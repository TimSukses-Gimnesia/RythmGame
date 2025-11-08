using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class SpawnNote : MonoBehaviour
{
    [Header("OSU Beatmap")]
    public TextAsset osuBeatmap;        // File .osu (rename ke .txt agar TextAsset)
    public float extraOffsetSeconds = 0f; // offset manual (detik)

    [Header("Spawn Settings")]
    public float travelDuration = 2.0f;  // detik sebelum hit
    public float noteSpeed = 1.0f;       // multiplier (visual)

    [Header("Prefabs")]
    public GameObject notePrefab;

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

        // Muat AudioClip dari nama file .osu (contoh: dari Resources)
        if (!string.IsNullOrEmpty(chart.audioFilename))
        {
            string clipName = Path.GetFileNameWithoutExtension(chart.audioFilename);
            AudioClip clip = Resources.Load<AudioClip>(clipName);
            if (clip != null)
                audioSource.clip = clip;
            else
                Debug.LogWarning("Audio clip '" + clipName + "' tidak ditemukan di Resources.");
        }

        // Jadwalkan audio mulai setelah AudioLeadIn:contentReference[oaicite:9]{index=9}
        songStartDspTime = AudioSettings.dspTime + audioLeadInSec;
        audioSource.PlayScheduled(songStartDspTime);

        notes = chart.notes;
    }

    void Update()
    {
        double songTime = AudioSettings.dspTime - songStartDspTime;
        for (int i = notes.Count - 1; i >= 0; i--)
        {
            float hitTimeSec = notes[i].timeSec + extraOffsetSeconds;
            // Spawn saat berada dalam jendela travelDuration sebelum hit
            if (songTime >= hitTimeSec - travelDuration)
            {
                SpawnOne(notes[i], hitTimeSec);
                notes.RemoveAt(i);
            }
        }
    }

    void SpawnOne(OsuBeatmapLoader.OsuNote note, float hitTimeSec)
    {
        Transform spawnPos, targetPos;
        switch (note.dir)
        {
            case "up":    spawnPos = upSpawn;    targetPos = upTarget;    break;
            case "down":  spawnPos = downSpawn;  targetPos = downTarget;  break;
            case "left":  spawnPos = leftSpawn;  targetPos = leftTarget;  break;
            case "right": spawnPos = rightSpawn; targetPos = rightTarget; break;
            default:      spawnPos = upSpawn;    targetPos = upTarget;    break;
        }
        GameObject obj = Instantiate(notePrefab, spawnPos.position, Quaternion.identity);
        var n = obj.GetComponent<Note>();
        n.hitTime = hitTimeSec;               // waktu kena (detik)
        n.spawnPos = spawnPos.position;
        n.targetPos = targetPos.position;
        n.travelDuration = travelDuration;
        n.speed = noteSpeed;
    }
}
