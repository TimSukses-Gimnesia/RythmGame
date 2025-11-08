using UnityEngine;

public class Note : MonoBehaviour
{
    [Header("Timing")]
    public float hitTime;      // detik saat note harus kena
    public string dir;         // "up", "down", "left", "right" <-- DITAMBAHKAN

    [Header("Movement")]
    public Vector3 spawnPos;
    public Vector3 targetPos;
    public float travelDuration;
    public float speed = 1f;

    [HideInInspector]
    public bool isHit = false; // Tandai jika sudah kena hit <-- DITAMBAHKAN

    double songStartDspTime;

    void Start()
    {
        // Ambil waktu mulai lagu dari Spawner
        var spawner = FindFirstObjectByType<SpawnNote>();
        songStartDspTime = spawner != null ? spawner.songStartDspTime : AudioSettings.dspTime;
        transform.position = spawnPos;
    }

    void Update()
    {
        // Jika note sudah di-hit, hentikan pergerakannya
        if (isHit) return;

        double songTime = AudioSettings.dspTime - songStartDspTime;
        double effectiveDuration = travelDuration / Mathf.Max(0.001f, speed);
        double spawnTime = hitTime - effectiveDuration;
        double progress = (songTime - spawnTime) / effectiveDuration;
        progress = Mathf.Clamp01((float)progress);

        transform.position = Vector3.Lerp(spawnPos, targetPos, (float)progress);

        // Logic "Hapus note setelah melewati target" DIHAPUS DARI SINI.
        // Script HitJudgement.cs sekarang yang bertanggung jawab
        // untuk menghapus note (baik saat kena atau saat miss).
    }
}