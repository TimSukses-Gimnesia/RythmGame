using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

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

    [Header("Statistics")]
    public TMP_Text perfectCountText;
    public TMP_Text goodCountText;
    public TMP_Text missCountText;

    [Header("Rank Sprite Display")]
    public Image rankSpriteHolder;
    public Sprite rankSprite_S;
    public Sprite rankSprite_A;
    public Sprite rankSprite_B;
    public Sprite rankSprite_C;
    public Sprite rankSprite_D;

    [Header("Buttons")]
    public Button retryButton;
    public Button mainMenuButton;
    public Button highscoreButton;
    public Button highscoreCloseButton;

    [Header("Highscore UI")]
    public GameObject highscorePanel;
    public TMP_Text highscoreText;

    [Header("Audio")]
    public AudioSource sfxSource;
    public AudioClip sfxClick;
    public AudioClip sfxVictory;

    private bool isVisible = false;

    void Start()
    {
        if (panel != null)
            panel.SetActive(false);

        retryButton.onClick.AddListener(OnRetryButton);
        mainMenuButton.onClick.AddListener(OnMainMenuButton);
        highscoreButton.onClick.AddListener(OnHighscoreButton);
        highscoreCloseButton.onClick.AddListener(OnHighscoreCloseButton);

        if (highscorePanel != null)
            highscorePanel.SetActive(false);
    }

    public void ShowLevelComplete(long finalScore)
    {
        if (isVisible) return;
        isVisible = true;

        panel.SetActive(true);

        // Play victory sound
        if (sfxSource && sfxVictory)
            sfxSource.PlayOneShot(sfxVictory);

        titleText.text = "SONG COMPLETE!";
        scoreText.text = finalScore.ToString("N0");

        float acc = HitJudgement.GetAccuracy();
        accuracyText.text = $"{acc:F2}%";
        accuracySlider.value = acc / 100f;

        perfectCountText.text = HitJudgement.countPerfect.ToString();
        goodCountText.text = HitJudgement.countGood.ToString();
        missCountText.text = HitJudgement.countMiss.ToString();

        SetRankSprite(acc);

        motivationalText.text = GetMotivationalFeedback(acc);

        HighscoreManager.AddScore(GameSession.SelectedBeatmapName, finalScore);
    }

    void SetRankSprite(float acc)
    {
        if (rankSpriteHolder == null) return;

        if (acc >= 95f) rankSpriteHolder.sprite = rankSprite_S;
        else if (acc >= 90f) rankSpriteHolder.sprite = rankSprite_A;
        else if (acc >= 80f) rankSpriteHolder.sprite = rankSprite_B;
        else if (acc >= 70f) rankSpriteHolder.sprite = rankSprite_C;
        else rankSpriteHolder.sprite = rankSprite_D;

        rankSpriteHolder.preserveAspect = true;
    }

    string GetMotivationalFeedback(float acc)
    {
        if (acc >= 95f) return "S Rank Achieved! Perfect synchronization!";
        if (acc >= 90f) return "Amazing job! You're in the A tier!";
        if (acc >= 80f) return "Great work! Solid B performance!";
        if (acc >= 70f) return "Nice! You're getting better!";
        return "Keep practicing — you can do this!";
    }

    // ===========================================================
    // BUTTON IMPLEMENTATION (Click + Action inside same function)
    // ===========================================================

    void PlayClick()
    {
        if (sfxSource && sfxClick)
            sfxSource.PlayOneShot(sfxClick);
    }

    public void OnRetryButton()
    {
        PlayClick();
        StartCoroutine(LoadGameplayDelayed());
    }

    IEnumerator LoadGameplayDelayed()
    {
        yield return new WaitForSecondsRealtime(0.22f);
        SceneManager.LoadScene("Gameplay");
    }

    public void OnMainMenuButton()
    {
        PlayClick();
        StartCoroutine(LoadMainMenuDelayed());
    }

    IEnumerator LoadMainMenuDelayed()
    {
        yield return new WaitForSecondsRealtime(0.22f);
        GameSession.Clear();
        SceneManager.LoadScene("MainMenu");
    }

    public void OnHighscoreButton()
    {
        PlayClick();
        OpenHighscores();
    }

    public void OnHighscoreCloseButton()
    {
        PlayClick();
        CloseHighscores();
    }

    // ===========================================================

    void OpenHighscores()
    {
        highscorePanel.SetActive(true);

        var list = HighscoreManager.LoadTop3(GameSession.SelectedBeatmapName);

        if (list.Count == 0)
        {
            highscoreText.text = "Belum ada skor.";
        }
        else
        {
            highscoreText.text = "";
            for (int i = 0; i < list.Count; i++)
                highscoreText.text += $"{i + 1}. {list[i].score:N0} — {list[i].date}\n";
        }
    }

    void CloseHighscores()
    {
        highscorePanel.SetActive(false);
    }
}