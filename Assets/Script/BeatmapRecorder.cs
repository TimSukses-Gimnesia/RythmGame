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
            SaveJson($"C:/Users/piosg/Documents/GitHub/Body Frontier/Body Frontier/RythmGame/Assets/BeatmapJson/{fileName}");
        }
    }

    void CheckDirection(string dir, KeyControl key, double songTime)
    {
        if (key == null) return;

        if (key.wasPressedThisFrame)
            holdStart[dir] = songTime;

        if (key.wasReleasedThisFrame)
        {
            if (!holdStart.ContainsKey(dir)) return;

            double holdDurationSec = songTime - holdStart[dir];

            // convert detik -> beat
            float beat = (float)(songTime * BPM / 60.0);
            float holdBeat = (float)(holdDurationSec * BPM / 60.0);

            var n = new NoteData
            {
                beat = beat,
                dir = dir
            };

            if (Keyboard.current != null && Keyboard.current.zKey.isPressed)
            {
                n.type = "obstacle";
            }
            else if (holdDurationSec >= holdThresholdSec)
            {
                n.type = "hold";
                n.holdBeat = holdBeat;
            }
            else
            {
                n.type = "note";
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
