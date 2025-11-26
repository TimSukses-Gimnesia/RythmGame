using UnityEngine;
using TMPro;
using System.Collections;

public class ScoreDisplay : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI comboText;

    [Header("Combo Pop Settings")]
    public float popScale = 1.5f;
    public float popDuration = 0.1f;

    [Header("Combo Colors")] // ?? PENGATURAN WARNA
    public Color colorNormal = Color.white;       // Combo 0-9
    public Color colorBronze = new Color(1f, 0.8f, 0.4f); // Combo 10+ (Kuning Pucat)
    public Color colorGold = new Color(1f, 0.84f, 0f);    // Combo 30+ (Emas)
    public Color colorPlatinum = new Color(0f, 1f, 1f);   // Combo 50+ (Cyan/Biru Muda)
    public Color colorUltimate = new Color(1f, 0f, 1f);   // Combo 100+ (Ungu/Pink)

    private int lastCombo = 0;
    private Vector3 originalComboScale;
    private Coroutine popCoroutine;

    void Start()
    {
        if (comboText != null)
        {
            originalComboScale = comboText.transform.localScale;
            comboText.enabled = false;

            // Pastikan alignment rata tengah lewat kode (opsional, lebih baik set di Inspector)
            comboText.alignment = TextAlignmentOptions.Center;
        }
        lastCombo = 0;
    }

    void Update()
    {
        // 1. Update Score
        if (scoreText != null)
        {
            scoreText.text = HitJudgement.score.ToString("N0");
        }

        // 2. Update Combo
        if (comboText != null)
        {
            if (HitJudgement.combo != lastCombo)
            {
                if (HitJudgement.combo > lastCombo && HitJudgement.combo > 1)
                {
                    // Format teks dengan Baris Baru (\n) agar angka di bawah
                    comboText.text = $"COMBO\n<size=150%>{HitJudgement.combo}</size>"; // Angka dibuat lebih besar sedikit

                    comboText.enabled = true;

                    //  UBAH WARNA BERDASARKAN JUMLAH COMBO
                    UpdateComboColor(HitJudgement.combo);

                    if (popCoroutine != null) StopCoroutine(popCoroutine);
                    popCoroutine = StartCoroutine(PopComboText());
                }
                else if (HitJudgement.combo == 0)
                {
                    comboText.enabled = false; // Sembunyikan jika combo putus
                }
                lastCombo = HitJudgement.combo;
            }
        }
    }

    void UpdateComboColor(int count)
    {
        if (count >= 100) comboText.color = colorUltimate;
        else if (count >= 50) comboText.color = colorPlatinum;
        else if (count >= 30) comboText.color = colorGold;
        else if (count >= 10) comboText.color = colorBronze;
        else comboText.color = colorNormal;
    }

    private IEnumerator PopComboText()
    {
        comboText.transform.localScale = originalComboScale * popScale;
        yield return new WaitForSeconds(popDuration);
        comboText.transform.localScale = originalComboScale;
        popCoroutine = null;
    }
}