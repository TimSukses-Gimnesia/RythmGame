using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class HitJudgement : MonoBehaviour
{
    // --- STATIC STATS ---
    public static long score = 0;
    public static int combo = 0;
    public static float health;

    public static int countPerfect;
    public static int countGood;
    public static int countMiss;

    [Header("Lane Settings")]
    public string targetDirection;
    public Key targetKey;

    [Header("Timing Windows (detik)")]
    public float perfectTime = 0.05f;
    public float goodTime = 0.1f;

    [Header("UI Popup & Sprites")]
    public GameObject popupPrefab;
    public Transform popupContainer;
    public Sprite perfectSprite;
    public Sprite goodSprite;
    public Sprite missSprite;
    public Sprite breakSprite;

    [Header("Scoring & Health Values")]
    public float perfectHealthGain = 10f;
    public float goodHealthGain = 5f;
    public float missHealthPenalty = 15f;
    public float holdBreakPenalty = 10f;
    public float holdSuccessGain = 2f;

    [Header("VFX")]
    public GameObject hitEffectPrefab;
    public Transform effectsParent;
    public string effectSortingLayer = "Default";
    public int effectSortingOrder = 20;
    public bool enableHitEffect = true;
    public GameObject chromaticBurstPrefab;

    // Internal Variables
    private List<Note> notesInTrigger = new List<Note>();
    private SpawnNote spawner;
    private Note currentlyHoldingNote = null;

    private PlayerPulse playerPulse;
    private Transform playerTransform;

    void Start()
    {
        spawner = FindFirstObjectByType<SpawnNote>();

        score = 0;
        combo = 0;
        countPerfect = 0;
        countGood = 0;
        countMiss = 0;

        health = FindFirstObjectByType<PlayerMovement>()?.maxHealth ?? 100f;

        playerPulse = FindFirstObjectByType<PlayerPulse>();
        playerTransform = FindFirstObjectByType<PlayerMovement>()?.transform;
    }

    public static float GetAccuracy()
    {
        int totalHits = countPerfect + countGood + countMiss;
        if (totalHits == 0) return 0f;
        float totalPoints = (countPerfect * 100f) + (countGood * 50f);
        float maxPoints = totalHits * 100f;
        return (totalPoints / maxPoints) * 100f;
    }


    private string GetJudgement(double timeDiff)
    {
        if (timeDiff <= perfectTime) return "Perfect";
        if (timeDiff <= goodTime) return "Good";
        return "Miss";
    }

    
    void PlayHitAnimation(Note note, string judgementType)
    {
        var effect = note.GetComponent<NoteHitEffect>();
        if (effect != null)
        {
            effect.PlayEffectAndDestroy(judgementType);
        }
        else
        {
            Destroy(note.gameObject);
        }
    }

    // --- LOGIC HIT NORMAL NOTE ---
    void HandleHit(string judgement, Note note, bool destroyNote)
    {
        combo++;
        int baseScore = (judgement == "Perfect") ? 200 : (judgement == "Good" ? 50 : 0);
        score += (long)baseScore * combo;

        Sprite spriteToShow = null;

        if (judgement == "Perfect")
        {
            ApplyHealth(perfectHealthGain);
            spriteToShow = perfectSprite;
            countPerfect++;
            CameraShake.Shake(0.08f, 0.07f);
            if (playerPulse != null) playerPulse.Pulse(0.60f);
            if (chromaticBurstPrefab != null && playerTransform != null)
                Instantiate(chromaticBurstPrefab, playerTransform.position, Quaternion.identity, effectsParent);
        }
        else if (judgement == "Good")
        {
            ApplyHealth(goodHealthGain);
            spriteToShow = goodSprite;
            countGood++;
            CameraShake.Shake(0.05f, 0.04f);
            if (playerPulse != null) playerPulse.Pulse(0.30f);
        }

        if (note.type == "hold") note.initialJudgement = judgement;

        SpawnPopup(spriteToShow);
        SpawnHitEffect(note);
        PlayHitSFX(judgement);

        note.isHit = true;

        if (destroyNote)
        {
            PlayHitAnimation(note, judgement);
        }
    }

    // --- LOGIC HOLD NOTE ---
    void HandleHoldJudgement(bool success, Note note)
    {
        Sprite spriteToShow = null;
        string finalJudgement = "Good";

        if (success)
        {
            combo++;
            score += 150 * combo;
            ApplyHealth(holdSuccessGain);

            if (note.initialJudgement == "Good")
            {
                spriteToShow = goodSprite;
                countGood++;
                PlayHitSFX("Good");
                finalJudgement = "Good";
            }
            else
            {
                spriteToShow = perfectSprite;
                countPerfect++;
                finalJudgement = "Perfect";
            }
            CameraShake.Shake(0.05f, 0.04f);
        }
        else
        {
            combo = 0;
            ApplyHealth(-holdBreakPenalty);
            spriteToShow = (breakSprite != null) ? breakSprite : missSprite;
            countMiss++;
            PlayHitSFX("BREAK");
            finalJudgement = "Miss";
        }

        SpawnPopup(spriteToShow);
        PlayHitAnimation(note, finalJudgement);
        currentlyHoldingNote = null;
    }

    // --- LOGIC MISS ---
    void HandleMiss(Note note)
    {
        if (note == null || note.isHit || note.type == "decoy") return;
        if (note == currentlyHoldingNote) currentlyHoldingNote = null;

        SpawnPopup(missSprite);
        combo = 0;
        ApplyHealth(-missHealthPenalty);
        PlayHitSFX("Miss");
        countMiss++;
        DamageEffect.Instance.TriggerFlash();

        note.isHit = true;
        Destroy(note.gameObject);
    }

    // --- UPDATE LOOP ---
    void Update()
    {
        if (spawner == null || spawner.songStartDspTime == 0.0) return;
        double songTime = AudioSettings.dspTime - spawner.songStartDspTime;

        // 1. Cek Miss
        while (notesInTrigger.Count > 0 && songTime > notesInTrigger[0].hitTime + goodTime)
        {
            Note noteToMiss = notesInTrigger[0];
            notesInTrigger.RemoveAt(0);

            if (noteToMiss.type != "decoy") HandleMiss(noteToMiss);
            else Destroy(noteToMiss.gameObject);
        }

        // 2. Logic Hold Note
        if (currentlyHoldingNote != null)
        {
            double holdEndTime = currentlyHoldingNote.hitTime + currentlyHoldingNote.holdDurationSec;

            if (IsLaneKeyPressed())
            {
                if (!currentlyHoldingNote.isHolding) currentlyHoldingNote.isHolding = true;
                if (songTime >= holdEndTime) HandleHoldJudgement(true, currentlyHoldingNote);
                else currentlyHoldingNote.UpdateHoldProgress(songTime);
            }

            if (WasLaneKeyReleasedThisFrame())
            {
                if (songTime < holdEndTime - goodTime)
                {
                    currentlyHoldingNote.holdBroken = true;
                    HandleHoldJudgement(false, currentlyHoldingNote);
                }
                else HandleHoldJudgement(true, currentlyHoldingNote);
            }
        }

        // 3. Logic Tap Note
        if (currentlyHoldingNote == null && WasLaneKeyPressedThisFrame())
        {
            if (notesInTrigger.Count > 0)
            {
                Note noteToHit = notesInTrigger[0];

           
                if (noteToHit.type == "decoy")
                {
                    notesInTrigger.RemoveAt(0);
                    Destroy(noteToHit.gameObject);
                    return;
                }

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

    // --- INPUT & VISUAL HELPERS ---
    bool IsLaneKeyPressed() { if (Keyboard.current == null) return false; bool primary = Keyboard.current[targetKey].isPressed; bool alt = targetDirection switch { "up" => Keyboard.current.wKey?.isPressed ?? false, "down" => Keyboard.current.sKey?.isPressed ?? false, "left" => Keyboard.current.aKey?.isPressed ?? false, "right" => Keyboard.current.dKey?.isPressed ?? false, _ => false }; return primary || alt; }
    bool WasLaneKeyPressedThisFrame() { if (Keyboard.current == null) return false; bool primary = Keyboard.current[targetKey].wasPressedThisFrame; bool alt = targetDirection switch { "up" => Keyboard.current.wKey?.wasPressedThisFrame ?? false, "down" => Keyboard.current.sKey?.wasPressedThisFrame ?? false, "left" => Keyboard.current.aKey?.wasPressedThisFrame ?? false, "right" => Keyboard.current.dKey?.wasPressedThisFrame ?? false, _ => false }; return primary || alt; }
    bool WasLaneKeyReleasedThisFrame() { if (Keyboard.current == null) return false; bool primary = Keyboard.current[targetKey].wasReleasedThisFrame; bool alt = targetDirection switch { "up" => Keyboard.current.wKey?.wasReleasedThisFrame ?? false, "down" => Keyboard.current.sKey?.wasReleasedThisFrame ?? false, "left" => Keyboard.current.aKey?.wasReleasedThisFrame ?? false, "right" => Keyboard.current.dKey?.wasReleasedThisFrame ?? false, _ => false }; return primary || alt; }

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
        if (note != null && notesInTrigger.Contains(note)) notesInTrigger.Remove(note);
    }

    void SpawnPopup(Sprite sprite) { if (popupPrefab && sprite && popupContainer) { var newPopup = Instantiate(popupPrefab, popupContainer); newPopup.transform.localPosition = Vector3.zero; var hp = newPopup.GetComponent<HitPopup>(); if (hp) hp.Setup(sprite); } }
    void SpawnHitEffect(Note note) { if (!enableHitEffect || !hitEffectPrefab) return; var fx = Instantiate(hitEffectPrefab, note.targetPos, Quaternion.identity, effectsParent); var ps = fx.GetComponent<ParticleSystem>(); var psR = fx.GetComponent<ParticleSystemRenderer>(); if (psR) { psR.sortingLayerName = effectSortingLayer; psR.sortingOrder = effectSortingOrder; } if (ps) Destroy(fx, ps.main.duration + ps.main.startLifetime.constantMax); else Destroy(fx, 1f); }
    void PlayHitSFX(string judgement) { if (SFXManager.Instance == null) return; if (judgement == "Perfect" || judgement == "Good") SFXManager.Instance.PlayHit(); else if (judgement == "Miss") SFXManager.Instance.PlayMiss(); else if (judgement == "BREAK") SFXManager.Instance.PlayComboBreak(); }
    void ApplyHealth(float delta) { float maxHP = FindFirstObjectByType<PlayerMovement>()?.maxHealth ?? 100f; health = Mathf.Clamp(health + delta, 0f, maxHP); }
}