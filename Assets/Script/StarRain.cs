using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StarRain : MonoBehaviour
{
    [Header("Spawn Settings")]
    public int starCount = 50;               // jumlah maksimum bintang aktif sekaligus
    public float spawnInterval = 0.1f;       // jeda antar spawn
    public Vector2 spawnXRange = new Vector2(-10f, 10f); // posisi X acak
    public Vector2 fallSpeedRange = new Vector2(1f, 4f); // kecepatan jatuh
    public Vector2 scaleRange = new Vector2(0.5f, 1.5f); // skala acak

    [Header("Area Settings")]
    public float spawnHeight = 6f;           // posisi Y awal (atas layar)
    public float destroyHeight = -6f;        // posisi Y hancur (bawah layar)

    [Header("Sorting & Fade")]
    public string sortingLayer = "Default";
    public int sortingOrder = 0;
    public bool fadeOut = true;
    public float fadeDuration = 1f;

    [Header("Star Sprites (Assign via Inspector)")]
    public List<Sprite> starSprites = new List<Sprite>();

    private Camera mainCam;
    private readonly List<GameObject> activeStars = new List<GameObject>();

    void Start()
    {
        mainCam = Camera.main;
        StartCoroutine(SpawnStars());
    }

    IEnumerator SpawnStars()
    {
        while (true)
        {
            // batasi jumlah bintang di layar
            activeStars.RemoveAll(star => star == null);
            if (activeStars.Count < starCount)
                SpawnStar();

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    void SpawnStar()
    {
        if (starSprites == null || starSprites.Count == 0)
        {
            Debug.LogWarning("⚠️ Belum ada sprite bintang yang diassign di Inspector!");
            return;
        }

        // Pilih sprite acak
        Sprite sprite = starSprites[Random.Range(0, starSprites.Count)];

        // Buat GameObject baru
        GameObject star = new GameObject("Star");
        SpriteRenderer sr = star.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerName = sortingLayer;
        sr.sortingOrder = sortingOrder;

        // Random posisi X dan skala
        float x = Random.Range(spawnXRange.x, spawnXRange.y);
        float y = spawnHeight;
        star.transform.position = new Vector3(x, y, 0);
        float scale = Random.Range(scaleRange.x, scaleRange.y);
        star.transform.localScale = Vector3.one * scale;

        // Tambahkan komponen untuk gerak jatuh
        StarFaller faller = star.AddComponent<StarFaller>();
        faller.speed = Random.Range(fallSpeedRange.x, fallSpeedRange.y);
        faller.destroyHeight = destroyHeight;
        faller.fadeOut = fadeOut;
        faller.fadeDuration = fadeDuration;

        activeStars.Add(star);
    }
}

public class StarFaller : MonoBehaviour
{
    [HideInInspector] public float speed = 2f;
    [HideInInspector] public float destroyHeight = -6f;
    [HideInInspector] public bool fadeOut = true;
    [HideInInspector] public float fadeDuration = 1f;

    private SpriteRenderer sr;
    private float initialY;
    private bool isFading = false;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        initialY = transform.position.y;
    }

    void Update()
    {
        transform.position += Vector3.down * speed * Time.deltaTime;

        // mulai fade sebelum benar-benar melewati bawah layar
        float distanceTraveled = initialY - transform.position.y;
        float totalFall = initialY - destroyHeight;
        float progress = distanceTraveled / totalFall;

        if (fadeOut && progress > 0.8f && !isFading)
        {
            // mulai fade ketika 80% perjalanan ke bawah
            StartCoroutine(FadeAndDestroy());
            isFading = true;
        }
        else if (!fadeOut && transform.position.y < destroyHeight)
        {
            Destroy(gameObject);
        }
    }

    IEnumerator FadeAndDestroy()
    {
        float t = 0f;
        Color startColor = sr.color;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            if (sr != null)
            {
                float alpha = Mathf.Lerp(1f, 0f, t / fadeDuration);
                sr.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            }
            yield return null;
        }

        Destroy(gameObject);
    }
}
