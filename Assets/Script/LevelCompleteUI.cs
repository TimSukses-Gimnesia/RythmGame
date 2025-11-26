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

    [Header("Sound Effects")]
    public AudioClip victorySFX;
    public AudioClip confirmSFX;
    public AudioClip hoverSFX;

    private AudioSource sfxAudio;
    private bool isVisible = false;

    void Start()
    {
        sfxAudio = gameObject.AddComponent<AudioSource>();

        if (panel != null)
            panel.SetActive(false);

        retryButton?.onClick.AddListener(OnRetry);
        mainMenuButton?.onClick.AddListener(OnMainMenu);
        highscoreButton?.onClick.AddListener(OpenHighscores);
        highscoreCloseButton?.onClick.AddListener(CloseHighscores);

        if (highscorePanel != null)
            highscorePanel.SetActive(false);
    }

    // ============================================================
    // SHOW RESULT PANEL
    // ============================================================
    public void ShowLevelComplete(long finalScore)
    {
        if (isVisible) return;
        isVisible = true;

        panel.SetActive(true);

        // Victory SFX
        if (victorySFX)
            sfxAudio.PlayOneShot(victorySFX);

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

    // ============================================================
    // RANK SPRITE SELECTOR
    // ============================================================
    void SetRankSprite(float acc)
    {
        if (rankSpriteHolder == null) return;

        if (acc >= 95f)
            rankSpriteHolder.sprite = rankSprite_S;
        else if (acc >= 90f)
            rankSpriteHolder.sprite = rankSprite_A;
        else if (acc >= 80f)
            rankSpriteHolder.sprite = rankSprite_B;
        else if (acc >= 70f)
            rankSpriteHolder.sprite = rankSprite_C;
        else
            rankSpriteHolder.sprite = rankSprite_D;

        rankSpriteHolder.preserveAspect = true; // supaya ukuran sprite sesuai texture
    }

    // ============================================================
    // MOTIVATIONAL TEXT
    // ============================================================
    string GetMotivationalFeedback(float acc)
    {
        if (acc >= 95f) return "S Rank! Absolute perfection!";
        if (acc >= 90f) return "Amazing job! You're in the A tier!";
        if (acc >= 80f) return "Great work! Solid B performance!";
        if (acc >= 70f) return "Nice! You're getting better!";
        return "Keep practicing — you can do this!";
    }

    // ============================================================
    // BUTTONS WITH DELAY (prevent SFX cutting off)
    // ============================================================
    public void OnRetry()
    {
        StartCoroutine(ReloadRoutine());
    }

    private IEnumerator ReloadRoutine()
    {
        if (confirmSFX) sfxAudio.PlayOneShot(confirmSFX);
        yield return new WaitForSecondsRealtime(0.15f);

        Time.timeScale = 1f;
        SceneManager.LoadScene("Gameplay");
    }

    public void OnMainMenu()
    {
        StartCoroutine(MainMenuRoutine());
    }

    private IEnumerator MainMenuRoutine()
    {
        if (confirmSFX) sfxAudio.PlayOneShot(confirmSFX);
        yield return new WaitForSecondsRealtime(0.15f);

        Time.timeScale = 1f;
        GameSession.Clear();
        SceneManager.LoadScene("MainMenu");
    }

    public void OpenHighscores()
    {
        StartCoroutine(OpenHSRoutine());
    }

    private IEnumerator OpenHSRoutine()
    {
        if (hoverSFX) sfxAudio.PlayOneShot(hoverSFX);
        yield return new WaitForSecondsRealtime(0.05f);

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

    public void CloseHighscores()
    {
        if (hoverSFX) sfxAudio.PlayOneShot(hoverSFX);
        highscorePanel.SetActive(false);
    }
}
