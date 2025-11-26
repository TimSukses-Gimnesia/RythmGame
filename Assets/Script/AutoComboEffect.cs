using UnityEngine;
using UnityEngine.Rendering; // Wajib untuk akses Volume
using UnityEngine.Rendering.Universal; // Wajib untuk URP

public class AutoComboEffect: MonoBehaviour
{
    [Header("Settings")]
    public int comboThreshold = 50; // Batas combo
    public float smoothSpeed = 5f;  // Kecepatan transisi efek

    [Header("Intensity Settings")]
    [Range(0f, 1f)] public float maxChromaticAb = 0.8f; // Seberapa kuat efek warna RGB terpisah (0-1)
    [Range(-1f, 1f)] public float maxLensDistortion = -0.3f; // Seberapa melengkung layarnya (-0.3 bikin cembung dikit)

    [Header("References")]
    public Volume globalVolume; // Tarik Global Volume ke sini

    // Variabel internal untuk menyimpan settingan efek
    private ChromaticAberration chromatic;
    private LensDistortion distortion;

    void Start()
    {
        if (globalVolume == null)
        {
            // Coba cari otomatis jika lupa ditarik
            globalVolume = FindFirstObjectByType<Volume>();
        }

        if (globalVolume != null)
        {
            // Ambil komponen efek dari Volume Profile
            globalVolume.profile.TryGet(out chromatic);
            globalVolume.profile.TryGet(out distortion);
        }
    }

    void Update()
    {
        // Tentukan target nilai berdasarkan combo
        float targetChromo = 0f;
        float targetDistort = 0f;

        if (HitJudgement.combo >= comboThreshold)
        {
            // Jika Combo > 50, aktifkan efek
            targetChromo = maxChromaticAb;
            targetDistort = maxLensDistortion;
        }
        else
        {
            // Jika Combo putus, kembalikan ke 0 (normal)
            targetChromo = 0f;
            targetDistort = 0f;
        }

        // Terapkan animasi halus (Lerp)
        if (chromatic != null)
        {
            chromatic.intensity.value = Mathf.Lerp(chromatic.intensity.value, targetChromo, Time.deltaTime * smoothSpeed);
        }

        if (distortion != null)
        {
            distortion.intensity.value = Mathf.Lerp(distortion.intensity.value, targetDistort, Time.deltaTime * smoothSpeed);
        }
    }
}