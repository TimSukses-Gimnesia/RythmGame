using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenuAudioManager : MonoBehaviour
{
    [Header("Audio Clips")]
    public AudioClip clickSound;
    public AudioClip bgmMusic;

    [Header("Settings")]
    public float clickVolume = 0.7f;
    public float bgmVolume = 0.5f;

    private AudioSource bgmSource;
    private AudioSource sfxSource;

    void Awake()
    {
        // Buat 2 AudioSource: 1 untuk BGM, 1 untuk efek klik
        bgmSource = gameObject.AddComponent<AudioSource>();
        sfxSource = gameObject.AddComponent<AudioSource>();

        // Setup BGM
        if (bgmMusic != null)
        {
            bgmSource.clip = bgmMusic;
            bgmSource.volume = bgmVolume;
            bgmSource.loop = true;
            bgmSource.Play();
        }

        // Setup SFX
        sfxSource.playOnAwake = false;
        sfxSource.volume = clickVolume;

        // Opsional: supaya tidak hancur kalau menu ganti scene
        DontDestroyOnLoad(gameObject);
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
