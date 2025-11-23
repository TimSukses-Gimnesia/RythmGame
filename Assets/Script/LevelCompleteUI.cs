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

        // Highscore button
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

        // RANK
        if (rankText != null)
            SetRankText(accuracy);

        // Save highscore
        HighscoreManager.AddScore(GameSession.SelectedBeatmapName, finalScore);
    }

    void SetRankText(float acc)
    {
        if (acc >= 95f)
        {
            rankText.text = "S";
            rankText.color = new Color(1f, 0.84f, 0f);
        }
        else if (acc >= 90f)
        {
            rankText.text = "A";
            rankText.color = Color.green;
        }
        else if (acc >= 80f)
        {
            rankText.text = "B";
            rankText.color = Color.cyan;
        }
        else if (acc >= 70f)
        {
            rankText.text = "C";
            rankText.color = new Color(1f, 0.64f, 0f);
        }
        else
        {
            rankText.text = "D";
            rankText.color = Color.gray;
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

    // ---------------------------------------------------------
    // HIGH SCORE POPUP
    // ---------------------------------------------------------
    public void OpenHighscores()
    {
        if (highscorePanel == null || highscoreText == null)
            return;

        string map = GameSession.SelectedBeatmapName;
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
