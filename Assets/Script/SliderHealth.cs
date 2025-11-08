using UnityEngine;
using UnityEngine.UI; 

[RequireComponent(typeof(Slider))]
public class SliderHealth : MonoBehaviour
{
    public Slider healthSlider;
    private PlayerMovement player; 

    void Start()
    {
        healthSlider = GetComponent<Slider>();

        // Cari script PlayerMovement di scene
        player = FindFirstObjectByType<PlayerMovement>();

        if (player != null)
        {
            // Atur nilai maksimum slider sesuai maxHealth dari Player
            healthSlider.maxValue = player.maxHealth;
            healthSlider.value = player.maxHealth;
        }
        else
        {
            Debug.LogError("HealthBarUI tidak bisa menemukan script PlayerMovement!");
        }
    }

    void Update()
    {
        // Update value slider setiap frame sesuai health statis
        if (player != null)
        {
            healthSlider.value = HitJudgement.health;
        }
    }
}