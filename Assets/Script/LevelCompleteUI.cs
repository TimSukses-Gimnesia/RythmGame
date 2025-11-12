using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelCompleteUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject panel;
    public TMP_Text scoreText;
    public TMP_Text titleText;
    public Button retryButton;
    public Button mainMenuButton;

    private bool isVisible = false;

    void Awake()
    {
        // Prevent duplicate instances
        var all = FindObjectsByType<LevelCompleteUI>(FindObjectsSortMode.None);
        if (all.Length > 1)
        {
            Destroy(this);
            return;
        }

        // Auto-find panel if missing
        if (panel == null)
            panel = transform.Find("LevelCompletePanel")?.gameObject;
    }

    void Start()
    {
        if (panel != null)
            panel.SetActive(false);

        if (retryButton != null)
            retryButton.onClick.AddListener(OnRetry);

        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(OnMainMenu);
    }

    public void ShowLevelComplete(int finalScore)
    {
        if (isVisible) return;
        isVisible = true;

        if (panel != null)
            panel.SetActive(true);

        // ❌ Jangan pakai Time.timeScale = 0
        // Cukup hentikan gameplay di SpawnNote

        if (titleText != null)
            titleText.text = "SONG COMPLETE!";

        if (scoreText != null)
            scoreText.text = $"Your Score: {finalScore}";
    }

    public void OnRetry()
    {
        // Pastikan waktu normal
        Time.timeScale = 1f;

        if (!string.IsNullOrEmpty(GameSession.SelectedOsuFile))
            SceneManager.LoadScene("Gameplay");
        else
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void OnMainMenu()
    {
        // Pastikan waktu normal sebelum ganti scene
        Time.timeScale = 1f;

        GameSession.Clear();
        SceneManager.LoadScene("MainMenu");
    }
}