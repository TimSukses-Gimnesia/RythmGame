using UnityEngine;
using UnityEngine.Audio; // 🔥 INTEGRASI MIXER

public class SFXManager : MonoBehaviour
{
    public static SFXManager Instance;

    [Header("Sound Clips")]
    public AudioClip hitSound;
    public AudioClip missSound;
    public AudioClip comboBreakSound;
    [Range(0f, 1f)] public float volume = 0.8f;

    [Header("Mixer Group")] // 🔥 BARU: Untuk kontrol volume global SFX
    public AudioMixerGroup sfxGroup;

    private AudioSource source;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;

        source.volume = volume;

        // 🔥 HUBUNGKAN KE MIXER GROUP SFX
        if (sfxGroup != null)
        {
            source.outputAudioMixerGroup = sfxGroup;
        }
    }

    public void PlayHit()
    {
        if (hitSound != null)
            source.PlayOneShot(hitSound, volume);
    }

    public void PlayMiss()
    {
        if (missSound != null)
            source.PlayOneShot(missSound, volume);
    }

    public void PlayComboBreak()
    {
        if (comboBreakSound != null)
            source.PlayOneShot(comboBreakSound, volume);
    }
}