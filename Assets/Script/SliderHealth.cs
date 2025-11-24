using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class SliderHealth : MonoBehaviour
{

    public Slider healthSlider;
    private PlayerMovement player;

    [Header("Animation")]
    [Tooltip("Berapa cepat bar bergerak (poin health per detik)")]
    public float animationSpeed = 50f; // Kecepatan animasi bar

    void Start()
    {
        healthSlider = GetComponent<Slider>();

        // Cari script PlayerMovement di scene
        player = FindFirstObjectByType<PlayerMovement>();

        if (player != null)
        {
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
        
        if (player == null) return;

        //mengambil damage sesuai note
        float targetValue = HitJudgement.health;

      

        healthSlider.value = targetValue;
    }
}