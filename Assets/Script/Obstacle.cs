using UnityEngine;

public class Obstacle : MonoBehaviour
{
    [Header("Movement Timing")]
    public float hitTime;             // waktu saat obstacle mencapai target (beat)
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

    private double songStartDspTime;
    private bool hasReachedTarget = false;
    private Vector3 moveDir;
    private Camera mainCam;
    private bool isOutOfView = false;

    void Start()
    {
        var spawner = FindFirstObjectByType<SpawnNote>();
        songStartDspTime = spawner != null ? spawner.songStartDspTime : AudioSettings.dspTime;

        transform.position = spawnPos;
        moveDir = (targetPos - spawnPos).normalized;
        mainCam = Camera.main;
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
        // Bergerak stabil tanpa Lerp — terus menerus dengan kecepatan konstan
        transform.position += moveDir * (speed * postTargetSpeedMultiplier) * Time.deltaTime;
    }

    private void CheckOutOfViewAndDestroy()
    {
        if (isOutOfView) return;
        if (mainCam == null) return;

        Vector3 viewport = mainCam.WorldToViewportPoint(transform.position);

        // Jika obstacle benar-benar keluar dari layar (lebih dari margin)
        if (viewport.x < -viewportMargin || viewport.x > 1 + viewportMargin ||
            viewport.y < -viewportMargin || viewport.y > 1 + viewportMargin)
        {
            isOutOfView = true;
            Destroy(gameObject, destroyDelayAfterOut);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("💥 Player hit obstacle!");
            HitJudgement.health -= damage;
            Destroy(gameObject);
        }
    }
}
