using UnityEngine;

public class Note : MonoBehaviour
{
    [Header("Timing")]
    public float hitTime;        // detik saat note harus kena
    public Vector3 spawnPos;
    public Vector3 targetPos;
    public float travelDuration;
    public float speed = 1f;

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
        double songTime = AudioSettings.dspTime - songStartDspTime;
        double effectiveDuration = travelDuration / Mathf.Max(0.001f, speed);
        double spawnTime = hitTime - effectiveDuration;
        double progress = (songTime - spawnTime) / effectiveDuration;
        progress = Mathf.Clamp01((float)progress);

        transform.position = Vector3.Lerp(spawnPos, targetPos, (float)progress);

        // Hapus note setelah melewati target
        if (progress >= 1f && songTime > hitTime + 0.2f)
            Destroy(gameObject);
    }
}

