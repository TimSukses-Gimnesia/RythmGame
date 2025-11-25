using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class VolumeSettings : MonoBehaviour
{
    // 🔥 1. POLA PERSISTENSI SINGLETON
    public static VolumeSettings Instance;

    // HUBUNGKAN INI KE ASSET MainAudioMixer DI INSPECTOR
    public AudioMixer mainMixer;

    [Header("UI Sliders")]
    public Slider musicSlider;
    public Slider sfxSlider;
    public Slider bgmSlider;

    // Nama Parameter yang Diekspos (Harus SAMA PERSIS!)
    private const string MUSIC_PARAM = "MusicVolume";
    private const string SFX_PARAM = "SFXVolume";
    private const string BGM_PARAM = "BGMVolume";

    void Awake()
    {
        // Terapkan Singleton Pattern untuk Persistensi
        if (Instance != null)
        {
            Destroy(gameObject); // Hancurkan duplikat
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // 🔥 JANGAN HANCURKAN SAAT SCENE BERGANTI

        // Muat setting volume yang tersimpan dan atur posisi Slider UI
        LoadAllVolumesAndSetupUI();
    }

    // Fungsi utama yang dipanggil saat game dimulai untuk memuat nilai
    private void LoadAllVolumesAndSetupUI()
    {
        // Panggil fungsi setup untuk setiap kategori
        SetupSliderAndMixer(MUSIC_PARAM, musicSlider);
        SetupSliderAndMixer(SFX_PARAM, sfxSlider);
        SetupSliderAndMixer(BGM_PARAM, bgmSlider);
    }

    // 🔥 FUNGSI INTI: Memuat nilai dan Mendorongnya ke Mixer DAN Slider UI
    private void SetupSliderAndMixer(string paramName, Slider slider)
    {
        // 1. Muat nilai yang tersimpan (default 1.0f)
        float savedValue = PlayerPrefs.GetFloat(paramName, 1f);

        // 2. Terapkan ke Audio Mixer (Mengubah Level Suara)
        ApplyVolumeToMixer(paramName, savedValue);

        // 3. Terapkan ke Slider UI (Mengubah Tampilan Bar)
        if (slider != null)
        {
            // SetValueWithoutNotify mencegah slider memicu OnValueChanged saat inisialisasi.
            slider.SetValueWithoutNotify(savedValue);
        }
    }

    // =======================================================
    // 🖱️ PUBLIC METHODS (Dihubungkan ke Event On Value Changed)
    // =======================================================

    public void SetBGMVolume(float sliderValue)
    {
        ApplyVolumeToMixer(BGM_PARAM, sliderValue);
    }

    public void SetSFXVolume(float sliderValue)
    {
        ApplyVolumeToMixer(SFX_PARAM, sliderValue);
    }

    public void SetMusicVolume(float sliderValue)
    {
        ApplyVolumeToMixer(MUSIC_PARAM, sliderValue);
    }

    // =======================================================
    // ⚙️ CORE LOGIC
    // =======================================================

    // Fungsi Inti: Mengkonversi nilai slider (0-1) ke Desibel (dB) dan menerapkannya
    private void ApplyVolumeToMixer(string parameterName, float sliderValue)
    {
        // Konversi nilai linear (0-1) menjadi skala Desibel (-80dB hingga 0dB)
        float volumeDb = Mathf.Log10(Mathf.Max(0.0001f, sliderValue)) * 20f;

        if (mainMixer != null)
        {
            mainMixer.SetFloat(parameterName, volumeDb);
        }

        // Simpan nilai linear float (0-1) untuk persistence
        PlayerPrefs.SetFloat(parameterName, sliderValue);
        PlayerPrefs.Save();
    }
}