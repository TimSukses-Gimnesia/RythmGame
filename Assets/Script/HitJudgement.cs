using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class HitJudgement : MonoBehaviour
{
    [Header("Lane Settings")]
    public string targetDirection;
    public Key targetKey;

    [Header("Timing Windows (detik)")]
    public float perfectTime = 0.05f;
    public float goodTime = 0.1f;

    [Header("UI Popup")]
    public HitPopup popup;

    [Header("Health Bar Values")]
    public float perfectHealthGain = 10f;
    public float goodHealthGain = 5f;
    public float missHealthPenalty = 15f;
    public float holdBreakPenalty = 10f;
    public float holdSuccessGain = 2f;

    [Header("Hit Effect (VFX)")]
    public GameObject hitEffectPrefab;
    public bool enableHitEffect = true;
    public Transform effectsParent; // opsional (biar hierarchy rapi)
    public string effectSortingLayer = "Default";
    public int effectSortingOrder = 20;

    private List<Note> notesInTrigger = new List<Note>();
    private SpawnNote spawner;

    private Note currentlyHoldingNote = null;

    public static int score = 0;
    public static int combo = 0;
    public static float health;

    void Start()
    {
        spawner = FindFirstObjectByType<SpawnNote>();
    }

    // ---- PATCH: HEALTH CLAMP ----
    void ApplyHealth(float delta)
    {
        float maxHP = FindFirstObjectByType<PlayerMovement>()?.maxHealth ?? 100f;
        health = Mathf.Clamp(health + delta, 0f, maxHP);
    }

    // ---- NEW: Spawn Hit Visual Effect ----
    void SpawnHitEffect(Note note)
    {
        if (!enableHitEffect) return;
        if (hitEffectPrefab == null || note == null) return;

        Vector3 pos = new Vector3(note.targetPos.x, note.targetPos.y, 0f);
        GameObject fx = Instantiate(hitEffectPrefab, pos, Quaternion.identity, effectsParent);

        var psRenderer = fx.GetComponent<ParticleSystemRenderer>();
        if (psRenderer != null)
        {
            psRenderer.sortingLayerName = effectSortingLayer;
            psRenderer.sortingOrder = effectSortingOrder;
        }

        var ps = fx.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            Destroy(fx, ps.main.duration + ps.main.startLifetime.constantMax);
        }
        else
        {
            Destroy(fx, 1.2f);
        }
    }

    private string GetJudgement(double timeDiff)
    {
        if (timeDiff <= perfectTime) return "Perfect";
        if (timeDiff <= goodTime) return "Good";
        return "Miss";
    }

    void Update()
    {
        if (spawner == null) return;
        if (spawner.songStartDspTime == 0.0) return;

        double songTime = AudioSettings.dspTime - spawner.songStartDspTime;

        while (notesInTrigger.Count > 0 && songTime > notesInTrigger[0].hitTime + goodTime)
        {
            Note noteToMiss = notesInTrigger[0];
            notesInTrigger.RemoveAt(0);
            HandleMiss(noteToMiss);
        }

        if (currentlyHoldingNote != null)
        {
            double holdEndTime = currentlyHoldingNote.hitTime + currentlyHoldingNote.holdDurationSec;

            if (Keyboard.current != null && Keyboard.current[targetKey].isPressed)
            {
                if (songTime >= holdEndTime)
                {
                    HandleHoldJudgement(true, currentlyHoldingNote);
                }
                else
                {
                    currentlyHoldingNote.UpdateHoldProgress(songTime);
                }
            }

            if (Keyboard.current != null && Keyboard.current[targetKey].wasReleasedThisFrame)
            {
                if (songTime < holdEndTime - goodTime)
                {
                    HandleHoldJudgement(false, currentlyHoldingNote);
                }
                else
                {
                    HandleHoldJudgement(true, currentlyHoldingNote);
                }
            }
        }

        if (currentlyHoldingNote == null && Keyboard.current != null && Keyboard.current[targetKey].wasPressedThisFrame)
        {
            if (notesInTrigger.Count > 0)
            {
                Note noteToHit = notesInTrigger[0];
                double timeDiff = System.Math.Abs(songTime - noteToHit.hitTime);
                string judgement = GetJudgement(timeDiff);

                if (judgement != "Miss")
                {
                    if (noteToHit.type == "hold")
                    {
                        currentlyHoldingNote = noteToHit;
                        notesInTrigger.RemoveAt(0);
                        HandleHit(judgement, noteToHit, false);
                    }
                    else
                    {
                        notesInTrigger.RemoveAt(0);
                        HandleHit(judgement, noteToHit, true);
                    }
                }
                else
                {
                    notesInTrigger.RemoveAt(0);
                    HandleMiss(noteToHit);
                }
            }
        }
    }

    // ---- PATCHED HandleHit ----
    void HandleHit(string judgement, Note note, bool destroyNote)
    {
        combo++;

        int baseScore = (judgement == "Perfect") ? 200 : (judgement == "Good" ? 50 : 0);

        if (judgement == "Perfect") ApplyHealth(perfectHealthGain);
        else if (judgement == "Good") ApplyHealth(goodHealthGain);

        popup.Setup(judgement + "!", (judgement == "Perfect") ? Color.cyan : Color.green);
        popup.gameObject.SetActive(true);

        score += baseScore * combo;

        SpawnHitEffect(note); // <---- efek hit DI SINI

        note.isHit = true;
        if (destroyNote) Destroy(note.gameObject);
    }

    // ---- PATCHED HandleHoldJudgement ----
    void HandleHoldJudgement(bool success, Note note)
    {
        if (success)
        {
            combo++;
            score += 150 * combo;
            ApplyHealth(holdSuccessGain);
            popup.Setup("HOLD OK!", Color.yellow);

            SpawnHitEffect(note); // <---- efek hold selesai DI SINI
        }
        else
        {
            combo = 0;
            ApplyHealth(-holdBreakPenalty);
            popup.Setup("BREAK", Color.red);
        }

        popup.gameObject.SetActive(true);
        Destroy(note.gameObject);
        currentlyHoldingNote = null;
    }

    void HandleMiss(Note note)
    {
        if (note == null || note.isHit) return;

        if (note == currentlyHoldingNote) currentlyHoldingNote = null;

        popup.Setup("MISS", Color.red);
        popup.gameObject.SetActive(true);

        combo = 0;
        ApplyHealth(-missHealthPenalty);

        note.isHit = true;
        Destroy(note.gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Note note = other.GetComponent<Note>();
        if (note != null && note.dir == targetDirection && !note.isHit)
        {
            notesInTrigger.Add(note);
            notesInTrigger.Sort((a, b) => a.hitTime.CompareTo(b.hitTime));
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        Note note = other.GetComponent<Note>();
        if (note != null && notesInTrigger.Contains(note))
        {
            notesInTrigger.Remove(note);
        }
    }
}
