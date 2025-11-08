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

    private List<Note> notesInTrigger = new List<Note>();
    private SpawnNote spawner;

    public static int score = 0;
    public static int combo = 0;
    public static float health; 

    void Start()
    {
        spawner = FindFirstObjectByType<SpawnNote>();

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
            Debug.Log("Auto-Miss (kadaluwarsa)");
            HandleMiss(noteToMiss);
        }

        if (Keyboard.current != null && Keyboard.current[targetKey].wasPressedThisFrame)
        {
            if (notesInTrigger.Count > 0)
            {
                Note noteToHit = notesInTrigger[0];
                notesInTrigger.RemoveAt(0);
                double timeDiff = System.Math.Abs(songTime - noteToHit.hitTime);

                if (timeDiff <= perfectTime)
                {
                    HandleHit("Perfect", noteToHit);
                }
                else if (timeDiff <= goodTime)
                {
                    HandleHit("Good", noteToHit);
                }
                else
                {
                    Debug.Log("Bad Timing! (Miss) Selisih: " + timeDiff.ToString("F3"));
                    HandleMiss(noteToHit);
                }
            }
        }
    }

    void HandleHit(string judgement, Note note)
    {
        combo++;

        int baseScore = 0;
        Color popupColor = Color.white;

        if (judgement == "Perfect")
        {
            baseScore = 200;
            popupColor = Color.cyan;
            Debug.Log("PERFECT!");
            health += perfectHealthGain; 
        }
        else if (judgement == "Good")
        {
            baseScore = 50;
            popupColor = Color.green;
            Debug.Log("GOOD!");
            health += goodHealthGain; 
        }

        popup.Setup(judgement + "!", popupColor);
        popup.gameObject.SetActive(true);

        int pointsGained = baseScore * combo;
        score += pointsGained;

        Debug.Log($"Score +{pointsGained}! Total: {score} (Combo: {combo})");

        note.isHit = true;
        Destroy(note.gameObject);
    }

    void HandleMiss(Note note)
    {
        if (note == null || note.isHit) return;

        popup.Setup("MISS", Color.red);
        popup.gameObject.SetActive(true);

        Debug.Log("MISS!");
        combo = 0;
        health -= missHealthPenalty; 

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
            if (!note.isHit)
            {
                Debug.Log("Miss (Keluar Trigger)");
                HandleMiss(note);
            }
            notesInTrigger.Remove(note);
        }
    }
}