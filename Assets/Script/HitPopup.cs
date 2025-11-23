using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class HitPopup : MonoBehaviour
{
    [Header("UI References")]
    public Image displayImage;

    [Header("Animation Settings")]
    public float moveSpeed = 50f;
    public float fadeOutTime = 0.5f;
    public float popScale = 1.2f;

    private Vector3 originalScale;

    void Awake()
    {
        if (displayImage == null) displayImage = GetComponent<Image>();
        originalScale = transform.localScale;
    }

    public void Setup(Sprite sprite)
    {
        if (displayImage == null) return;

        displayImage.sprite = sprite;
        displayImage.SetNativeSize();

        // Efek membesar di awal
        transform.localScale = originalScale * popScale;

        // Mulai animasi
        StartCoroutine(AnimateAndDestroy());
    }

    private IEnumerator AnimateAndDestroy()
    {
        float timer = 0f;
        Color startColor = displayImage.color;

        while (timer < fadeOutTime)
        {
            // Gerak ke atas
            transform.Translate(Vector3.up * moveSpeed * Time.deltaTime);

            // Animasi Scale (mengecil kembali normal)
            transform.localScale = Vector3.Lerp(originalScale * popScale, originalScale, timer / fadeOutTime);

            // Fade Out
            float alpha = 1.0f - (timer / fadeOutTime);
            displayImage.color = new Color(startColor.r, startColor.g, startColor.b, alpha);

            timer += Time.deltaTime;
            yield return null;
        }

        // HANCURKAN OBJECT SETELAH SELESAI
        Destroy(gameObject);
    }
}