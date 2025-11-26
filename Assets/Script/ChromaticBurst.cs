using UnityEngine;

public class ChromaticBurst : MonoBehaviour
{
    public float fadeSpeed = 3f;
    private SpriteRenderer[] srs;

    private void Awake()
    {
        srs = GetComponentsInChildren<SpriteRenderer>();
    }

    // Dipanggil setelah Instantiate
    public void Initialize(Sprite sprite)
    {
        if (sprite == null) return;

        foreach (var sr in srs)
        {
            if (sr == null) continue;
            sr.sprite = sprite;
        }
    }

    private void Update()
    {
        // Fade alpha
        foreach (var sr in srs)
        {
            if (sr == null) continue;

            Color c = sr.color;
            c.a -= fadeSpeed * Time.deltaTime;
            sr.color = c;
        }

        // Burst expand & rotate
        transform.localScale += Vector3.one * 0.7f * Time.deltaTime;
        transform.Rotate(0, 0, 120f * Time.deltaTime);

        // Auto-destroy
        if (srs.Length > 0 && srs[0].color.a <= 0f)
            Destroy(gameObject);
    }
}
