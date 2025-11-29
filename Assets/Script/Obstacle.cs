using UnityEngine;

public class Obstacle : MonoBehaviour
{
    [Header("Movement Timing")]
    public float hitTime;           // waktu saat obstacle mencapai target (beat)
    public Vector3 spawnPos;
    public Vector3 targetPos;
    public float travelDuration = 2f;
    public float speed = 1f;

    [Header("Extra Settings")]
    public float postTargetSpeedMultiplier = 1.3f;  // kecepatan setelah melewati target
    public float destroyDelayAfterOut = 0.5f;       // delay kecil sebelum destroy
    public float viewportMargin = 0.25f;            // margin area kamera sebelum dianggap "keluar"

    [Header("Damage Settings")]
    public float damage = 50f;

    // --- VISUAL ENHANCEMENT & ROCKET VISUALS ---
    [Header("Visual Enhancement (Sharp & Vibrant)")]
    [Range(1.0f, 5.0f)]
    public float colorBrightnessMultiplier = 2.5f;
    [Range(0.0f, 1.0f)]
    public float saturationBoost = 0.5f;

    [Header("Rocket Visuals")]
    [Tooltip("Particle System yang berperan sebagai pendorong roket. HARUS dihubungkan di Inspector.")]
    public ParticleSystem thrusterParticles;
    [Tooltip("Sesuaikan offset rotasi roket (mis. -90 jika model menghadap ke atas).")]
    public float rotationOffset = -90f;

    private const float SelfIlluminationFactor = 4.0f;
    private Renderer obstacleRenderer;
    private Material obstacleMaterial;
    private Color originalColor;


    private double songStartDspTime;
    private bool hasReachedTarget = false;
    private Vector3 moveDir;
    private Camera mainCam;
    private bool isOutOfView = false;

    void Start()
    {
        var spawner = FindFirstObjectByType<SpawnNote>();
        songStartDspTime = spawner != null ? spawner.songStartDspTime : AudioSettings.dspTime;

  
        obstacleRenderer = GetComponent<Renderer>();
        if (obstacleRenderer != null)
        {
            obstacleMaterial = obstacleRenderer.material;

            if (obstacleMaterial.HasProperty("_Color"))
            {
                originalColor = obstacleMaterial.color;
            }

            ApplyVibrancyAndSelfIllumination();
        }


        transform.position = spawnPos;
        moveDir = (targetPos - spawnPos).normalized;
        mainCam = Camera.main;

        // Atur orientasi agar objek menghadap ke arah pergerakan (seperti roket)
        SetupRocketOrientation();
    }

    void Update()
    {
        if (mainCam == null) mainCam = Camera.main;

        if (!hasReachedTarget)
        {
            MoveTowardTargetBeatSynced();
        }
        else
        {
            MoveConstantlyOffscreen();
        }

        CheckOutOfViewAndDestroy();
    }

    // Fungsi untuk membuat warna lebih tajam dan cerah (Vibrancy)
    private void ApplyVibrancyAndSelfIllumination()
    {
        if (obstacleMaterial == null) return;

        Color baseColor = originalColor;

        float h, s, v;
        Color.RGBToHSV(baseColor, out h, out s, out v);

        // Tingkatkan Saturasi (membuat warna lebih murni/tajam)
        s = Mathf.Clamp01(s + saturationBoost);

        // Tingkatkan Kecerahan (membuat warna terlihat lebih mencolok)
        v *= colorBrightnessMultiplier;

        Color vibrantColor = Color.HSVToRGB(h, s, v);

        if (obstacleMaterial.HasProperty("_Color"))
        {
            obstacleMaterial.color = vibrantColor;
        }

        // Aplikasikan Self-Illumination (PENTING tanpa Bloom)
        if (obstacleMaterial.HasProperty("_EmissionColor"))
        {
            Color finalIlluminationColor = vibrantColor * SelfIlluminationFactor;

            obstacleMaterial.EnableKeyword("_EMISSION");
            obstacleMaterial.SetColor("_EmissionColor", finalIlluminationColor);
        }
        else
        {
            Debug.LogWarning("Material tidak memiliki properti _EmissionColor. Tidak dapat menerapkan Self-Illumination.");
        }
    }

    // Fungsi untuk mengatur rotasi objek agar menghadap ke arah pergerakan
    private void SetupRocketOrientation()
    {
        // Hitung sudut rotasi berdasarkan arah pergerakan (moveDir)
        float angle = Mathf.Atan2(moveDir.y, moveDir.x) * Mathf.Rad2Deg;

        // Terapkan rotasi ke objek utama dengan offset (rotationOffset)
        transform.rotation = Quaternion.Euler(0, 0, angle + rotationOffset);

        // Partikel pendorong harus otomatis mengikuti rotasi parent-nya (roket).
        if (thrusterParticles != null)
        {
            // Pastikan partikel menyala (jika tidak otomatis play on awake)
            if (!thrusterParticles.isPlaying)
            {
                thrusterParticles.Play();
            }
        }
    }


    private void MoveTowardTargetBeatSynced()
    {
        double songTime = AudioSettings.dspTime - songStartDspTime;
        double effectiveDuration = travelDuration / Mathf.Max(0.001f, speed);
        double spawnTime = hitTime - effectiveDuration;

        double t = (songTime - spawnTime) / effectiveDuration;
        float progress = Mathf.Clamp01((float)t);

        transform.position = Vector3.Lerp(spawnPos, targetPos, progress);

        if (progress >= 1f)
        {
            hasReachedTarget = true;
        }
    }

    private void MoveConstantlyOffscreen()
    {
        transform.position += moveDir * (speed * postTargetSpeedMultiplier) * Time.deltaTime;
    }

    private void CheckOutOfViewAndDestroy()
    {
        if (isOutOfView) return;
        if (mainCam == null) return;

        Vector3 viewport = mainCam.WorldToViewportPoint(transform.position);

        if (viewport.x < -viewportMargin || viewport.x > 1 + viewportMargin ||
            viewport.y < -viewportMargin || viewport.y > 1 + viewportMargin)
        {
            isOutOfView = true;
            // Hentikan partikel sebelum destroy agar tidak terjadi efek partikel "melompat"
            if (thrusterParticles != null)
            {
                thrusterParticles.Stop();
                // Tunggu sebentar agar partikel yang tersisa selesai
                Destroy(gameObject, thrusterParticles.main.startLifetime.constantMax);
            }
            else
            {
                Destroy(gameObject, destroyDelayAfterOut);
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("💥 Player hit obstacle!");
           
             HitJudgement.health -= damage;
            HitJudgement.combo = 0;
            DamageEffect.Instance.TriggerFlash();

            // Hentikan partikel sebelum destroy
            if (thrusterParticles != null)
            {
                thrusterParticles.Stop();
                Destroy(gameObject, thrusterParticles.main.startLifetime.constantMax);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}