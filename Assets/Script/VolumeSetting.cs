using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class VolumeSettings : MonoBehaviour
{
    public static VolumeSettings Instance;

    public AudioMixer mainMixer;

    [Header("UI Sliders")]
    public Slider musicSlider;
    public Slider sfxSlider;
    public Slider bgmSlider;

    private const string MUSIC_PARAM = "MusicVolume";
    private const string SFX_PARAM = "SFXVolume";
    private const string BGM_PARAM = "BGMVolume";

    void Awake()
    {
        // Singleton Setup
        if (Instance != null)
        {
            Instance.musicSlider = this.musicSlider;
            Instance.sfxSlider = this.sfxSlider;
            Instance.bgmSlider = this.bgmSlider;

            // PENTING: Panggil setup ulang di frame berikutnya agar aman
            Instance.StartCoroutine(Instance.DelayedSetup());

            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    void Start()
    {
        // Jalankan setup awal
        SetupSliders();
    }

    // Coroutine kecil untuk memberi jeda 1 frame saat pindah scene
    public System.Collections.IEnumerator DelayedSetup()
    {
        yield return null;
        SetupSliders();
    }

    public void SetupSliders()
    {
        // --- MUSIC SLIDER ---
        if (musicSlider != null)
        {
            musicSlider.onValueChanged.RemoveAllListeners();
            float savedVal = PlayerPrefs.GetFloat(MUSIC_PARAM, 1f);
            musicSlider.SetValueWithoutNotify(savedVal);
            ApplyVolumeToMixer(MUSIC_PARAM, savedVal); // Paksa Mixer ikut nilai ini
            musicSlider.onValueChanged.AddListener(SetMusicVolume);
        }

        // --- SFX SLIDER ---
        if (sfxSlider != null)
        {
            sfxSlider.onValueChanged.RemoveAllListeners();
            float savedVal = PlayerPrefs.GetFloat(SFX_PARAM, 1f);
            sfxSlider.SetValueWithoutNotify(savedVal);
            ApplyVolumeToMixer(SFX_PARAM, savedVal); // Paksa Mixer ikut nilai ini
            sfxSlider.onValueChanged.AddListener(SetSFXVolume);
        }

        // --- BGM SLIDER ---
        if (bgmSlider != null)
        {
            bgmSlider.onValueChanged.RemoveAllListeners();
            float savedVal = PlayerPrefs.GetFloat(BGM_PARAM, 1f);
            bgmSlider.SetValueWithoutNotify(savedVal);
            ApplyVolumeToMixer(BGM_PARAM, savedVal); // Paksa Mixer ikut nilai ini
            bgmSlider.onValueChanged.AddListener(SetBGMVolume);
        }
    }

    public void SetMusicVolume(float value) { ApplyVolumeToMixer(MUSIC_PARAM, value); }
    public void SetSFXVolume(float value) { ApplyVolumeToMixer(SFX_PARAM, value); }
    public void SetBGMVolume(float value) { ApplyVolumeToMixer(BGM_PARAM, value); }

    private void ApplyVolumeToMixer(string parameterName, float sliderValue)
    {
        // Rumus Logaritmik
        float volumeDb = Mathf.Log10(Mathf.Max(0.0001f, sliderValue)) * 20f;

        if (mainMixer != null)
        {
            mainMixer.SetFloat(parameterName, volumeDb);
        }

        PlayerPrefs.SetFloat(parameterName, sliderValue);
        PlayerPrefs.Save();
    }
}