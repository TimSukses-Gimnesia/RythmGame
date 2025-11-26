using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class GameOverUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject panel;
    public Button retryButton;
    public Button mainMenuButton;

    // 🔥 BARU: Overlay Merah
    [Header("Game Over Overlay")]
    [Tooltip("Image UI yang akan menjadi overlay merah. Harus full screen.")]
    public Image redOverlay;
    [Tooltip("Durasi fade in/out overlay.")]
    public float fadeDuration = 0.5f;
    [Tooltip("Transparansi maksimum overlay merah (0-1).")]
    [Range(0f, 1f)]
    public float maxOverlayAlpha = 0.6f;

    [Header("Audio")]
    public AudioClip defeatSFX;    // efek saat panel muncul
    public AudioClip clickSFX;     // efek klik tombol

    private AudioSource sfxSource;
    private bool isVisible = false;
    private Coroutine fadeCoroutine; // 🔥 Untuk mengontrol coroutine overlay

    void Awake()
    {
        // Prevent duplicate UI
        var all = FindObjectsByType<GameOverUI>(FindObjectsSortMode.None);
        if (all.Length > 1)
        {
            Destroy(this);
            return;
        }

        // Auto-detect panel
        if (panel == null)
            panel = transform.Find("GameOverPanel")?.gameObject;

        // --- Create SFX Audio Source ---
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.loop = false;
        sfxSource.volume = 1f;
        sfxSource.ignoreListenerPause = true; // 🔥 Diperlukan agar suara bermain saat Time.timeScale = 0
    }

    void Start()
    {
        if (panel != null)
            panel.SetActive(false);

        // 🔥 BARU: Inisialisasi dan sembunyikan overlay di awal
        if (redOverlay != null)
        {
            redOverlay.gameObject.SetActive(false);
            Color originalColor = redOverlay.color;
            originalColor.a = 0f;
            redOverlay.color = originalColor;
        }

        retryButton?.onClick.AddListener(() =>
        {
            PlayClick();
            StartCoroutine(DelayedRetry());
        });

        mainMenuButton?.onClick.AddListener(() =>
        {
            PlayClick();
            StartCoroutine(DelayedMainMenu());
        });
    }

    // ============================================================
    // SHOW GAME OVER
    // ============================================================
    public void ShowGameOver(long _)
    {
        if (isVisible) return;
        isVisible = true;

        panel?.SetActive(true);

        // 🔥 BARU: Mulai efek overlay sebelum game di-pause
        if (redOverlay != null)
        {
            redOverlay.gameObject.SetActive(true);
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadeRedOverlay());
        }

        PlayDefeat();

        // 🔥 Tunda pause game sedikit agar SFX Defeat terdengar
        StartCoroutine(PauseGameAfterDelay(0.1f));
    }

    // 🔥 FUNGSI BARU: Coroutine untuk Fade Overlay
    IEnumerator FadeRedOverlay()
    {
        // Fade In
        float timer = 0f;
        while (timer < fadeDuration)
        {
            // 🔥 Gunakan unscaledDeltaTime karena Time.timeScale akan 0
            timer += Time.unscaledDeltaTime;
            Color color = redOverlay.color;
            color.a = Mathf.Lerp(0f, maxOverlayAlpha, timer / fadeDuration);
            redOverlay.color = color;
            yield return null;
        }

        // Pastikan alpha diatur tepat di maxOverlayAlpha
        Color finalInColor = redOverlay.color;
        finalInColor.a = maxOverlayAlpha;
        redOverlay.color = finalInColor;

        // Jeda sebentar pada transparansi maksimum
        yield return new WaitForSecondsRealtime(0.5f);

        // Fade Out
        timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            Color color = redOverlay.color;
            color.a = Mathf.Lerp(maxOverlayAlpha, 0f, timer / fadeDuration);
            redOverlay.color = color;
            yield return null;
        }

        // Pastikan alpha diatur tepat di 0
        Color finalOutColor = redOverlay.color;
        finalOutColor.a = 0f;
        redOverlay.color = finalOutColor;
        redOverlay.gameObject.SetActive(false); // Sembunyikan setelah fade out
    }

    // 🔥 FUNGSI BARU: Coroutine untuk menunda pause game
    IEnumerator PauseGameAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        Time.timeScale = 0f;
    }


    // ============================================================
    // AUDIO
    // ============================================================
    void PlayClick()
    {
        if (clickSFX != null)
            sfxSource.PlayOneShot(clickSFX);
    }

    void PlayDefeat()
    {
        if (defeatSFX != null)
            sfxSource.PlayOneShot(defeatSFX);
    }

    // ============================================================
    // BUTTON ACTIONS (with delay)
    // ============================================================
    IEnumerator DelayedRetry()
    {
        // 🔥 Hentikan overlay saat aksi dimulai
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        if (redOverlay != null) redOverlay.gameObject.SetActive(false);

        yield return new WaitForSecondsRealtime(0.15f);

        Time.timeScale = 1f;
        SceneManager.LoadScene("Gameplay");
    }

    IEnumerator DelayedMainMenu()
    {
        // 🔥 Hentikan overlay saat aksi dimulai
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        if (redOverlay != null) redOverlay.gameObject.SetActive(false);

        yield return new WaitForSecondsRealtime(0.15f);

        Time.timeScale = 1f;
        GameSession.Clear();
        SceneManager.LoadScene("MainMenu");
    }
}