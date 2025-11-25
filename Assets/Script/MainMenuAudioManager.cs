using UnityEngine;
using UnityEngine.Audio; // 🔥 INTEGRASI MIXER
using System.Collections;

public class MainMenuAudioManager : MonoBehaviour
{
    public static MainMenuAudioManager Instance; // Opsional, jika diperlukan di script lain.

    [Header("Audio Clips")]
    public AudioClip clickSound;
    public AudioClip bgmMusic;

    [Header("Mixer Groups")] // 🔥 BARU: Hubungkan ke grup BGM & SFX
    public AudioMixerGroup bgmGroup;
    public AudioMixerGroup sfxGroup;

    [Header("Settings")]
    [Range(0f, 1f)] public float clickVolume = 0.7f;
    [Range(0f, 1f)] public float bgmVolume = 0.5f;

    private AudioSource bgmSource;
    private AudioSource sfxSource;

    void Awake()
    {
        // Pastikan hanya ada satu instance dan persisten
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Buat 2 AudioSource: 1 untuk BGM, 1 untuk efek klik
        bgmSource = gameObject.AddComponent<AudioSource>();
        sfxSource = gameObject.AddComponent<AudioSource>();

        // Setup BGM
        if (bgmMusic != null)
        {
            bgmSource.clip = bgmMusic;
            bgmSource.volume = bgmVolume;
            bgmSource.loop = true;
            if (bgmGroup != null) bgmSource.outputAudioMixerGroup = bgmGroup; // HUBUNGKAN BGM
            bgmSource.Play();
        }

        // Setup SFX (Klik)
        sfxSource.playOnAwake = false;
        sfxSource.volume = clickVolume;
        if (sfxGroup != null) sfxSource.outputAudioMixerGroup = sfxGroup; // HUBUNGKAN SFX
    }

    public void PlayClick()
    {
        if (clickSound != null)
            sfxSource.PlayOneShot(clickSound, clickVolume);
    }

    // Opsional: Fade out BGM kalau mau transisi scene halus
    public void FadeOutBGM(float duration = 1.5f)
    {
        StartCoroutine(FadeOutCoroutine(duration));
    }

    private System.Collections.IEnumerator FadeOutCoroutine(float duration)
    {
        float startVol = bgmSource.volume;
        float t = 0f;

        while (t < duration)
        {
            bgmSource.volume = Mathf.Lerp(startVol, 0f, t / duration);
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        bgmSource.Stop();
        bgmSource.volume = startVol;
    }
}