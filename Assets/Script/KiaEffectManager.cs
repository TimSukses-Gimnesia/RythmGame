using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class KiaiEffectManager : MonoBehaviour
{
    [Header("References")]
    // 🔥 BISA TARIK SALAH SATU (Image atau SpriteRenderer)
    public Image backgroundImageUI;         // Jika pakai UI Image
    public SpriteRenderer backgroundSprite; // Jika pakai Sprite Renderer

    public Image flashOverlay;
    public Slider songProgressBar;
    public RectTransform progressBarFill;

    [Header("Background Colors")]
    public Color normalColor = Color.white;
    public Color kiaiColor = new Color(1f, 0.6f, 0.8f); // Pink saat Reff
    public float colorTransitionSpeed = 2f;

    [Header("Progress Bar Settings")]
    public Color barNormalColor = Color.white;
    public Color barKiaiColor = new Color(0f, 0.8f, 1f); // Biru Neon
    public float normalHeight = 10f;
    public float kiaiHeight = 25f;

    private List<OsuBeatmapLoader.TimingPoint> timingPoints;
    private bool isInKiai = false;
    private Image progressBarFillImage;
    private SpawnNote spawner;

    void Start()
    {
        spawner = FindFirstObjectByType<SpawnNote>();

        if (progressBarFill != null)
            progressBarFillImage = progressBarFill.GetComponent<Image>();

        // Pastikan flash mati dan transparan di awal
        if (flashOverlay != null)
        {
            flashOverlay.color = new Color(1f, 1f, 1f, 0f);
            flashOverlay.raycastTarget = false;
        }
    }

    // Dipanggil oleh SpawnNote setelah load chart
    public void SetupTiming(List<OsuBeatmapLoader.TimingPoint> points)
    {
        timingPoints = points;
        if (timingPoints != null)
            timingPoints.Sort((a, b) => a.timeSec.CompareTo(b.timeSec));
    }

    // 🔥 FUNGSI PENTING: Dipanggil saat Game Over agar layar tidak putih
    public void StopKiaiImmediate()
    {
        StopAllCoroutines(); // Matikan animasi flash

        // Reset Flash jadi transparan
        if (flashOverlay != null)
            flashOverlay.color = new Color(1f, 1f, 1f, 0f);

        // Reset Warna Background
        if (backgroundImageUI != null) backgroundImageUI.color = normalColor;
        if (backgroundSprite != null) backgroundSprite.color = normalColor;

        // Reset Progress Bar
        if (progressBarFillImage != null) progressBarFillImage.color = barNormalColor;
        if (progressBarFill != null)
        {
            Vector2 size = progressBarFill.sizeDelta;
            size.y = normalHeight;
            progressBarFill.sizeDelta = size;
        }

        isInKiai = false;
    }

    void Update()
    {
        if (timingPoints == null || timingPoints.Count == 0 || spawner == null) return;

        // Jangan update jika game sedang pause/game over (TimeScale 0), 
        // KECUALI jika kita ingin animasi UI tetap jalan.
        // Tapi untuk sinkronisasi lagu, kita pakai waktu lagu.

        double songTime = AudioSettings.dspTime - spawner.songStartDspTime;

        CheckKiaiState((float)songTime);
        UpdateBackground();
        UpdateProgressBar();
    }

    void CheckKiaiState(float time)
    {
        bool currentKiai = false;
        // Loop untuk mencari timing point aktif saat ini
        for (int i = 0; i < timingPoints.Count; i++)
        {
            if (time >= timingPoints[i].timeSec)
            {
                currentKiai = timingPoints[i].isKiai;
            }
            else
            {
                break; // Karena sudah urut, stop jika ketemu masa depan
            }
        }

        // Deteksi perubahan state (Masuk/Keluar Reff)
        if (currentKiai != isInKiai)
        {
            if (currentKiai) OnKiaiStart();
            else OnKiaiEnd();

            isInKiai = currentKiai;
        }
    }

    void OnKiaiStart()
    {
        // Mulai Flash
        if (flashOverlay != null) StartCoroutine(FlashEffect());
    }

    void OnKiaiEnd() { }

    void UpdateBackground()
    {
        Color target = isInKiai ? kiaiColor : normalColor;

        // Update UI Image
        if (backgroundImageUI != null)
        {
            backgroundImageUI.color = Color.Lerp(backgroundImageUI.color, target, Time.deltaTime * colorTransitionSpeed);
        }

        // Update Sprite Renderer
        if (backgroundSprite != null)
        {
            backgroundSprite.color = Color.Lerp(backgroundSprite.color, target, Time.deltaTime * colorTransitionSpeed);
        }
    }

    void UpdateProgressBar()
    {
        if (progressBarFill == null || progressBarFillImage == null) return;

        Color targetCol = isInKiai ? barKiaiColor : barNormalColor;
        progressBarFillImage.color = Color.Lerp(progressBarFillImage.color, targetCol, Time.deltaTime * 5f);

        Vector2 size = progressBarFill.sizeDelta;
        float targetH = isInKiai ? kiaiHeight : normalHeight;
        size.y = Mathf.Lerp(size.y, targetH, Time.deltaTime * 5f);
        progressBarFill.sizeDelta = size;
    }

    // 🔥 ANIMASI FLASH (Menggunakan unscaledDeltaTime agar tidak beku saat Game Over)
    System.Collections.IEnumerator FlashEffect()
    {
        float t = 0f;
        float duration = 0.5f; // Durasi flash

        while (t < duration)
        {
            float alpha = Mathf.Lerp(0.4f, 0f, t / duration); // Dari 40% ke 0%

            if (flashOverlay != null)
                flashOverlay.color = new Color(1f, 1f, 1f, alpha);

            // Gunakan unscaledDeltaTime agar tetap jalan meski Time.timeScale = 0
            t += Time.unscaledDeltaTime;

            yield return null;
        }

        // Pastikan bersih di akhir
        if (flashOverlay != null)
            flashOverlay.color = new Color(1f, 1f, 1f, 0f);
    }
}