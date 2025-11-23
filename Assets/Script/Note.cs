using UnityEngine;

public class Note : MonoBehaviour
{
    [HideInInspector] public bool isHolding = false;
    [HideInInspector] public bool holdBroken = false;

    [Header("Timing")]
    public float hitTime;
    public string dir;
    public string type;
    public float holdDurationSec;

    [Header("Movement")]
    public Vector3 spawnPos;
    public Vector3 targetPos;
    public float travelDuration;
    public float speed = 1f;
    public float noteMoveSpeed;
    [HideInInspector] public string initialJudgement = "Perfect";
    // ==========================================
    // 🌊 PHANTOM SLIDE (NORMAL & HOLD)
    // ==========================================
    [Header("Phantom (Slide) Logic")]
    public bool isPhantom = false;
    public Vector3 fakeSpawnPos;
    public Vector3 fakeTargetPos;
    public float switchThreshold = 0.5f;
    public float transitionDuration = 0.2f;
    public GameObject switchEffectPrefab;
    private bool hasTriggeredFX = false;

    // ==========================================
    // 👻 GHOST HOLD (WORMHOLE EFFECT)
    // ==========================================
    [Header("Ghost Hold (Wormhole) Logic")]
    public bool isGhostHold = false;
    [Tooltip("Titik (0.0-1.0) di mana note hilang total dan pindah posisi.")]
    public float ghostSwitchPoint = 0.5f;
    public float fadeSpeed = 4f;

    // ==========================================
    // INTERNAL VARS
    // ==========================================
    [HideInInspector] public bool isHit = false;
    public bool forceTiledDrawMode = true;

    // Rotasi
    [HideInInspector] public Quaternion targetRotation;
    private Quaternion initialRotation;

    private double songStartDspTime;
    private SpriteRenderer mySpriteRenderer;
    private TrailRenderer trail;

    // Untuk manipulasi visual
    private float currentGhostAlpha = 1f;
    private Vector3 originalScale; // Menyimpan skala asli untuk efek shrinking

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

        // Simpan skala awal agar bisa dikembalikan setelah efek Ghost
        originalScale = transform.localScale;

        // 1. SET POSISI AWAL
        if (isPhantom || isGhostHold)
            transform.position = fakeSpawnPos;
        else
            transform.position = spawnPos;

        // 2. SET ROTASI AWAL
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
        if (type == "hold")
        {
            if (mySpriteRenderer != null) mySpriteRenderer.enabled = false;
            if (head != null) head.gameObject.SetActive(true);
            if (body != null) body.gameObject.SetActive(true);
            if (tail != null) tail.gameObject.SetActive(true);

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

        if (mySpriteRenderer != null) { mySpriteRenderer.enabled = true; mySpriteRenderer.size = new Vector2(1f, 1f); }
        if (head != null) head.gameObject.SetActive(false);
        if (body != null) body.gameObject.SetActive(false);
        if (tail != null) tail.gameObject.SetActive(false);
    }

    public void UpdateHoldProgress(double songTime)
    {
        if (type != "hold") return;

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

        // 🔥 UPDATE COLOR + ALPHA UNTUK GHOST
        if (allSpriteRenderers != null)
        {
            Color baseColor = Color.Lerp(Color.white, Color.yellow, progress);
            baseColor.a = currentGhostAlpha;
            foreach (var sr in allSpriteRenderers) sr.color = baseColor;
        }
    }

    void Update()
    {
        if (isHit) return;

        double songTime = AudioSettings.dspTime - songStartDspTime;
        double effectiveDuration = travelDuration / Mathf.Max(0.001f, speed);
        double spawnTime = hitTime - effectiveDuration;

        double t = (songTime - spawnTime) / effectiveDuration;
        float progress = Mathf.Clamp01((float)t);

        // ==========================================
        // 👻 LOGIKA GHOST HOLD (WORMHOLE EFFECT)
        // ==========================================
        if (isGhostHold)
        {
            // Hitung jarak ke titik tengah teleport (0.5)
            float distToCenter = Mathf.Abs(progress - ghostSwitchPoint); // 0.5 -> 0 -> 0.5

            // Efek Skala: 1 -> 0 -> 1 (Mengecil lalu Membesar)
            // Dikali 2 supaya saat distToCenter 0.5 (awal), scale-nya 1.
            float scaleFactor = Mathf.Clamp01(distToCenter * 2f * fadeSpeed);
            // Tambahkan curve agar scaling lebih smooth (Easing)
            scaleFactor = scaleFactor * scaleFactor * (3f - 2f * scaleFactor);

            transform.localScale = originalScale * scaleFactor;
            currentGhostAlpha = scaleFactor; // Alpha mengikuti scale

            // Logika Posisi & Rotasi Snap
            if (progress < ghostSwitchPoint)
            {
                // FASE 1: Di Jalur Palsu
                transform.position = Vector3.Lerp(fakeSpawnPos, fakeTargetPos, progress);
                transform.rotation = initialRotation;
            }
            else
            {
                // FASE 2: Di Jalur Asli
                transform.position = Vector3.Lerp(spawnPos, targetPos, progress);
                transform.rotation = targetRotation;
            }

            // Update Alpha untuk Normal Note (Hold Note dihandle di UpdateHoldProgress)
            if (type != "hold" && mySpriteRenderer != null)
            {
                Color c = mySpriteRenderer.color;
                c.a = currentGhostAlpha;
                mySpriteRenderer.color = c;
            }
        }
        // ==========================================
        // 🌊 LOGIKA PHANTOM SLIDE (SMOOTH BANKING)
        // ==========================================
        else if (isPhantom)
        {
            currentGhostAlpha = 1f;
            transform.localScale = originalScale; // Pastikan scale normal

            float startTransition = switchThreshold - (transitionDuration / 2f);
            float endTransition = switchThreshold + (transitionDuration / 2f);

            // 1. Fase Sebelum Slide (Jalur Palsu)
            if (progress < startTransition)
            {
                transform.position = Vector3.Lerp(fakeSpawnPos, fakeTargetPos, progress);
                transform.rotation = initialRotation;
            }
            // 3. Fase Setelah Slide (Jalur Asli)
            else if (progress > endTransition)
            {
                transform.position = Vector3.Lerp(spawnPos, targetPos, progress);
                transform.rotation = targetRotation;
            }
            // 2. Fase Transisi (SLIDING & ROTATING)
            else
            {
                // Trigger Effect Sekali Saja
                if (!hasTriggeredFX && switchEffectPrefab != null)
                {
                    hasTriggeredFX = true;
                    Instantiate(switchEffectPrefab, transform.position, Quaternion.identity);
                }

                // Hitung progress linear 0 s/d 1 dalam durasi transisi
                float tLinear = Mathf.InverseLerp(startTransition, endTransition, progress);

                // Gunakan Easing agar gerakan lebih luwes (Slow in - Fast Out - Slow In)
                float tEased = Mathf.SmoothStep(0f, 1f, tLinear);
                    
                // --- POSISI ---
                Vector3 posOnFake = Vector3.Lerp(fakeSpawnPos, fakeTargetPos, progress);
                Vector3 posOnReal = Vector3.Lerp(spawnPos, targetPos, progress);
                transform.position = Vector3.Lerp(posOnFake, posOnReal, tEased);

                // --- ROTASI (KUNCI PERBAIKAN) ---
                // Menggunakan Slerp agar note berputar pelan menuju arah jalur baru
                // Ini membuat efek "Banking" seperti pesawat berbelok
                transform.rotation = Quaternion.Slerp(initialRotation, targetRotation, tEased);
            }
        }
        else
        {
            // NORMAL NOTE
            currentGhostAlpha = 1f;
            transform.localScale = originalScale;
            transform.position = Vector3.Lerp(spawnPos, targetPos, progress);
            transform.rotation = targetRotation;
        }

        if (type == "hold") UpdateHoldProgress(songTime);
    }
}