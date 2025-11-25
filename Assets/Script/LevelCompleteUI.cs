using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelCompleteUI : MonoBehaviour
{
    [Header("Main UI")]
    public GameObject panel;
    public TMP_Text titleText;
    public TMP_Text scoreText;

    [Header("Accuracy Display")]
    public TMP_Text accuracyText;
    public Slider accuracySlider;

    [Header("Feedback")]
    public TMP_Text motivationalText;

    [Header("Statistics (Jumlah Hit)")]
    public TMP_Text perfectCountText;
    public TMP_Text goodCountText;
    public TMP_Text missCountText;

    [Header("Rank Display")]
    public TMP_Text rankText;

    [Header("Buttons")]
    public Button retryButton;
    public Button mainMenuButton;

    [Header("Highscore UI")]
    public GameObject highscorePanel;
    public TMP_Text highscoreText;
    public Button highscoreCloseButton;
    public Button highscoreButton;

    // --- Definisi Warna ---
    private static readonly Color ColorS = new Color(1f, 0.84f, 0f);
    private static readonly Color ColorA = Color.green;
    private static readonly Color ColorB = Color.cyan;
    private static readonly Color ColorC = new Color(1f, 0.64f, 0f);
    private static readonly Color ColorD = Color.gray;
    private static readonly Color ColorMotivation = new Color(0.8f, 0.4f, 1f);

    private bool isVisible = false;

    void Awake()
    {
        var all = FindObjectsByType<LevelCompleteUI>(FindObjectsSortMode.None);
        if (all.Length > 1)
        {
            Destroy(this);
            return;
        }

        if (panel == null)
            panel = transform.Find("LevelCompletePanel")?.gameObject;
    }

    void Start()
    {
        if (panel != null) panel.SetActive(false);

        if (retryButton != null)
            retryButton.onClick.AddListener(OnRetry);

        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(OnMainMenu);

        if (highscoreButton != null)
            highscoreButton.onClick.AddListener(OpenHighscores);

        if (highscoreCloseButton != null)
            highscoreCloseButton.onClick.AddListener(CloseHighscores);

        if (highscorePanel != null)
            highscorePanel.SetActive(false);
    }

    public void ShowLevelComplete(long finalScore)
    {
        if (isVisible) return;
        isVisible = true;

        if (panel != null)
            panel.SetActive(true);

        if (titleText != null)
            titleText.text = "SONG COMPLETE!";

        // SCORE
        if (scoreText != null)
            scoreText.text = finalScore.ToString("N0");

        // ACCURACY
        float accuracy = HitJudgement.GetAccuracy();

        if (accuracyText != null)
            accuracyText.text = $"{accuracy:F2}%";

        if (accuracySlider != null)
            accuracySlider.value = accuracy / 100f;

        // STATISTICS
        if (perfectCountText != null)
            perfectCountText.text = HitJudgement.countPerfect.ToString();

        if (goodCountText != null)
            goodCountText.text = HitJudgement.countGood.ToString();

        if (missCountText != null)
            missCountText.text = HitJudgement.countMiss.ToString();

        // RANK & FEEDBACK
        if (rankText != null)
            SetRankText(accuracy);

        if (motivationalText != null)
            motivationalText.text = GetMotivationalFeedback(accuracy);

        // Save highscore
        HighscoreManager.AddScore(GameSession.SelectedBeatmapName, finalScore);
    }

    // 🔥 FUNGSI TELAH DIMODIFIKASI UNTUK MENGGUNAKAN ColorUtility LANGSUNG
    string GetMotivationalFeedback(float acc)
    {
        // Konversi warna ke string Hex RGB dengan ColorUtility
        string colorSHex = ColorUtility.ToHtmlStringRGB(ColorS);
        string colorMotivationHex = ColorUtility.ToHtmlStringRGB(ColorMotivation);
        string colorDHex = ColorUtility.ToHtmlStringRGB(ColorD);

        // Jika sudah Rank S
        if (acc >= 95f)
        {
            return $"<color=#{colorSHex}>S Rank Achieved! Perfect synchronization!</color>";
        }

        // Jika mendekati Rank S (95%)
        if (acc >= 94f)
        {
            float needed = 95f - acc;
            return $"<color=#{colorMotivationHex}>So close! You're only {needed:F2}% away from S Rank!</color>";
        }

        // Jika mendekati Rank A (90%)
        if (acc >= 85f)
        {
            float needed = 90f - acc;
            return $"<color=#{colorMotivationHex}>Keep going! Push {needed:F2}% more for Rank A.</color>";
        }

        // Jika Rank B ke bawah
        if (acc < 85f)
        {
            return $"<color=#{colorDHex}>Practice makes perfect. Focus on rhythm and timing!</color>";
        }

        return "";
    }

    void SetRankText(float acc)
    {
        if (acc >= 95f)
        {
            rankText.text = "S";
            rankText.color = ColorS;
        }
        else if (acc >= 90f)
        {
            rankText.text = "A";
            rankText.color = ColorA;
        }
        else if (acc >= 80f)
        {
            rankText.text = "B";
            rankText.color = ColorB;
        }
        else if (acc >= 70f)
        {
            rankText.text = "C";
            rankText.color = ColorC;
        }
        else
        {
            rankText.text = "D";
            rankText.color = ColorD;
        }
    }

    public void OnRetry()
    {
        Time.timeScale = 1f;

        if (!string.IsNullOrEmpty(GameSession.SelectedOsuFile))
            SceneManager.LoadScene("Gameplay");
        else
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void OnMainMenu()
    {
        Time.timeScale = 1f;
        GameSession.Clear();
        SceneManager.LoadScene("MainMenu");
    }

    // --- Highscore Popups ---
    public void OpenHighscores()
    {
        if (highscorePanel == null || highscoreText == null)
            return;

        string map = GameSession.SelectedBeatmapName;
        // Asumsi HighscoreManager ada
        var list = HighscoreManager.LoadTop3(map);

        if (list.Count == 0)
        {
            highscoreText.text = "Belum ada skor.";
        }
        else
        {
            highscoreText.text = "";
            for (int i = 0; i < list.Count; i++)
            {
                // Asumsi HighscoreManager mengembalikan objek dengan properti score dan date
                highscoreText.text += $"{i + 1}. {list[i].score:N0} — {list[i].date}\n";
            }
        }

        highscorePanel.SetActive(true);
    }

    public void CloseHighscores()
    {
        if (highscorePanel != null)
            highscorePanel.SetActive(false);
    }
}