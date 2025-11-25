using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class DamageEffect : MonoBehaviour
{
    public static DamageEffect Instance;

    [Header("Vignette UI Image")]
    public Image vignetteImage;      // Drag Image di sini

    [Header("Settings")]
    public float fadeOutSpeed = 2f;  // Semakin besar, semakin cepat hilang
    public float flashAlpha = 0.45f; // Transparansi merah saat muncul
    public float pulseScale = 1.04f; // Sedikit membesar saat flash

    private Coroutine currentRoutine;

    void Awake()
    {
        Instance = this;

        if (vignetteImage != null)
        {
            Color c = vignetteImage.color;
            c.a = 0;
            vignetteImage.color = c;
        }
    }

    public void TriggerFlash()
    {
        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        currentRoutine = StartCoroutine(FlashRoutine());
    }

    IEnumerator FlashRoutine()
    {
        // 1. Munculkan merah seketika
        Color c = vignetteImage.color;
        c.a = flashAlpha;
        vignetteImage.color = c;

        // Scaling pulse
        transform.localScale = Vector3.one * pulseScale;

        // 2. Fade out
        while (vignetteImage.color.a > 0.01f)
        {
            vignetteImage.color = Color.Lerp(
                vignetteImage.color,
                new Color(c.r, c.g, c.b, 0),
                Time.deltaTime * fadeOutSpeed
            );

            transform.localScale = Vector3.Lerp(
                transform.localScale,
                Vector3.one,
                Time.deltaTime * fadeOutSpeed
            );

            yield return null;
        }

        // Reset
        Color reset = vignetteImage.color;
        reset.a = 0;
        vignetteImage.color = reset;

        transform.localScale = Vector3.one;
    }
}
