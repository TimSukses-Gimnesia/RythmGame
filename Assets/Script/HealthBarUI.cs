using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    [Header("References")]
    public Image healthFillImage;
    public float smoothSpeed = 5f;

    private float maxHealthCached = 100f;
    private PlayerMovement player;

    void Start()
    {
        // 1. Ambil referensi player & Max HP
        player = FindFirstObjectByType<PlayerMovement>();
        if (player != null)
        {
            maxHealthCached = player.maxHealth;
        }
        else
        {
            Debug.LogError("[HealthBarUI] PlayerMovement tidak ditemukan! Default MaxHealth = 100.");
        }

        if (healthFillImage != null) healthFillImage.fillAmount = 1f;
    }

    void Update()
    {
        // Ambil health saat ini
        float currentHealth = HitJudgement.health;

        // Safety check: Update MaxHealth jika player baru ketemu (misal karena delay loading)
        if (player != null && maxHealthCached != player.maxHealth)
        {
            maxHealthCached = player.maxHealth;
        }

        // 2. Hitung Target Fill (0.0 - 1.0)
        float targetFillAmount = currentHealth / maxHealthCached;

        // 3. LOGIKA PAKSA NOL (SNAP TO ZERO)
        // Jika nyawa 0 atau kurang, LANGSUNG matikan gambarnya. Jangan pakai Lerp.
        if (currentHealth <= 0f)
        {
            healthFillImage.fillAmount = 0f;
        }
        else
        {
            // Jika nyawa masih ada, baru pakai animasi halus
            healthFillImage.fillAmount = Mathf.Lerp(healthFillImage.fillAmount, targetFillAmount, Time.deltaTime * smoothSpeed);
        }
    }
}