using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Overlay Panels")]
    public GameObject settingsPanel;
    public GameObject creditsPanel;
    public GameObject howToPlayPanel;
    public GameObject quitConfirmationPanel; // <-- 1. Tambahkan referensi panel konfirmasi

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

    public void ClosePanel(GameObject panelToClose)
    {
        if (panelToClose != null)
        {
            panelToClose.SetActive(false);
        }
    }

    // 2. Modifikasi OnQuit() untuk HANYA memunculkan panel
    public void OnQuit()
    {
        if (quitConfirmationPanel != null)
        {
            quitConfirmationPanel.SetActive(true);
        }
        else
        {
            // Fallback jika panel tidak di-assign, langsung quit
            Debug.LogWarning("Quit Confirmation Panel not set. Quitting directly.");
            ConfirmQuit();
        }
    }

    // 3. Buat fungsi BARU untuk tombol "Yes"
    public void ConfirmQuit()
    {
        Debug.Log("Quitting application...");
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}