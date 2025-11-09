using UnityEngine;

public class Note : MonoBehaviour
{
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

    [HideInInspector]
    public bool isHit = false;

    double songStartDspTime;

    private SpriteRenderer mySpriteRenderer;

    void Start()
    {
        var spawner = FindFirstObjectByType<SpawnNote>();
        songStartDspTime = spawner != null ? spawner.songStartDspTime : AudioSettings.dspTime;
        transform.position = spawnPos;

        mySpriteRenderer = GetComponent<SpriteRenderer>();
        if (mySpriteRenderer == null)
        {
            Debug.LogError("Prefab Note tidak memiliki SpriteRenderer!", this.gameObject);
        }
    }

    public void SetupVisuals()
    {
        if (mySpriteRenderer == null) return; 

        if (type == "hold")
        {
            

            if (noteMoveSpeed > 0)
            {
                
                float noteLength = noteMoveSpeed * holdDurationSec;

           
                mySpriteRenderer.size = new Vector2(mySpriteRenderer.size.x, noteLength);

             
            }
        }
   
    }

 
    public void UpdateHoldProgress(double songTime)
    {
        if (type != "hold" || mySpriteRenderer == null) return;

        double holdStartTime = hitTime;
        double holdEndTime = hitTime + holdDurationSec;
        float progress = Mathf.Clamp01((float)((songTime - holdStartTime) / (holdEndTime - holdStartTime)));

        mySpriteRenderer.color = Color.Lerp(Color.white, Color.yellow, progress);
    }

    void Update()
    {
        if (isHit) return;

        double songTime = AudioSettings.dspTime - songStartDspTime;
        double effectiveDuration = travelDuration / Mathf.Max(0.001f, speed);
        double spawnTime = hitTime - effectiveDuration;
        double progress = (songTime - spawnTime) / effectiveDuration;
        progress = Mathf.Clamp01((float)progress);

        transform.position = Vector3.Lerp(spawnPos, targetPos, (float)progress);
    }
}