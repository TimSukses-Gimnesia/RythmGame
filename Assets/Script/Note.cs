using UnityEngine;

public class Note : MonoBehaviour
{
    [HideInInspector] public bool isHolding = false;
    [HideInInspector] public bool holdBroken = false;

    [Header("Timing")]
    public float hitTime;
    public string dir;
    public string type; // "note", "hold", "decoy"
    public float holdDurationSec;

    [Header("Visual Settings")] // 🔥 BARU: Setting Warna
    [Tooltip("Warna saat note hold sedang ditekan.")]
    public Color activeHoldColor = new Color(1f, 0.85f, 0f); // Default: Emas/Gold

    [Header("Decoy Settings")]
    [Tooltip("Waktu (detik) sebelum kena garis, note ini akan hancur.")]
    public float despawnOffset = 0.2f;
    [Tooltip("Prefab Particle System yang muncul saat Decoy hilang/hancur.")]
    public GameObject decoyVanishEffect;

    [Header("Movement")]
    public Vector3 spawnPos;
    public Vector3 targetPos;
    public float travelDuration;
    public float speed = 1f;
    public float noteMoveSpeed;
    [HideInInspector] public string initialJudgement = "Perfect";

    [Header("Phantom (Slide) Logic")]
    public bool isPhantom = false;
    public Vector3 fakeSpawnPos;
    public Vector3 fakeTargetPos;
    public float switchThreshold = 0.5f;
    public float transitionDuration = 0.2f;
    public GameObject switchEffectPrefab;
    private bool hasTriggeredFX = false;

    [Header("Ghost Hold (Wormhole) Logic")]
    public bool isGhostHold = false;
    public float ghostSwitchPoint = 0.5f;
    public float fadeSpeed = 4f;

    [HideInInspector] public bool isHit = false;
    public bool forceTiledDrawMode = true;

    [HideInInspector] public Quaternion targetRotation;
    private Quaternion initialRotation;

    private double songStartDspTime;
    private SpriteRenderer mySpriteRenderer;
    private TrailRenderer trail;

    private float currentGhostAlpha = 1f;
    private Vector3 originalScale;
    private Color baseNoteColor = Color.white; // Menyimpan warna dasar arah

    [Header("Hold Parts")]
    public Transform head;
    public Transform body;
    public Transform tail;
    public float headHeight = 0.3f;
    public float tailHeight = 0.3f;
    private SpriteRenderer bodySR;
    private SpriteRenderer[] allSpriteRenderers;

    void Awake()
    {
        mySpriteRenderer = GetComponent<SpriteRenderer>();
        if (body != null) bodySR = body.GetComponent<SpriteRenderer>();
        trail = GetComponent<TrailRenderer>();
    }

    void Start()
    {
        var spawner = FindFirstObjectByType<SpawnNote>();
        songStartDspTime = spawner != null ? spawner.songStartDspTime : AudioSettings.dspTime;

        if (trail != null) trail.emitting = false;

        originalScale = transform.localScale;

        // Setup Posisi Awal
        if (isPhantom || isGhostHold)
            transform.position = fakeSpawnPos;
        else
            transform.position = spawnPos;

        // Setup Rotasi Awal
        initialRotation = transform.rotation;
        if (targetRotation.x == 0 && targetRotation.y == 0 && targetRotation.z == 0 && targetRotation.w == 0)
            targetRotation = transform.rotation;

        SetupVisuals();

        if (type == "hold")
        {
            allSpriteRenderers = GetComponentsInChildren<SpriteRenderer>();
            mySpriteRenderer = null;
        }

        if (trail != null) Invoke("EnableTrail", 0.1f);
    }

    void EnableTrail() { if (trail != null) trail.emitting = true; }

    public void SetupVisuals()
    {
        // 1. Tentukan Warna Berdasarkan Arah
        baseNoteColor = Color.white; // Default Up

        if (dir == "right") baseNoteColor = Color.yellow;
        else if (dir == "left") baseNoteColor = new Color(0.88f, 0.62f, 1f); // Pink/Magenta
        else if (dir == "down") baseNoteColor = Color.cyan; // Biru Muda/Cyan

        // 2. Setup Visual untuk HOLD Note
        if (type == "hold")
        {
            if (mySpriteRenderer != null) mySpriteRenderer.enabled = false;

            if (head != null)
            {
                head.gameObject.SetActive(true);
                head.GetComponent<SpriteRenderer>().color = baseNoteColor;
            }

            if (body != null)
            {
                body.gameObject.SetActive(true);
                if (bodySR != null) bodySR.color = baseNoteColor;
            }

            if (tail != null)
            {
                tail.gameObject.SetActive(true);
                tail.GetComponent<SpriteRenderer>().color = baseNoteColor;
            }

            float totalLength = noteMoveSpeed * holdDurationSec;
            float maxDistance = Vector3.Distance(spawnPos, targetPos);
            totalLength = Mathf.Min(totalLength, maxDistance);
            float bodyLength = Mathf.Max(0, totalLength - (headHeight + tailHeight));

            head.localPosition = Vector3.zero;
            if (bodySR != null)
            {
                if (forceTiledDrawMode) bodySR.drawMode = SpriteDrawMode.Tiled;
                Vector2 size = bodySR.size; size.y = bodyLength; bodySR.size = size;
            }
            body.localPosition = new Vector3(0, -headHeight, 0);
            tail.localPosition = new Vector3(0, -headHeight - bodyLength, 0);
            return;
        }

        // 3. Setup Visual untuk NORMAL / DECOY Note
        if (mySpriteRenderer != null)
        {
            mySpriteRenderer.enabled = true;
            mySpriteRenderer.size = new Vector2(1f, 1f);

            // Jika Decoy, biarkan DecoyColorChanger yang atur
            if (type != "decoy")
            {
                mySpriteRenderer.color = baseNoteColor;
            }
        }

        if (head != null) head.gameObject.SetActive(false);
        if (body != null) body.gameObject.SetActive(false);
        if (tail != null) tail.gameObject.SetActive(false);
    }

    public void UpdateHoldProgress(double songTime)
    {
        if (type != "hold") return;

        // --- 1. Logika Ukuran Body ---
        double holdStartTime = hitTime;
        double holdEndTime = hitTime + holdDurationSec;
        float progress = Mathf.Clamp01((float)((songTime - holdStartTime) / (holdEndTime - holdStartTime)));

        float totalLength = noteMoveSpeed * holdDurationSec;
        float maxDistance = Vector3.Distance(spawnPos, targetPos);
        totalLength = Mathf.Min(totalLength, maxDistance);
        float maxBodyLength = Mathf.Max(0, totalLength - (headHeight + tailHeight));
        float currentBodyLength = maxBodyLength * (1f - progress);

        if (bodySR != null) { Vector2 size = bodySR.size; size.y = currentBodyLength; bodySR.size = size; }
        body.localPosition = new Vector3(0, -headHeight, 0);
        tail.localPosition = new Vector3(0, -headHeight - currentBodyLength, 0);

        // --- 2. Logika Warna Responsif (Modifikasi) ---
        if (allSpriteRenderers != null)
        {
            Color finalColor;

            if (isHolding)
            {
                // 🔥 Saat ditekan: Gunakan warna Emas (activeHoldColor)
                finalColor = activeHoldColor;
            }
            else
            {
                // Saat lepas: Kembali ke Warna Arah
                finalColor = baseNoteColor;
            }

            finalColor.a = currentGhostAlpha;

            foreach (var sr in allSpriteRenderers)
            {
                sr.color = finalColor;
            }
        }
    }

    void Update()
    {
        if (isHit) return;

        double songTime = AudioSettings.dspTime - songStartDspTime;

        // 🔥 LOGIKA DECOY
        if (type == "decoy")
        {
            if (songTime > hitTime - despawnOffset)
            {
                if (decoyVanishEffect != null)
                {
                    GameObject vfx = Instantiate(decoyVanishEffect, transform.position, Quaternion.identity);
                    Destroy(vfx, 2f);
                }
                Destroy(gameObject);
                return;
            }
        }

        double effectiveDuration = travelDuration / Mathf.Max(0.001f, speed);
        double spawnTime = hitTime - effectiveDuration;
        double t = (songTime - spawnTime) / effectiveDuration;
        float progress = Mathf.Clamp01((float)t);

        // --- Logic Pergerakan Normal / Phantom / Ghost ---
        if (isGhostHold)
        {
            float distToCenter = Mathf.Abs(progress - ghostSwitchPoint);
            float scaleFactor = Mathf.Clamp01(distToCenter * 2f * fadeSpeed);
            scaleFactor = scaleFactor * scaleFactor * (3f - 2f * scaleFactor);

            transform.localScale = originalScale * scaleFactor;
            currentGhostAlpha = scaleFactor;

            if (progress < ghostSwitchPoint)
            {
                transform.position = Vector3.Lerp(fakeSpawnPos, fakeTargetPos, progress);
                transform.rotation = initialRotation;
            }
            else
            {
                transform.position = Vector3.Lerp(spawnPos, targetPos, progress);
                transform.rotation = targetRotation;
            }

            if (type != "hold" && mySpriteRenderer != null)
            {
                Color c = (type == "decoy") ? mySpriteRenderer.color : baseNoteColor;
                c.a = currentGhostAlpha;
                mySpriteRenderer.color = c;
            }
        }
        else if (isPhantom)
        {
            currentGhostAlpha = 1f;
            transform.localScale = originalScale;

            float startTransition = switchThreshold - (transitionDuration / 2f);
            float endTransition = switchThreshold + (transitionDuration / 2f);

            if (progress < startTransition)
            {
                transform.position = Vector3.Lerp(fakeSpawnPos, fakeTargetPos, progress);
                transform.rotation = initialRotation;
            }
            else if (progress > endTransition)
            {
                transform.position = Vector3.Lerp(spawnPos, targetPos, progress);
                transform.rotation = targetRotation;
            }
            else
            {
                if (!hasTriggeredFX && switchEffectPrefab != null)
                {
                    hasTriggeredFX = true;
                    Instantiate(switchEffectPrefab, transform.position, Quaternion.identity);
                }

                float tLinear = Mathf.InverseLerp(startTransition, endTransition, progress);
                float tEased = Mathf.SmoothStep(0f, 1f, tLinear);

                Vector3 posOnFake = Vector3.Lerp(fakeSpawnPos, fakeTargetPos, progress);
                Vector3 posOnReal = Vector3.Lerp(spawnPos, targetPos, progress);
                transform.position = Vector3.Lerp(posOnFake, posOnReal, tEased);
                transform.rotation = Quaternion.Slerp(initialRotation, targetRotation, tEased);
            }
        }
        else
        {
            currentGhostAlpha = 1f;
            transform.localScale = originalScale;
            transform.position = Vector3.Lerp(spawnPos, targetPos, progress);
            transform.rotation = targetRotation;
        }

        if (type == "hold") UpdateHoldProgress(songTime);
    }
}