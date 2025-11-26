using UnityEngine;
using System.Collections;

public class PlayerPulse : MonoBehaviour
{
    private Vector3 originalScale;
    private Coroutine pulseRoutine;

    void Awake()
    {
        originalScale = transform.localScale;
    }

    public void Pulse(float strength)
    {
        Debug.Log("Pulse triggered! strength = " + strength);
        if (pulseRoutine != null) StopCoroutine(pulseRoutine);
        pulseRoutine = StartCoroutine(PulseRoutine(strength));
    }

    IEnumerator PulseRoutine(float strength)
    {
        Vector3 enlarged = originalScale * (1f + strength);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 6f;
            float curve = Mathf.Sin(t * Mathf.PI);
            curve = Mathf.Pow(Mathf.Abs(curve), 0.7f); // naik turun smooth
            transform.localScale = Vector3.Lerp(originalScale, enlarged, curve);
            yield return null;
        }

        transform.localScale = originalScale;
    }
}
