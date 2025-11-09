using UnityEngine;
using System.Collections.Generic;
using System.IO;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class BeatmapRecorder : MonoBehaviour
{
    [Header("Tempo")]
    public float BPM = 142f;                  // set sesuai lagu
    [Tooltip("Ambang durasi hold (detik) untuk dicatat sebagai hold note")]
    public float holdThresholdSec = 0.3f;

    [Header("Output")]
    public string fileName = "recorded_beat.json"; // nama file output

    public List<NoteData> recordedNotes = new List<NoteData>();

    double songStartDspTime;
    bool isRec = false;

    // simpan waktu mulai hold per arah
    private readonly Dictionary<string, double> holdStart = new Dictionary<string, double>();

    void Start()
    {
        songStartDspTime = AudioSettings.dspTime;
        isRec = true;
    }

    void Update()
    {
        if (!isRec) return;

        double songTime = AudioSettings.dspTime - songStartDspTime;

        // rekam ke-empat arah
        CheckDirection("up",    Keyboard.current?.wKey, songTime);
        CheckDirection("down",  Keyboard.current?.sKey, songTime);
        CheckDirection("left",  Keyboard.current?.aKey, songTime);
        CheckDirection("right", Keyboard.current?.dKey, songTime);

        // simpan manual (R)
        if (Keyboard.current?.rKey != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            // Ini adalah path yang benar ke folder StreamingAssets
            string path = Path.Combine(Application.streamingAssetsPath, fileName);
            SaveJson(path);
        }
    }

    void CheckDirection(string dir, KeyControl key, double songTime)
    {
        if (key == null) return;

        if (key.wasPressedThisFrame)
            holdStart[dir] = songTime; // Simpan waktu MULAI

        if (key.wasReleasedThisFrame)
        {
            if (!holdStart.ContainsKey(dir)) return;

            // --- INI LOGIKA BARU ---
            double hitTimeSec = holdStart[dir]; // Waktu mulai adalah saat tombol ditekan
            double holdDurationSec = songTime - hitTimeSec; // Durasi adalah selisihnya

            var n = new NoteData
            {
                timeSec = (float)hitTimeSec, // Simpan waktu mulai
                dir = dir
            };
            // ---------------------

            if (Keyboard.current != null && Keyboard.current.zKey.isPressed)
            {
                n.type = "obstacle";
            }
            else if (holdDurationSec >= holdThresholdSec)
            {
                n.type = "hold";
                n.holdDurationSec = (float)holdDurationSec; // Simpan durasi hold
            }
            else
            {
                n.type = "note";
                n.holdDurationSec = 0f; // Note biasa tidak punya durasi
            }

            recordedNotes.Add(n);
            holdStart.Remove(dir);
        }
    }

    public void SaveJson(string path)
    {
        var ndl = new NoteDataList { notes = recordedNotes };
        string json = JsonUtility.ToJson(ndl, true);
        File.WriteAllText(path, json);
        Debug.Log($"Saved beatmap to: {path}  (notes: {recordedNotes.Count})");
    }
}
