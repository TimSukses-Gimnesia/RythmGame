using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class GameOverUI : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject panel;               // parent panel of the Game Over popup
    public TextMeshProUGUI scoreText;      // text showing player’s score
    public Button retryButton;
    public Button mainMenuButton;

    [Header("Scene Names")]
    public string mainMenuSceneName = "MainMenu";
    public string retrySceneName = "TestGamePlay"; // current scene to reload

    void Start()
    {
        if (panel != null)
            panel.SetActive(false); // hide initially

        if (retryButton != null)
            retryButton.onClick.AddListener(OnRetryPressed);

        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(OnMainMenuPressed);
    }

    public void ShowGameOver(int score)
    {
        if (panel != null)
            panel.SetActive(true);

        if (scoreText != null)
            scoreText.text = $"YOUR SCORE: {score}";

        // Freeze game
        Time.timeScale = 0f;
    }

    void OnRetryPressed()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(retrySceneName);
    }

    void OnMainMenuPressed()
    {
        Time.timeScale = 1f;
        // Empty for now — ready for integration later
        if (!string.IsNullOrEmpty(mainMenuSceneName))
            SceneManager.LoadScene(mainMenuSceneName);
        else
            Debug.Log("Main menu scene not set yet.");
    }
}
