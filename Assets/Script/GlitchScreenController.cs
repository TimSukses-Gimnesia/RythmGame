using UnityEngine;
using UnityEngine.UI; // Wajib ada untuk mengakses komponen Image

public class GlitchController : MonoBehaviour
{
    [Header("Pengaturan Gerak Objek")]
    public float kecepatanGerak = 500f;
    public float batasAtas = 1000f;
    public float posisiAwalY = -500f;

    [Header("Pengaturan Transparansi")]
    [Range(0f, 1f)] public float transparansi = 0.5f; // Slider 0 (Hilang) sampai 1 (Jelas)

    private RectTransform rectTransform;
    private Image targetImage; // Variabel untuk menyimpan komponen Image

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();

        // Ambil komponen Image yang ada di objek ini
        targetImage = GetComponent<Image>();

        // Cek apakah ada Image, kalau tidak ada kasih peringatan
        if (targetImage == null)
        {
            Debug.LogWarning("Objek ini tidak punya komponen Image! Transparansi tidak bisa diubah.");
        }
    }

    void Update()
    {
        // --- 1. LOGIKA GERAK (Kode Lama) ---
        float deltaY = kecepatanGerak * Time.deltaTime;
        Vector3 newPosition = rectTransform.localPosition;
        newPosition.y += deltaY;
        rectTransform.localPosition = newPosition;

        if (rectTransform.localPosition.y > batasAtas)
        {
            rectTransform.localPosition = new Vector3(
                rectTransform.localPosition.x,
                posisiAwalY,
                rectTransform.localPosition.z
            );
        }

        // --- 2. LOGIKA TRANSPARANSI (Kode Baru) ---
        if (targetImage != null)
        {
            // Ambil warna saat ini
            Color warnaBaru = targetImage.color;

            // Ubah nilai Alpha (A) sesuai slider transparansi
            warnaBaru.a = transparansi;

            // Terapkan kembali ke gambar
            targetImage.color = warnaBaru;
        }
    }
}