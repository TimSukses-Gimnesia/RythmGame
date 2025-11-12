using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject panel;
    public TMP_Text scoreText;
    public Button retryButton;
    public Button mainMenuButton;

    private bool isVisible = false;

    void Awake()
    {
        // Prevent duplicate instances
        var all = FindObjectsByType<GameOverUI>(FindObjectsSortMode.None);
        if (all.Length > 1)
        {
            Destroy(this);
            return;
        }

        // Auto-find panel if missing
        if (panel == null)
            panel = transform.Find("GameOverPanel")?.gameObject;
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

    public void ShowGameOver(int finalScore)
    {
        if (isVisible) return;
        isVisible = true;

        if (panel != null)
            panel.SetActive(true);

        Time.timeScale = 0f;

        if (scoreText != null)
            scoreText.text = $"Your Score: {finalScore}";
    }

    public void OnRetry()
    {
        Time.timeScale = 1f;

        // Use GameSession to reload the last beatmap
        if (!string.IsNullOrEmpty(GameSession.SelectedOsuFile))
        {
            SceneManager.LoadScene("Gameplay");
        }
        else
        {
            Scene current = SceneManager.GetActiveScene();
            SceneManager.LoadScene(current.name);
        }
    }

    public void OnMainMenu()
    {
        Time.timeScale = 1f;

        // Clear session to avoid reloading the last beatmap next time
        GameSession.Clear();

        SceneManager.LoadScene("MainMenu");
    }
}
