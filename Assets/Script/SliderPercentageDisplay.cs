using UnityEngine;
using UnityEngine.UI;
using TMPro; // Asumsi kamu menggunakan TextMeshPro

[RequireComponent(typeof(Slider))]
public class SliderPercentageDisplay : MonoBehaviour
{

    public TextMeshProUGUI valueText;

    private Slider slider;

    void Awake()
    {
        slider = GetComponent<Slider>();
        // Tampilkan nilai awal saat game dimuat (yang sudah diload dari PlayerPrefs)
        UpdateText(slider.value);
    }

    void OnEnable()
    {
        // Subskripsi ke event perubahan nilai slider
        slider.onValueChanged.AddListener(UpdateText);
    }

    void OnDisable()
    {
        // Hapus subskripsi saat objek dimatikan
        slider.onValueChanged.RemoveListener(UpdateText);
    }

    // Fungsi ini menerima nilai float (0.0 hingga 1.0)
    public void UpdateText(float value)
    {
        if (valueText != null)
        {
            // Konversi nilai 0.0-1.0 menjadi 0-100 dan tambahkan simbol persentase
            int percentage = Mathf.RoundToInt(value * 100f);
            valueText.text = $"{percentage}%";
        }
    }
}