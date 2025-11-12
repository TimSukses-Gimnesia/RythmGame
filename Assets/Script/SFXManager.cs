using UnityEngine;

public class SFXManager : MonoBehaviour
{
    public static SFXManager Instance;

    [Header("Hit Sounds")]
    public AudioClip hitSound;        // untuk Perfect & Good
    public AudioClip missSound;       // untuk Miss
    public AudioClip comboBreakSound; // opsional tambahan
    [Range(0f, 1f)] public float volume = 0.8f;

    private AudioSource source;

    void Awake()
    {
        // Pastikan cuma ada 1 instance di game
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
