using UnityEngine;
using UnityEngine.SceneManagement;

public class UIBeatmapManager : MonoBehaviour
{
    public void OnBack()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
