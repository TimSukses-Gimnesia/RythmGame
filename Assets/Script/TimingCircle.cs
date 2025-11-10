using UnityEngine;

public class TimingCircle : MonoBehaviour
{
    public float hitTime;
    public float travelDuration;
    public double startDsp;

    public float startScale = 2f;
    public float endScale = 1f;

    public Transform followTarget;

    private SpriteRenderer sr;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        // Move with the note
        if (followTarget != null)
            transform.position = followTarget.position;

        // Determine progression toward hit time
        double songTime = AudioSettings.dspTime - startDsp;
        float t = Mathf.InverseLerp(hitTime - travelDuration, hitTime, (float)songTime);
        t = Mathf.Clamp01(t);

        // Scale shrink over time
        float scale = Mathf.Lerp(startScale, endScale, t);
        transform.localScale = new Vector3(scale, scale, 1f);

        // Soft fade-out for clean visual finish
        if (sr != null)
        {
            Color c = sr.color;
            c.a = Mathf.Lerp(0f, 1f, t);
            sr.color = c;
        }

        // Destroy exactly when hit is reached
        if (t >= 1f)
            Destroy(gameObject);
    }
}