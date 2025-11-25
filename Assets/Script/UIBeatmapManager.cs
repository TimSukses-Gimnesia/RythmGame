using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIBeatmapManager : MonoBehaviour
{
    public void OnBack()
    {
        StartCoroutine(BackRoutine());
    }

    IEnumerator BackRoutine()
    {
        // Cari audio manager
        var audioManager = FindAnyObjectByType<MainMenuAudioManager>();

        float waitTime = 0f;

        if (audioManager != null && audioManager.clickSound != null)
        {
            // Mainkan efek klik
            audioManager.PlayClick();

            // tunggu sesuai durasi clip
            waitTime = audioManager.clickSound.length;
        }

        // tunggu tanpa terpengaruh timeScale
        yield return new WaitForSecondsRealtime(waitTime);

        // pindah scene
        SceneManager.LoadScene("MainMenu");
    }
}
