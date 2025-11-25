using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class KiaiEffectManager : MonoBehaviour
{
    [Header("References")]
    // 🔥 BISA TARIK SALAH SATU (Image atau SpriteRenderer)
    public Image backgroundImageUI;        // Jika pakai UI Image
    public SpriteRenderer backgroundSprite; // Jika pakai Sprite Renderer

    public Image flashOverlay;
    public Slider songProgressBar;
    public RectTransform progressBarFill;

    [Header("Background Colors")]
    public Color normalColor = Color.white;
    public Color kiaiColor = new Color(1f, 0.6f, 0.8f);
    public float colorTransitionSpeed = 2f;

    [Header("Progress Bar Settings")]
    public Color barNormalColor = Color.white;
    public Color barKiaiColor = new Color(0f, 0.8f, 1f);
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

        if (flashOverlay != null)
        {
            flashOverlay.color = new Color(1f, 1f, 1f, 0f);
            flashOverlay.raycastTarget = false;
        }
    }

    public void SetupTiming(List<OsuBeatmapLoader.TimingPoint> points)
    {
        timingPoints = points;
        if (timingPoints != null)
            timingPoints.Sort((a, b) => a.timeSec.CompareTo(b.timeSec));
    }

    void Update()
    {
        if (timingPoints == null || timingPoints.Count == 0 || spawner == null) return;

        double songTime = AudioSettings.dspTime - spawner.songStartDspTime;

        CheckKiaiState((float)songTime);
        UpdateBackground();
        UpdateProgressBar();
    }

    void CheckKiaiState(float time)
    {
        bool currentKiai = false;
        for (int i = 0; i < timingPoints.Count; i++)
        {
            if (time >= timingPoints[i].timeSec)
                currentKiai = timingPoints[i].isKiai;
            else
                break;
        }

        if (currentKiai != isInKiai)
        {
            if (currentKiai) OnKiaiStart();
            else OnKiaiEnd();
            isInKiai = currentKiai;
        }
    }

    void OnKiaiStart()
    {
        if (flashOverlay != null) StartCoroutine(FlashEffect());
    }

    void OnKiaiEnd() { }

    void UpdateBackground()
    {
        // Tentukan warna target
        Color target = isInKiai ? kiaiColor : normalColor;

        // 🔥 LOGIKA BARU: Cek mana yang diisi, UI atau Sprite
        if (backgroundImageUI != null)
        {
            backgroundImageUI.color = Color.Lerp(backgroundImageUI.color, target, Time.deltaTime * colorTransitionSpeed);
        }

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

    System.Collections.IEnumerator FlashEffect()
    {
        float t = 0f;
        while (t < 0.5f)
        {
            float alpha = Mathf.Lerp(0.4f, 0f, t / 0.5f);
            flashOverlay.color = new Color(1f, 1f, 1f, alpha);
            t += Time.deltaTime;
            yield return null;
        }
        flashOverlay.color = new Color(1f, 1f, 1f, 0f);
    }
}