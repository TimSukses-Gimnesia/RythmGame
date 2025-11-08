using UnityEngine;
using TMPro;
using System.Collections;

public class HitPopup : MonoBehaviour
{
    public TextMeshProUGUI textMesh;
    public float moveSpeed = 50f;     // Kecepatan gerak ke atas (pixel per detik)
    public float fadeOutTime = 0.5f;  // Waktu (detik) untuk menghilang

    private Color originalColor;
    private Vector3 originalPosition; // Posisi relatif terhadap parent (Canvas)

    void Awake()
    {
        if (textMesh == null)
        {
            textMesh = GetComponent<TextMeshProUGUI>();
        }

        // Simpan posisi & warna asli
        originalPosition = transform.localPosition; // Pakai localPosition untuk UI
        originalColor = textMesh.color;
    }

    // Fungsi ini akan dipanggil oleh HitJudgement saat di-spawn
    public void Setup(string text, Color color)
    {
        textMesh.text = text;
        textMesh.color = color;
    }

    // Saat object ini di-Enable (dihidupkan), mulai animasi
    void OnEnable()
    {
        // Reset ke posisi & warna asli sebelum mulai
        transform.localPosition = originalPosition;
        textMesh.color = new Color(originalColor.r, originalColor.g, originalColor.b, 1f); // Pastikan alpha 1

        // Mulai animasi
        StartCoroutine(AnimatePopup());
    }

    private IEnumerator AnimatePopup()
    {
        float timer = 0f;
        Color startColor = textMesh.color; // Ambil warna yang di-Setup (misal: Cyan)

        while (timer < fadeOutTime)
        {
            // 1. Bergerak ke atas
            transform.Translate(Vector3.up * moveSpeed * Time.deltaTime);

            // 2. Fade out (menghilang)
            float alpha = 1.0f - (timer / fadeOutTime);
            textMesh.color = new Color(startColor.r, startColor.g, startColor.b, alpha);

            timer += Time.deltaTime;
            yield return null;
        }

        // 3. Setelah selesai, matikan object
        gameObject.SetActive(false);
    }
}

