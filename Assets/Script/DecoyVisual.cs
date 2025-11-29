using UnityEngine;

[RequireComponent(typeof(Note))]
[RequireComponent(typeof(SpriteRenderer))]
public class DecoyVisual : MonoBehaviour
{
    [Header("Glitch Settings")]
    public Color glitchColor = new Color(1f, 0.2f, 0.2f, 0.6f); // Merah transparan
    public float shakeAmount = 0.05f; // Seberapa kuat getarannya
    public float flickerSpeed = 0.1f; // Kecepatan kedip

    private Note note;
    private SpriteRenderer sr;
    private Vector3 baseLocalPos;
    private float flickerTimer;

    void Start()
    {
        note = GetComponent<Note>();
        sr = GetComponent<SpriteRenderer>();

      
    }

    void Update()
    {
        // HANYA JALAN JIKA INI DECOY
        if (note == null || note.type != "decoy")
        {
            this.enabled = false; // Matikan script jika bukan decoy
            return;
        }

        // 1. Efek Warna & Transparansi (Flicker)
        flickerTimer += Time.deltaTime;
        if (flickerTimer > flickerSpeed)
        {
            flickerTimer = 0;
            // Ubah alpha secara acak antara 0.3 (samar) dan 0.7 (agak jelas)
            float randomAlpha = Random.Range(0.3f, 0.7f);
            sr.color = new Color(glitchColor.r, glitchColor.g, glitchColor.b, randomAlpha);
        }

        Vector3 shakeOffset = (Vector3)Random.insideUnitCircle * shakeAmount;


        sr.transform.localPosition += shakeOffset * 0.1f; // Efek getar halus
    }
}