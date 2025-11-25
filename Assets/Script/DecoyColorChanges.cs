using UnityEngine;

[RequireComponent(typeof(Note))]
[RequireComponent(typeof(SpriteRenderer))]
public class DecoyColorChanger : MonoBehaviour
{
    [Header("Color Transition Settings")]
    public Color startColor = Color.white; // Warna awal (mirip note asli)
    public Color endColor = Color.red;     // Warna akhir (tanda bahaya)
    [Tooltip("Persentase perjalanan saat warna mulai berubah (0.0 - 1.0)")]
    public float startChangePoint = 0.2f; // Mulai berubah setelah 20% perjalanan
    [Tooltip("Persentase perjalanan saat warna selesai berubah (0.0 - 1.0)")]
    public float endChangePoint = 0.8f;   // Selesai berubah di 80% perjalanan

    private Note note;
    private SpriteRenderer sr;
    private double spawnTime;
    private double effectiveDuration;

    void Start()
    {
        note = GetComponent<Note>();
        sr = GetComponent<SpriteRenderer>();

        // HANYA JALAN JIKA INI DECOY
        if (note == null || note.type != "decoy")
        {
            this.enabled = false;
            return;
        }

        // Hitung waktu spawn & durasi perjalanan
        var spawner = FindFirstObjectByType<SpawnNote>();
        effectiveDuration = note.travelDuration / Mathf.Max(0.001f, note.speed);
        spawnTime = note.hitTime - effectiveDuration;

        // Set warna awal
        sr.color = startColor;
    }

    void Update()
    {
        if (note.isHit) return;

        double songTime = AudioSettings.dspTime - FindFirstObjectByType<SpawnNote>().songStartDspTime;

        // Hitung progres perjalanan (0.0 = spawn, 1.0 = target)
        double t = (songTime - spawnTime) / effectiveDuration;
        float progress = Mathf.Clamp01((float)t);

        // Hitung progres transisi warna (Lerp factor)
        // Menggunakan InverseLerp untuk memetakan range progress ke range 0-1
        float colorProgress = Mathf.InverseLerp(startChangePoint, endChangePoint, progress);

        // Ubah warna secara halus
        sr.color = Color.Lerp(startColor, endColor, colorProgress);
    }
}