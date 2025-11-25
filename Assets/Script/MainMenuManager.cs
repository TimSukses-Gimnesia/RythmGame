using UnityEngine;
using UnityEngine.SceneManagement;
// Mengelola transisi scene dan panel overlay.

public class MainMenuManager : MonoBehaviour
{
    [Header("Overlay Panels")]
    public GameObject settingsPanel;
    public GameObject creditsPanel;
    public GameObject howToPlayPanel;
    public GameObject quitConfirmationPanel;

    public void OnStartGame()
    {
        var audioManager = FindFirstObjectByType<MainMenuAudioManager>();
        if (audioManager != null)
            audioManager.FadeOutBGM(1.5f);
        SceneManager.LoadScene("BeatmapSelect");
    }

    public void OnSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
        }
    }

    public void OnCredits()
    {
        if (creditsPanel != null)
        {
            creditsPanel.SetActive(true);
        }
    }

    public void OnHowToPlay()
    {
        if (howToPlayPanel != null)
        {
            howToPlayPanel.SetActive(true);
        }
    }

    // Fungsi universal untuk menutup panel apa pun.
    public void ClosePanel(GameObject panelToClose)
    {
        if (panelToClose != null)
        {
            panelToClose.SetActive(false);
        }
    }

    public void OnQuit()
    {
        if (quitConfirmationPanel != null)
        {
            quitConfirmationPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("Quit Confirmation Panel not set. Quitting directly.");
            ConfirmQuit();
        }
    }

    public void ConfirmQuit()
    {
        Debug.Log("Quitting application...");
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}