using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Diperlukan untuk menggunakan fungsi .Where()

public class ObstacleSpawner : MonoBehaviour
{
    [Header("Dependencies")]
    // File beatmap yang sama dengan yang digunakan oleh SpawnNote
    public TextAsset osuBeatmap; 
    
    // Referensi ke SpawnNote untuk mendapatkan waktu mulai lagu yang akurat
    private SpawnNote mainSpawner; 

    [Header("Obstacle Prefabs")]
    public GameObject horizontalObstaclePrefab;
    public GameObject verticalObstaclePrefab;

    [Header("Timing")]
    public float extraOffsetSeconds = 0f;
    
    [Tooltip("Waktu yang dibutuhkan Obstacle untuk mencapai tengah. Harus SAMA dengan travelDuration di SpawnNote.cs.")]
    public float timeBeforeHit = 2.0f; // Sesuaikan dengan nilai travelDuration Anda

    // Daftar Obstacle yang dimuat dari file, hanya berisi data bertipe "obstacle"
    private List<OsuBeatmapLoader.OsuNote> obstacleNotes;

    void Start()
    {
        // Mencari komponen SpawnNote
        mainSpawner = FindFirstObjectByType<SpawnNote>();
        if (mainSpawner == null)
        {
            Debug.LogError("ObstacleSpawner memerlukan script SpawnNote (untuk timing)! Spawner dinonaktifkan.", this);
            enabled = false;
            return;
        }

        // 1. Load beatmap yang SAMA
        var chart = OsuBeatmapLoader.Load(osuBeatmap);
        
        // 2. Filter hanya data Obstacle. 
        // Beatmap Loader menerjemahkan HitObject type 13 menjadi note.type = "obstacle"
        obstacleNotes = chart.notes.Where(n => n.type == "obstacle").ToList(); 
        Debug.Log($"Loaded {obstacleNotes.Count} obstacle notes from beatmap.");
    }

    void Update()
    {
        // Pastikan mainSpawner ada dan masih ada obstacle yang perlu di-spawn
        if (mainSpawner == null || obstacleNotes == null || obstacleNotes.Count == 0) return;

        // Hitung waktu lagu saat ini
        double songTime = AudioSettings.dspTime - mainSpawner.songStartDspTime;

        // Loop mundur
        for (int i = obstacleNotes.Count - 1; i >= 0; i--)
        {
            OsuBeatmapLoader.OsuNote note = obstacleNotes[i];
            float hitTimeSec = note.timeSec + extraOffsetSeconds;

            // Obstacle di-spawn 'timeBeforeHit' detik sebelum waktu hit yang sebenarnya.
            if (songTime >= hitTimeSec - timeBeforeHit)
            {
                SpawnOneObstacle(note, hitTimeSec);
                obstacleNotes.RemoveAt(i);
            }
        }
    }

    void SpawnOneObstacle(OsuBeatmapLoader.OsuNote note, float hitTimeSec)
    {
        GameObject prefabToSpawn = null;
        
        // Tentukan Prefab yang digunakan berdasarkan Arah (dir)
        // note.dir diambil dari ExtraData di file .osu (Contoh: "left", "up")
        if (note.dir == "left" || note.dir == "right")
        {
            prefabToSpawn = horizontalObstaclePrefab;
        }
        else if (note.dir == "up" || note.dir == "down")
        {
            prefabToSpawn = verticalObstaclePrefab;
        }

        if (prefabToSpawn != null)
        {
            // Instansiasi Obstacle di Vector3.zero. 
            // Script MovingObstacle.cs yang terpasang pada prefab akan segera memindahkannya
            GameObject obj = Instantiate(prefabToSpawn, Vector3.zero, Quaternion.identity); 
            obj.name = "Obstacle_" + note.dir + "_" + hitTimeSec.ToString("0.000");
        }
        else
        {
            Debug.LogError($"Prefab Obstacle is NULL for direction {note.dir}! Check ObstacleSpawner Inspector!");
        }
    }
}