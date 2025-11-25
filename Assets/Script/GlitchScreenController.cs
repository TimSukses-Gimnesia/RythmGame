using UnityEngine;
using UnityEngine.UI;

public class GlitchController : MonoBehaviour
{
    [Header("Pengaturan Gerak Objek")]
    public float kecepatanGerak = 500f; // Kecepatan gerak (bisa diatur di Inspector)
    public float batasAtas = 1000f;     // Batas Y di mana objek akan di-reset
    public float posisiAwalY = -500f;   // Posisi Y di mana objek akan muncul kembali

    private RectTransform rectTransform;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    void Update()
    {
        // 1. Hitung perpindahan vertikal
        float deltaY = kecepatanGerak * Time.deltaTime;

        // 2. Pindahkan objek Glitch ke atas (menambah posisi Y)
        Vector3 newPosition = rectTransform.localPosition;
        newPosition.y += deltaY;
        rectTransform.localPosition = newPosition;

        // 3. LOGIKA LOOPING: Cek apakah sudah mencapai batas atas
        if (rectTransform.localPosition.y > batasAtas)
        {
            // Jika sudah di atas batas, reset posisi Y kembali ke posisi awal
            rectTransform.localPosition = new Vector3(
                rectTransform.localPosition.x,  // Pertahankan posisi X
                posisiAwalY,                    // Ganti Y dengan posisi awal
                rectTransform.localPosition.z   // Pertahankan posisi Z
            );
        }
    }
}