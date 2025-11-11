using UnityEngine;

public class Note : MonoBehaviour

{
    [HideInInspector] public bool isHolding = false;   // player sedang menahan
    [HideInInspector] public bool holdBroken = false;  // dilepas sebelum selesai

    [Header("Timing")]
    public float hitTime;
    public string dir;                // "up" | "down" | "left" | "right"
    public string type;               // "note" | "hold"
    public float holdDurationSec;     // dipakai kalau type == "hold"

    [Header("Movement")]
    public Vector3 spawnPos;
    public Vector3 targetPos;
    public float travelDuration;
    public float speed = 1f;

    // Kecepatan gerak (satuan: unit/detik) dihitung di spawner
    public float noteMoveSpeed;

    [HideInInspector] public bool isHit = false;

    // --- Options ---
    [Header("Visual Options")]
    [Tooltip("Paksa SpriteRenderer ke Tiled agar SpriteRenderer.size berefek.")]
    public bool forceTiledDrawMode = true;

    [Tooltip("Warn kalau sprite import setting berpotensi mengganggu tiling.")]
    public bool warnSpriteImportIssues = true;

    private double songStartDspTime;
    private SpriteRenderer mySpriteRenderer;

    [Header("Hold Parts")]
    public Transform head;
    public Transform body;
    public Transform tail;
    public float headHeight = 0.3f;
    public float tailHeight = 0.3f;

    private SpriteRenderer[] allSpriteRenderers;

    void Awake()
    {
        mySpriteRenderer = GetComponent<SpriteRenderer>();
        if (mySpriteRenderer == null)
        {
            var childSR = GetComponentInChildren<SpriteRenderer>();
            if(childSR == null)
                Debug.LogError("Prefab Note tidak memiliki SpriteRenderer!", this);
            return;
        }
    }

    void Start()
    {
        var spawner = FindFirstObjectByType<SpawnNote>();
        songStartDspTime = spawner != null ? spawner.songStartDspTime : AudioSettings.dspTime;

        // mulai dari posisi spawn
        transform.position = spawnPos;

        // set visual awal (panjang hold, dll)
        SetupVisuals();

        if (type == "hold")
        {
            allSpriteRenderers = GetComponentsInChildren<SpriteRenderer>();
            mySpriteRenderer = null; // tidak pakai root render
            return;
        }
    }

    /// <summary>
    /// Atur tampilan awal note.
    /// Untuk hold: memanjangkan sprite sesuai durasi * kecepatan gerak, dan di-clamp agar tak melewati target.
    /// Memperhitungkan orientasi (up/down = vertikal; left/right = horizontal).
    /// </summary>
    public void SetupVisuals()
    {
        if (type == "hold")
        {
            // pastikan main sprite renderer dimatiin agar tidak dobel
            if (mySpriteRenderer != null)
                mySpriteRenderer.enabled = false;

            // aktifkan head / body / tail
            if (head != null) head.gameObject.SetActive(true);
            if (body != null) body.gameObject.SetActive(true);
            if (tail != null) tail.gameObject.SetActive(true);

            float totalLength = noteMoveSpeed * holdDurationSec;
            float maxDistance = Vector3.Distance(spawnPos, targetPos);
            totalLength = Mathf.Min(totalLength, maxDistance);

            // body length
            float bodyLength = Mathf.Max(0, totalLength - (headHeight + tailHeight));

            // HEAD posisi 0
            head.localPosition = Vector3.zero;

            // BODY dibawah head
            body.localPosition = new Vector3(0, -headHeight * 0.5f, 0);
            body.localScale = new Vector3(0.5f, bodyLength, 1);

            // TAIL paling bawah
            tail.localPosition = new Vector3(0, -headHeight - bodyLength, 0);

            return;
        }

        // NOTE biasa tetap original behaviour mu
        if (mySpriteRenderer != null)
        {
            mySpriteRenderer.enabled = true;
            Vector2 s = mySpriteRenderer.size;
            mySpriteRenderer.size = new Vector2(1f, 1f);
        }

        if (head != null) head.gameObject.SetActive(false);
        if (body != null) body.gameObject.SetActive(false);
        if (tail != null) tail.gameObject.SetActive(false);
    }


    /// <summary>
    /// Opsional: progress visual saat hold berlangsung (contoh: ganti warna).
    /// </summary>
    public void UpdateHoldProgress(double songTime)
    {
        if (type != "hold") return;
    
        double holdStartTime = hitTime;
        double holdEndTime   = hitTime + holdDurationSec;
        float progress = Mathf.Clamp01((float)((songTime - holdStartTime) / (holdEndTime - holdStartTime)));
    
        float totalLength = noteMoveSpeed * holdDurationSec;
        float maxDistance = Vector3.Distance(spawnPos, targetPos);
        totalLength = Mathf.Min(totalLength, maxDistance);
    
        float maxBodyLength = Mathf.Max(0, totalLength - (headHeight + tailHeight));
    
        float currentBodyLength = maxBodyLength * (1f - progress);
    
        // head tetap 0,0,0
    
        body.localPosition = new Vector3(0, -headHeight, 0);
        body.localScale = new Vector3(body.localScale.x, currentBodyLength, 1);

        tail.localPosition = new Vector3(0, -headHeight - currentBodyLength, 0);
        
        Color c = Color.Lerp(Color.white, Color.yellow, progress);
        foreach (var sr in allSpriteRenderers)
        {
            sr.color = c;
        }
    }

    void Update()
    {
        if (isHit) return;

        double songTime = AudioSettings.dspTime - songStartDspTime;

        // Lerp posisi dari spawn -> target berdasarkan travelDuration & speed
        double effectiveDuration = travelDuration / Mathf.Max(0.001f, speed);
        double spawnTime = hitTime - effectiveDuration;
        double t = (songTime - spawnTime) / effectiveDuration;
        float progress = Mathf.Clamp01((float)t);

        transform.position = Vector3.Lerp(spawnPos, targetPos, progress);

        // (Opsional) Update visual progres untuk hold
        if (type == "hold")
            UpdateHoldProgress(songTime);
    }
}
