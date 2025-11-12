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
    public Transform effectsParent;
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

    // =============================
    // ✅ HEALTH MANAGEMENT
    // =============================
    void ApplyHealth(float delta)
    {
        float maxHP = FindFirstObjectByType<PlayerMovement>()?.maxHealth ?? 100f;
        health = Mathf.Clamp(health + delta, 0f, maxHP);
    }

    // =============================
    // ✅ HIT EFFECT (VISUAL)
    // =============================
    void SpawnHitEffect(Note note)
    {
        if (!enableHitEffect || hitEffectPrefab == null || note == null) return;

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
            Destroy(fx, ps.main.duration + ps.main.startLifetime.constantMax);
        else
            Destroy(fx, 1.2f);
    }

    // =============================
    // ✅ HIT SOUND (SFX)
    // =============================
    void PlayHitSFX(string judgement)
    {
        if (SFXManager.Instance == null) return;

        if (judgement == "Perfect" || judgement == "Good")
            SFXManager.Instance.PlayHit();
        else if (judgement == "Miss")
            SFXManager.Instance.PlayMiss();
        else if (judgement == "BREAK")
            SFXManager.Instance.PlayComboBreak();
    }

    // =============================
    // ✅ JUDGEMENT LOGIC
    // =============================
    private string GetJudgement(double timeDiff)
    {
        if (timeDiff <= perfectTime) return "Perfect";
        if (timeDiff <= goodTime) return "Good";
        return "Miss";
    }

    // =============================
    // ✅ INPUT HANDLER (ARROW + WASD)
    // =============================
    bool IsLaneKeyPressed()
    {
        if (Keyboard.current == null) return false;
        bool primary = Keyboard.current[targetKey].isPressed;
        bool alt = targetDirection switch
        {
            "up" => Keyboard.current.wKey?.isPressed ?? false,
            "down" => Keyboard.current.sKey?.isPressed ?? false,
            "left" => Keyboard.current.aKey?.isPressed ?? false,
            "right" => Keyboard.current.dKey?.isPressed ?? false,
            _ => false
        };
        return primary || alt;
    }

    bool WasLaneKeyPressedThisFrame()
    {
        if (Keyboard.current == null) return false;
        bool primary = Keyboard.current[targetKey].wasPressedThisFrame;
        bool alt = targetDirection switch
        {
            "up" => Keyboard.current.wKey?.wasPressedThisFrame ?? false,
            "down" => Keyboard.current.sKey?.wasPressedThisFrame ?? false,
            "left" => Keyboard.current.aKey?.wasPressedThisFrame ?? false,
            "right" => Keyboard.current.dKey?.wasPressedThisFrame ?? false,
            _ => false
        };
        return primary || alt;
    }

    bool WasLaneKeyReleasedThisFrame()
    {
        if (Keyboard.current == null) return false;
        bool primary = Keyboard.current[targetKey].wasReleasedThisFrame;
        bool alt = targetDirection switch
        {
            "up" => Keyboard.current.wKey?.wasReleasedThisFrame ?? false,
            "down" => Keyboard.current.sKey?.wasReleasedThisFrame ?? false,
            "left" => Keyboard.current.aKey?.wasReleasedThisFrame ?? false,
            "right" => Keyboard.current.dKey?.wasReleasedThisFrame ?? false,
            _ => false
        };
        return primary || alt;
    }

    // =============================
    // ✅ UPDATE LOOP
    // =============================
    void Update()
    {
        if (spawner == null || spawner.songStartDspTime == 0.0) return;
        double songTime = AudioSettings.dspTime - spawner.songStartDspTime;

        // --- auto MISS jika lewat timing window ---
        while (notesInTrigger.Count > 0 && songTime > notesInTrigger[0].hitTime + goodTime)
        {
            Note noteToMiss = notesInTrigger[0];
            notesInTrigger.RemoveAt(0);
            HandleMiss(noteToMiss);
        }

        // --- handle HOLD note ---
        if (currentlyHoldingNote != null)
        {
            double holdEndTime = currentlyHoldingNote.hitTime + currentlyHoldingNote.holdDurationSec;

            if (IsLaneKeyPressed())
            {
                if (!currentlyHoldingNote.isHolding)
                    currentlyHoldingNote.isHolding = true;

                if (songTime >= holdEndTime)
                    HandleHoldJudgement(true, currentlyHoldingNote);
                else
                    currentlyHoldingNote.UpdateHoldProgress(songTime);
            }

            if (WasLaneKeyReleasedThisFrame())
            {
                if (songTime < holdEndTime - goodTime)
                {
                    currentlyHoldingNote.holdBroken = true;
                    HandleHoldJudgement(false, currentlyHoldingNote);
                }
                else
                {
                    HandleHoldJudgement(true, currentlyHoldingNote);
                }
            }
        }

        // --- handle single tap notes ---
        if (currentlyHoldingNote == null && WasLaneKeyPressedThisFrame())
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
                        noteToHit.isHolding = true;
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

    // =============================
    // ✅ HANDLE HIT
    // =============================
    void HandleHit(string judgement, Note note, bool destroyNote)
    {
        combo++;
        int baseScore = (judgement == "Perfect") ? 200 : (judgement == "Good" ? 50 : 0);

        if (judgement == "Perfect") ApplyHealth(perfectHealthGain);
        else if (judgement == "Good") ApplyHealth(goodHealthGain);

        popup.Setup(judgement + "!", (judgement == "Perfect") ? Color.cyan : Color.green);
        popup.gameObject.SetActive(true);

        score += baseScore * combo;
        SpawnHitEffect(note);
        PlayHitSFX(judgement); // 🔊 play SFX

        note.isHit = true;
        if (destroyNote) Destroy(note.gameObject);
    }

    // =============================
    // ✅ HANDLE HOLD
    // =============================
    void HandleHoldJudgement(bool success, Note note)
    {
        if (success)
        {
            combo++;
            score += 150 * combo;
            ApplyHealth(holdSuccessGain);
            popup.Setup("HOLD OK!", Color.yellow);
            SpawnHitEffect(note);
            PlayHitSFX("Good"); // gunakan suara hit biasa
        }
        else
        {
            combo = 0;
            ApplyHealth(-holdBreakPenalty);
            popup.Setup("BREAK", Color.red);
            PlayHitSFX("BREAK"); // gunakan suara miss/combo break
        }

        popup.gameObject.SetActive(true);
        Destroy(note.gameObject);
        currentlyHoldingNote = null;
    }

    // =============================
    // ✅ HANDLE MISS
    // =============================
    void HandleMiss(Note note)
    {
        if (note == null || note.isHit) return;
        if (note == currentlyHoldingNote) currentlyHoldingNote = null;

        popup.Setup("MISS", Color.red);
        popup.gameObject.SetActive(true);

        combo = 0;
        ApplyHealth(-missHealthPenalty);
        PlayHitSFX("Miss"); // 🔊 suara miss

        note.isHit = true;
        Destroy(note.gameObject);
    }

    // =============================
    // ✅ COLLISION DETECTION
    // =============================
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
