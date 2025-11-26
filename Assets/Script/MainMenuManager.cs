using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections; // Wajib ada untuk IEnumerator

// Tambahkan ini agar otomatis punya AudioSource saat script dipasang
[RequireComponent(typeof(AudioSource))]
public class MainMenuManager : MonoBehaviour
{
    [Header("Overlay Panels")]
    public GameObject settingsPanel;
    public GameObject creditsPanel;
    public GameObject howToPlayPanel;
    public GameObject quitConfirmationPanel;

    [Header("Audio")] //  BARU: Slot untuk Audio Clip
    public AudioClip startGameSound;
    private AudioSource sfxSource;

    void Start()
    {
        // Ambil komponen AudioSource di objek ini
        sfxSource = GetComponent<AudioSource>();
    }

    public void OnStartGame()
    {
        // Panggil Coroutine agar ada jeda waktu sebelum pindah scene
        StartCoroutine(StartGameSequence());
    }

    IEnumerator StartGameSequence()
    {
        // 1. Mainkan Sound Effect Start
        if (sfxSource != null && startGameSound != null)
        {
            sfxSource.PlayOneShot(startGameSound);
        }

        // 2. Panggil Fade Out BGM (Logic Lama)
        var audioManager = FindFirstObjectByType<MainMenuAudioManager>();
        if (audioManager != null)
        {
            audioManager.FadeOutBGM(1.5f);
        }

        // 3. Tunggu sebentar (Misal 1 detik) agar suara terdengar dan transisi halus
        yield return new WaitForSeconds(1.0f);

        // 4. Baru Pindah Scene
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