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
        // Pastikan player ada
        if (player == null) return;

        float targetValue = HitJudgement.health;

        float currentValue = healthSlider.value;

       
        float newSliderValue = Mathf.MoveTowards(
            currentValue,
            targetValue,
            animationSpeed * Time.deltaTime 
        );

        healthSlider.value = newSliderValue;
    }
}