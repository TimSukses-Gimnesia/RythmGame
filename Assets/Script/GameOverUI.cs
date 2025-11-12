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
        // Ensure only one GameOverUI exists
        var all = FindObjectsByType<GameOverUI>(FindObjectsSortMode.None);
        if (all.Length > 1)
        {
            Debug.LogWarning("[GameOverUI] Duplicate instance detected — removing extra.");
            Destroy(this);
            return;
        }

        // Try to auto-find panel if missing
        if (panel == null)
            panel = transform.Find("GameOverPanel")?.gameObject;

        if (panel == null)
            Debug.LogError("[GameOverUI] Panel reference is missing — assign it in the inspector!");
    }

    void Start()
    {
        // Hide panel safely on startup
        if (panel != null)
        {
            panel.SetActive(false);
            Debug.Log("[GameOverUI] Panel hidden on Start.");
        }

        // Hook up buttons (if not already)
        if (retryButton != null)
            retryButton.onClick.AddListener(OnRetry);
        else
            Debug.LogWarning("[GameOverUI] RetryButton not assigned.");

        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(OnMainMenu);
        else
            Debug.LogWarning("[GameOverUI] MainMenuButton not assigned.");
    }

    public void ShowGameOver(int finalScore)
    {
        if (isVisible)
        {
            Debug.Log("[GameOverUI] Already visible, ignoring duplicate ShowGameOver call.");
            return;
        }

        isVisible = true;
        Debug.Log($"[GameOverUI] ShowGameOver() called. Score = {finalScore}");

        // Activate the panel
        if (panel != null)
        {
            panel.SetActive(true);
            Debug.Log("[GameOverUI] Panel activated successfully.");
        }
        else
        {
            Debug.LogError("[GameOverUI] Panel reference is null!");
        }

        // Pause gameplay
        Time.timeScale = 0f;

        // Update score display
        if (scoreText != null)
            scoreText.text = $"Your Score: {finalScore}";
        else
            Debug.LogWarning("[GameOverUI] ScoreText reference is missing!");
    }

    public void OnRetry()
    {
        Debug.Log("[GameOverUI] Retry pressed — reloading current scene.");
        Time.timeScale = 1f;
        Scene current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.name);
    }

    public void OnMainMenu()
    {
        Debug.Log("[GameOverUI] Main Menu pressed — loading MainMenu scene.");
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}