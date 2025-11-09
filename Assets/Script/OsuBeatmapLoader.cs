using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

public static class OsuBeatmapLoader
{
    public class OsuChart
    {
        public string audioFilename;    // dari [General] AudioFilename
        public float audioLeadInSec;    // dari [General] AudioLeadIn (ms → detik)
        public List<OsuNote> notes = new List<OsuNote>();
    }

    public class OsuNote
    {
        public float timeSec;           // detik absolut sejak audio mulai
        public string dir;
        public string type = "note"; // "note" atau "hold"
        public float holdDurationSec = 0f;// "up"/"down"/"left"/"right"
    }

    public static OsuChart Load(TextAsset osuFile)
    {
        if (osuFile == null) throw new Exception("osuFile is null");
        var chart = new OsuChart();
        var all = new List<string>();
        using (var sr = new StringReader(osuFile.text))
        {
            string line;
            while ((line = sr.ReadLine()) != null) all.Add(line);
        }

        // --- [General]: AudioFilename & AudioLeadIn ---
        bool inGeneral = false;
        foreach (var raw in all)
        {
            var s = raw.Trim();
            if (s.StartsWith("[General]")) { inGeneral = true; continue; }
            if (inGeneral && s.StartsWith("[")) break;
            if (!inGeneral) continue;

            if (s.StartsWith("AudioFilename", StringComparison.OrdinalIgnoreCase))
            {
                int idx = s.IndexOf(':');
                if (idx >= 0)
                    chart.audioFilename = s.Substring(idx + 1).Trim().Trim('\"');
            }
            else if (s.StartsWith("AudioLeadIn", StringComparison.OrdinalIgnoreCase))
            {
                int idx = s.IndexOf(':');
                if (idx >= 0)
                {
                    var val = s.Substring(idx + 1).Trim();
                    if (int.TryParse(val, NumberStyles.Integer, CultureInfo.InvariantCulture, out int ms))
                        chart.audioLeadInSec = Mathf.Max(0f, ms / 1000f);
                }
            }
        }

        // --- [HitObjects]: hanya hitcircle ---
        bool inHit = false;
        foreach (var raw in all)
        {
            var s = raw.Trim();
            if (s.StartsWith("[HitObjects]")) { inHit = true; continue; }
            if (inHit && s.StartsWith("[")) break;
            if (!inHit || string.IsNullOrWhiteSpace(s)) continue;

            // Format: x,y,time,type,hitSound,objectParams,hitSample
            var parts = s.Split(',');
            if (parts.Length < 6) continue; // Butuh setidaknya 6 bagian untuk objectParams

            int x = int.Parse(parts[0]);
            int y = int.Parse(parts[1]);
            int timeMs = int.Parse(parts[2]);
            int type = int.Parse(parts[3]);

            // Abaikan slider standar (bit 1) & spinner (bit 3)
            // INI PENTING: Jangan abaikan hold note mania (bit 7 / 128)
            if ((type & 2) != 0 || (type & 8) != 0) continue;

            float timeSec = timeMs / 1000f; // detik absolut sejak awal audio
            var note = new OsuNote
            {
                timeSec = timeSec,
                dir = XYToDirection(x, y)
            };

            // Cek apakah ini hold note (osu!mania)
            if ((type & 128) != 0)
            {

                note.type = "hold";

                // objectParams berisi endTime:hitSample
                var objParams = parts[5].Split(':');
                if (objParams.Length > 0 && int.TryParse(objParams[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int endTimeMs))
                {
                    float endTimeSec = endTimeMs / 1000f;
                    note.holdDurationSec = Mathf.Max(0f, endTimeSec - timeSec);
                }
                else
                {
                    // Gagal parse endTime, anggap sebagai note biasa
                    note.type = "note";
                    note.holdDurationSec = 0f;
                }
            }
            else
            {
                // Ini adalah note biasa (hit circle)
                note.type = "note";
                note.holdDurationSec = 0f;
            }

            chart.notes.Add(note);
        }
        chart.notes.Sort((a, b) => a.timeSec.CompareTo(b.timeSec));
        return chart;
    }

    static string XYToDirection(int x, int y)
    {
        // Ini adalah mapping standar untuk 4-key (4K) osu!mania
        // x=64 (lane 1), x=192 (lane 2), x=320 (lane 3), x=448 (lane 4)

        if (x < 100) return "left";  // (x=64)
        if (x < 250) return "down";  // (x=192) <--- INI AKAN MEMPERBAIKINYA
        if (x < 400) return "up";    // (x=320)
        return "right";             // (x=448)
    }
}
