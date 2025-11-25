using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using System.Linq;

public static class OsuBeatmapLoader
{
    public class OsuChart
    {
        public string audioFilename;      // Nama file lagu
        public float audioLeadInSec;      // Lead-in time
        public List<OsuNote> notes = new List<OsuNote>(); // Daftar semua note
        public List<TimingPoint> timingPoints = new List<TimingPoint>(); // 🔥 Data BPM & Kiai
    }

    // Class untuk menyimpan data Timing (BPM & Reff)
    public class TimingPoint
    {
        public float timeSec;
        public float beatLengthSec; // Durasi satu ketukan (detik)
        public bool isKiai;         // Apakah ini bagian Reff?
    }

    public class OsuNote
    {
        public float timeSec;
        public string dir;
        public string type = "note"; // "note", "hold", "obstacle", "decoy"
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

        // --- 1. Parsing [General] ---
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
                if (idx >= 0) chart.audioFilename = s.Substring(idx + 1).Trim().Trim('\"');
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

        // --- 2. Parsing [TimingPoints] (BPM & Kiai) ---
        bool inTiming = false;
        foreach (var raw in all)
        {
            var s = raw.Trim();
            if (s.StartsWith("[TimingPoints]")) { inTiming = true; continue; }
            if (inTiming && s.StartsWith("[")) break;
            if (!inTiming || string.IsNullOrWhiteSpace(s)) continue;

            var parts = s.Split(',');
            if (parts.Length < 2) continue;

            // Format .osu: time,beatLength,meter,sampleSet,sampleIndex,volume,uninherited,effects
            if (float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float timeMs) &&
                float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float beatLengthMs))
            {
                // beatLength positif = Perubahan BPM (Red Line)
                // beatLength negatif = Perubahan Speed/Volume (Green Line) -> Kita tetap ambil untuk data Kiai

                bool kiai = false;
                // Cek kolom ke-8 (index 7) untuk Effects Flag
                if (parts.Length >= 8)
                {
                    if (int.TryParse(parts[7], out int effects))
                    {
                        // Bit 0 (ganjil) menandakan Kiai Time aktif
                        kiai = (effects & 1) != 0;
                    }
                }

                // Kita simpan data timing yang relevan (BPM change) atau Event change (Kiai)
                if (beatLengthMs > 0) // Hanya ambil BPM utama untuk kalkulasi Decoy
                {
                    chart.timingPoints.Add(new TimingPoint
                    {
                        timeSec = timeMs / 1000f,
                        beatLengthSec = beatLengthMs / 1000f,
                        isKiai = kiai
                    });
                }
                else // Green line (Inherited), ambil data Kiai-nya saja, beatLength pakai previous
                {
                    // Untuk simplifikasi decoy, kita skip logic complex green line beatLength,
                    // tapi kita tetap butuh data Kiai start/end dari sini.
                    // (Implementasi Kiai Manager nanti akan membaca list ini urut waktu)
                    chart.timingPoints.Add(new TimingPoint
                    {
                        timeSec = timeMs / 1000f,
                        beatLengthSec = -1, // Penanda ini inherited
                        isKiai = kiai
                    });
                }
            }
        }

        // --- 3. Parsing [HitObjects] ---
        bool inHit = false;
        foreach (var raw in all)
        {
            var s = raw.Trim();
            if (s.StartsWith("[HitObjects]")) { inHit = true; continue; }
            if (inHit && s.StartsWith("[")) break;
            if (!inHit || string.IsNullOrWhiteSpace(s)) continue;

            var parts = s.Split(',');
            if (parts.Length < 6) continue;

            int x, y, timeMs, type;
            if (!int.TryParse(parts[0], out x) ||
                !int.TryParse(parts[1], out y) ||
                !int.TryParse(parts[2], out timeMs) ||
                !int.TryParse(parts[3], out type)) continue;

            // Filter Slider/Spinner standar osu! (kecuali type 13 Obstacle kita)
            if (((type & 2) != 0 || (type & 8) != 0) && type != 13) continue;
            if ((type & 1) == 0 && (type & 128) == 0 && type != 13) continue;

            float timeSec = timeMs / 1000f;
            var note = new OsuNote { timeSec = timeSec };

            if (type == 13) // Custom Obstacle
            {
                note.type = "obstacle";
                var dirParts = parts[5].Split(':');
                note.dir = (dirParts.Length > 0) ? dirParts[0] : "up";
            }
            else if ((type & 128) != 0) // Hold Note
            {
                note.type = "hold";
                note.dir = XYToDirection(x, y);
                var objParams = parts[5].Split(':');
                if (objParams.Length > 0 && int.TryParse(objParams[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int endTimeMs))
                {
                    note.holdDurationSec = Mathf.Max(0f, (endTimeMs / 1000f) - timeSec);
                }
            }
            else // Normal Note
            {
                note.type = "note";
                note.dir = XYToDirection(x, y);
                note.holdDurationSec = 0f;
            }

            chart.notes.Add(note);
        }

        chart.notes.Sort((a, b) => a.timeSec.CompareTo(b.timeSec));
        chart.timingPoints.Sort((a, b) => a.timeSec.CompareTo(b.timeSec)); // Penting urut waktu

        return chart;
    }

    static string XYToDirection(int x, int y)
    {
        if (x < 100) return "left";
        if (x < 250) return "down";
        if (x < 400) return "up";
        return "right";
    }
}