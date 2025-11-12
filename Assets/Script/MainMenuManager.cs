using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    public void OnStartGame()
    {
        SceneManager.LoadScene("BeatmapSelect");
    }

    public void OnSettings()
    {
        // nanti bisa load popup atau scene Settings
        Debug.Log("Settings menu belum diimplementasikan.");
    }

    public void OnCredits()
    {
        // tampilkan credit UI
        Debug.Log("Credits menu belum diimplementasikan.");
    }

    public void OnHowToPlay()
    {
        // tampilkan tutorial UI
        Debug.Log("How To Play belum diimplementasikan.");
    }

    public void OnQuit()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
