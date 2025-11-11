using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using System.Linq; // Ditambahkan untuk parsing ExtraData

public static class OsuBeatmapLoader
{
    public class OsuChart
    {
        public string audioFilename;      // dari [General] AudioFilename
        public float audioLeadInSec;      // dari [General] AudioLeadIn (ms → detik)
        public List<OsuNote> notes = new List<OsuNote>();
    }

    public class OsuNote
    {
        public float timeSec;             // detik absolut sejak audio mulai
        public string dir;
        // Sekarang bisa: "note", "hold", atau "obstacle"
        public string type = "note"; 
        public float holdDurationSec = 0f;
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

        // --- [General]: AudioFilename & AudioLeadIn (Logic ini sudah benar) ---
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

        // --- [HitObjects]: Memproses Notes dan Obstacles ---
        bool inHit = false;
        foreach (var raw in all)
        {
            var s = raw.Trim();
            if (s.StartsWith("[HitObjects]")) { inHit = true; continue; }
            if (inHit && s.StartsWith("[")) break;
            if (!inHit || string.IsNullOrWhiteSpace(s)) continue;

            // Format: x,y,time,type,hitSound,objectParams,hitSample
            var parts = s.Split(',');
            if (parts.Length < 6) continue;

            // Parsing dasar
            int x, y, timeMs, type;
            if (!int.TryParse(parts[0], out x) || 
                !int.TryParse(parts[1], out y) ||
                !int.TryParse(parts[2], out timeMs) ||
                !int.TryParse(parts[3], out type))
            {
                continue; // Skip baris yang gagal parsing
            }


            // --- PENTING: LOGIKA FILTER BARU ---
            // 1. Abaikan Slider (2) dan Spinner (8) standar, kecuali jika itu adalah Obstacle kita (13).
            if (((type & 2) != 0 || (type & 8) != 0) && type != 13) continue;
            
            // Kita hanya peduli pada Hit Circle (1), Hold Note Mania (128), atau Custom Obstacle (13)
            if ((type & 1) == 0 && (type & 128) == 0 && type != 13) continue;
            // ------------------------------------


            float timeSec = timeMs / 1000f; // detik absolut sejak awal audio
            var note = new OsuNote
            {
                timeSec = timeSec
            };
            
            // --- Cek Tipe Khusus (Obstacle) ---
            if (type == 13)
            {
                note.type = "obstacle";
                // Ambil arah (left/up/right/down) dari ExtraData (parts[5])
                // Format: DIR:0:0:0:
                var dirParts = parts[5].Split(':');
                if (dirParts.Length > 0)
                {
                    // note.dir akan diisi dengan "left", "up", "down", dll.
                    note.dir = dirParts[0]; 
                }
                else
                {
                    // Fallback jika ExtraData tidak ada
                    note.dir = "up"; 
                }
            }
            // --- Cek Tipe Hold Note Mania (128) ---
            else if ((type & 128) != 0)
            {
                note.type = "hold";
                note.dir = XYToDirection(x, y); // Gunakan mapping XY
                
                // objectParams berisi endTime:hitSample
                var objParams = parts[5].Split(':');
                if (objParams.Length > 0 && int.TryParse(objParams[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int endTimeMs))
                {
                    float endTimeSec = endTimeMs / 1000f;
                    note.holdDurationSec = Mathf.Max(0f, endTimeSec - timeSec);
                }
                else
                {
                    // Fallback jika gagal parse endTime
                    note.type = "note";
                    note.holdDurationSec = 0f;
                }
            }
            // --- Note Biasa (Hit Circle) ---
            else 
            {
                note.type = "note";
                note.dir = XYToDirection(x, y); // Gunakan mapping XY
                note.holdDurationSec = 0f;
            }

            chart.notes.Add(note);
        }
        chart.notes.Sort((a, b) => a.timeSec.CompareTo(b.timeSec));
        return chart;
    }

    static string XYToDirection(int x, int y)
    {
        // Mapping ini digunakan untuk Note dan Hold Note
        // x=64 (lane 1), x=192 (lane 2), x=320 (lane 3), x=448 (lane 4)

        if (x < 100) return "left";  // (x=64)
        if (x < 250) return "down";  // (x=192)
        if (x < 400) return "up";    // (x=320)
        return "right";              // (x=448)
    }
}