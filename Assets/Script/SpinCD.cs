using UnityEngine;

public class SpinCD : MonoBehaviour
{
    [Header("Retro Settings")]
    public float framesPerSecond = 12f;
    public float anglePerStep = 30f;

    private float timer;    

    void Update()
    {
       
        timer += Time.unscaledDeltaTime;

        if (timer >= 1f / framesPerSecond)
        {
            transform.Rotate(0, 0, -anglePerStep);
            timer = 0;
        }
    }
}