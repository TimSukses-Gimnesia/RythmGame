using UnityEngine;
using UnityEngine.UI;

public class SongProgressUI : MonoBehaviour
{
    [Header("UI References")]
    public Slider progressBar;

    [Header("Dependencies")]
    public AudioSource audioSource;
    public SpawnNote spawner; 

 

    void Start()
    {
        if (progressBar == null) progressBar = GetComponent<Slider>();
        if (spawner == null) spawner = FindFirstObjectByType<SpawnNote>();

        if (audioSource == null && spawner != null)
            audioSource = spawner.GetComponent<AudioSource>();

        progressBar.value = 0;
        progressBar.interactable = false; 
    }

    void Update()
    {
    
        if (progressBar == null || audioSource == null || spawner == null) return;

        if (audioSource.clip == null) return;

        if (spawner.isGameOver) return;

        float totalDuration = audioSource.clip.length;
        if (totalDuration <= 0) return; 
        float currentTime = audioSource.time;
        float progress = currentTime / totalDuration;
        if (!audioSource.isPlaying && progress > 0.95f)
        {
            progressBar.value = 1f;
        }
        else
        {
            progressBar.value = progress;
        }
    }
}