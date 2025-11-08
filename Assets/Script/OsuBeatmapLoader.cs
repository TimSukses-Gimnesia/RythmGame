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
        public string dir;              // "up"/"down"/"left"/"right"
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
                    chart.audioFilename = s.Substring(idx+1).Trim().Trim('\"');
            }
            else if (s.StartsWith("AudioLeadIn", StringComparison.OrdinalIgnoreCase))
            {
                int idx = s.IndexOf(':');
                if (idx >= 0)
                {
                    var val = s.Substring(idx+1).Trim();
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
            if (parts.Length < 5) continue;

            int x = int.Parse(parts[0]);
            int y = int.Parse(parts[1]);
            int timeMs = int.Parse(parts[2]);
            int type = int.Parse(parts[3]);

            // Abaikan slider (bit 1) & spinner (bit 3)
            if ((type & 2) != 0 || (type & 8) != 0) continue;

            float timeSec = timeMs / 1000f; // detik absolut sejak awal audio
            chart.notes.Add(new OsuNote {
                timeSec = timeSec,
                dir = XYToDirection(x, y)
            });
        }
        chart.notes.Sort((a, b) => a.timeSec.CompareTo(b.timeSec));
        return chart;
    }

    static string XYToDirection(int x, int y)
    {
        // Asumsi playfield osu! standard ~512x384
        if (y < 128) return "up";
        if (y > 256) return "down";
        if (x < 170) return "left";
        if (x > 340) return "right";
        return "up";
    }
}
