using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // Wajib ada untuk Slider

public class LevelCompleteUI : MonoBehaviour
{
    [Header("Main UI")]
    public GameObject panel;
    public TMP_Text titleText;
    public TMP_Text scoreText;

    [Header("Accuracy Display")]
    public TMP_Text accuracyText; // Teks Persen (98.5%)
    public Slider accuracySlider; // 🔥 BARU: Slider Bar untuk Visual Akurasi

    [Header("Statistics (Jumlah Hit)")]
    public TMP_Text perfectCountText;
    public TMP_Text goodCountText;
    public TMP_Text missCountText;

    [Header("Rank Display")]
    public TMP_Text rankText;

    [Header("Buttons")]
    public Button retryButton;
    public Button mainMenuButton;

    private bool isVisible = false;

    void Awake()
    {
        var all = FindObjectsByType<LevelCompleteUI>(FindObjectsSortMode.None);
        if (all.Length > 1) { Destroy(this); return; }
        if (panel == null) panel = transform.Find("LevelCompletePanel")?.gameObject;
    }

    void Start()
    {
        if (panel != null) panel.SetActive(false);
        if (retryButton != null) retryButton.onClick.AddListener(OnRetry);
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(OnMainMenu);
    }

    public void ShowLevelComplete(long finalScore)
    {
        if (isVisible) return;
        isVisible = true;

        if (panel != null) panel.SetActive(true);
        if (titleText != null) titleText.text = "SONG COMPLETE!";

        // 1. TAMPILKAN SCORE
        if (scoreText != null) scoreText.text = finalScore.ToString("N0");

        // 2. HITUNG AKURASI
        float accuracy = HitJudgement.GetAccuracy(); // Nilainya 0 - 100

        // Update Teks (98.5%)
        if (accuracyText != null) accuracyText.text = $"{accuracy:F2}%";

        // 🔥 UPDATE SLIDER BAR
        if (accuracySlider != null)
        {
            // Slider Unity nilainya 0.0 sampai 1.0
            // Jadi akurasi (0-100) harus dibagi 100
            accuracySlider.value = accuracy / 100f;
        }

        // 3. STATISTIK
        if (perfectCountText != null) perfectCountText.text = HitJudgement.countPerfect.ToString();
        if (goodCountText != null) goodCountText.text = HitJudgement.countGood.ToString();
        if (missCountText != null) missCountText.text = HitJudgement.countMiss.ToString();

        // 4. RANK TEKS
        if (rankText != null) SetRankText(accuracy);
    }

    void SetRankText(float acc)
    {
        if (acc >= 95f) { rankText.text = "S"; rankText.color = new Color(1f, 0.84f, 0f); }
        else if (acc >= 90f) { rankText.text = "A"; rankText.color = Color.green; }
        else if (acc >= 80f) { rankText.text = "B"; rankText.color = Color.cyan; }
        else if (acc >= 70f) { rankText.text = "C"; rankText.color = new Color(1f, 0.64f, 0f); }
        else { rankText.text = "D"; rankText.color = Color.gray; }
    }

    public void OnRetry()
    {
        Time.timeScale = 1f;
        if (!string.IsNullOrEmpty(GameSession.SelectedOsuFile)) SceneManager.LoadScene("Gameplay");
        else SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void OnMainMenu()
    {
        Time.timeScale = 1f;
        GameSession.Clear();
        SceneManager.LoadScene("MainMenu");
    }
}