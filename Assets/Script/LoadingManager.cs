using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class LoadingManager : MonoBehaviour
{
    [Header("Setup")]
    public GameObject loadingScreenPrefab;
    public float minimumWaitTime = 1.0f;

    public void LoadLevel(string sceneName)
    {
        StartCoroutine(LoadAsynchronously(sceneName));
    }

    IEnumerator LoadAsynchronously(string sceneName)
    {
        // --- PERBAIKAN 1: Selamatkan diri sendiri dulu! ---
        // Agar script ini tidak mati saat scene lama dihapus
        DontDestroyOnLoad(this.gameObject);

        // 1. Munculkan Loading Screen
        GameObject loaderCanvas = Instantiate(loadingScreenPrefab);
        DontDestroyOnLoad(loaderCanvas); // Canvas juga diselamatkan

        // 2. Mulai Load Scene
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false;

        // 3. Pause Game
        Time.timeScale = 0f;
        AudioListener.pause = true;

        float timer = 0f;

        // Loop tunggu loading & timer
        while (operation.progress < 0.9f || timer < minimumWaitTime)
        {
            timer += Time.unscaledDeltaTime;
            yield return null;
        }

        Debug.Log("Syarat selesai. Pindah scene...");

        operation.allowSceneActivation = true;

        
        while (!operation.isDone)
        {
            yield return null;
        }

     
        Time.timeScale = 1f;
        AudioListener.pause = false;

        if (loaderCanvas != null)
        {
            Destroy(loaderCanvas);
        }

        // --- PERBAIKAN 2: Bunuh diri setelah tugas selesai ---
        // Karena kita sudah membuat objek ini 'abadi' di awal, 
        // kita harus menghancurkannya manual agar tidak menumpuk/sampah.
        Destroy(this.gameObject);
    }
}