using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

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

    /// <summary>
    /// Called by SpawnNote when the song ends successfully.
    /// </summary>
    public void ShowLevelComplete(int finalScore, string beatmapName = "")
    {
        if (isVisible) return;
        isVisible = true;

        if (panel != null)
            panel.SetActive(true);

        // Don't freeze entire time system here — just pause gameplay logic elsewhere
        // Time.timeScale = 0f;

        // Update title & score text
        if (titleText != null)
            titleText.text = "SONG COMPLETE!";

        if (scoreText != null)
        {
            if (!string.IsNullOrEmpty(beatmapName))
                scoreText.text = $"{beatmapName}\nYour Score: {finalScore}";
            else
                scoreText.text = $"Your Score: {finalScore}";
        }
    }

    public void OnRetry()
    {
        // Ensure normal time before scene transition
        Time.timeScale = 1f;

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
        // ✅ Fix: Reset time BEFORE loading, and delay one frame to ensure Unity applies it.
        Time.timeScale = 1f;

        // Clear session data
        GameSession.Clear();

        StartCoroutine(LoadMainMenuDelayed());
    }

    private IEnumerator LoadMainMenuDelayed()
    {
        yield return null; // Wait one frame to fully restore time system
        SceneManager.LoadScene("MainMenu");
    }
}
