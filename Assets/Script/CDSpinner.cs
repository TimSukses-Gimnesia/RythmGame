using System.Collections;
using UnityEngine;

public class CDSpinner : MonoBehaviour
{
    [Header("Base rotation")]
    public float baseSpeed = 120f;       
    public bool useUnscaledDeltaTime = true;

    [Header("Retro / pixel style")]
    public int retroStepsPerSecond = 10;     // semakin kecil = semakin patah
    public float stepSize = 4f;              // besar loncatan per step

    [Header("Burst (Next / Prev)")]
    public float burstStepMultiplier = 4f;   // 4x lebih besar dari stepSize
    public float burstDuration = 0.25f;

    private float currentAngle = 0f;
    private float rotationDirection = -1;  
    private Coroutine burstRoutine;
    private float retroTimer = 0f;

    void Update()
    {
        float dt = useUnscaledDeltaTime ? Time.unscaledDeltaTime : Time.deltaTime;

        // timer untuk step patah-patah
        retroTimer += dt;

        float stepInterval = 1f / retroStepsPerSecond;

        if (retroTimer >= stepInterval)
        {
            retroTimer -= stepInterval;

            // tambahkan rotasi dalam STEP, bukan smooth
            currentAngle += stepSize * rotationDirection;

            transform.localRotation = Quaternion.Euler(0f, 0f, -currentAngle);
        }
    }

    public void SetDirectionNext()   // spin ke atas
    {
        rotationDirection = +1;
        TriggerBurst();
    }

    public void SetDirectionPrev()   // spin ke bawah
    {
        rotationDirection = -1;
        TriggerBurst();
    }

    public void TriggerBurst()
    {
        if (burstRoutine != null) StopCoroutine(burstRoutine);
        burstRoutine = StartCoroutine(BurstRoutine());
    }

    IEnumerator BurstRoutine()
    {
        float elapsed = 0f;

        while (elapsed < burstDuration)
        {
            elapsed += (useUnscaledDeltaTime ? Time.unscaledDeltaTime : Time.deltaTime);

            // burst = beberapa step sekaligus → patah-patah kuat
            currentAngle += stepSize * burstStepMultiplier * rotationDirection;

            transform.localRotation = Quaternion.Euler(0f, 0f, -currentAngle);

            yield return null;
        }

        burstRoutine = null;
    }
}
