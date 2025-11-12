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

    public float noteMoveSpeed;

    [HideInInspector] public bool isHit = false;

    [Header("Visual Options")]
    [Tooltip("Jika true, SpriteRenderer body akan diubah ke mode Tiled agar size.y bisa diatur.")]
    public bool forceTiledDrawMode = true;

    private double songStartDspTime;
    private SpriteRenderer mySpriteRenderer;

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

        if (body != null)
            bodySR = body.GetComponent<SpriteRenderer>();

        if (mySpriteRenderer == null && bodySR == null)
        {
            Debug.LogError("Prefab Note tidak memiliki SpriteRenderer!", this);
        }
    }

    void Start()
    {
        var spawner = FindFirstObjectByType<SpawnNote>();
        songStartDspTime = spawner != null ? spawner.songStartDspTime : AudioSettings.dspTime;

        transform.position = spawnPos;

        SetupVisuals();

        if (type == "hold")
        {
            allSpriteRenderers = GetComponentsInChildren<SpriteRenderer>();
            mySpriteRenderer = null;
        }
    }

    public void SetupVisuals()
    {
        if (type == "hold")
        {
            if (mySpriteRenderer != null)
                mySpriteRenderer.enabled = false;

            if (head != null) head.gameObject.SetActive(true);
            if (body != null) body.gameObject.SetActive(true);
            if (tail != null) tail.gameObject.SetActive(true);

            // === Hitung panjang body ===
            float totalLength = noteMoveSpeed * holdDurationSec;
            float maxDistance = Vector3.Distance(spawnPos, targetPos);
            totalLength = Mathf.Min(totalLength, maxDistance);

            float bodyLength = Mathf.Max(0, totalLength - (headHeight + tailHeight));

            // HEAD di posisi awal
            head.localPosition = Vector3.zero;

            // Pastikan SpriteRenderer body siap
            if (bodySR != null)
            {
                // ubah ke mode tiled agar bisa ubah size
                if (forceTiledDrawMode)
                    bodySR.drawMode = SpriteDrawMode.Tiled;

                // atur panjang body lewat size.y
                Vector2 size = bodySR.size;
                size.y = bodyLength;
                bodySR.size = size;

                // reset scale agar tidak ganggu
                // body.localScale = Vector3.one;
            }

            // BODY di bawah head
            body.localPosition = new Vector3(0, -headHeight, 0);

            // TAIL di bawah body
            tail.localPosition = new Vector3(0, -headHeight - bodyLength, 0);

            return;
        }

        // === NOTE BIASA ===
        if (mySpriteRenderer != null)
        {
            mySpriteRenderer.enabled = true;
            mySpriteRenderer.size = new Vector2(1f, 1f);
        }

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

        // Atur ulang posisi dan panjang body menggunakan size.y
        if (bodySR != null)
        {
            Vector2 size = bodySR.size;
            size.y = currentBodyLength;
            bodySR.size = size;
        }

        // posisikan ulang body & tail
        body.localPosition = new Vector3(0, -headHeight, 0);
        tail.localPosition = new Vector3(0, -headHeight - currentBodyLength, 0);

        // efek warna opsional
        if (allSpriteRenderers != null)
        {
            Color c = Color.Lerp(Color.white, Color.yellow, progress);
            foreach (var sr in allSpriteRenderers)
                sr.color = c;
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

        transform.position = Vector3.Lerp(spawnPos, targetPos, progress);

        if (type == "hold")
            UpdateHoldProgress(songTime);
    }
}
