    using System.IO;
    using System.Text;
    using UnityEngine;
    using UnityEngine.InputSystem;

    public class JsonToOsuConverter : MonoBehaviour
    {
        [Header("Input (dari Recorder)")]
        public string inputJsonFile = "recorded_beat.json";

        [Header("Output (untuk Loader)")]
        public string outputOsuFile = "MyNewBeatmap.txt"; // (Nama .txt untuk Unity)

        [Header("Audio (untuk disalin ke .osu)")]
        public string audioFileNameInOsu = "audio.mp3"; // Nama file audio Anda

        void Update()
        {
            // Tekan 'C' untuk Konversi (Convert)
            if (Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame)
            {
                string jsonPath = Path.Combine(Application.streamingAssetsPath, inputJsonFile);
                string osuPath = Path.Combine(Application.streamingAssetsPath, outputOsuFile);

                Convert(jsonPath, osuPath);
            }
        }

        public void Convert(string jsonPath, string osuPath)
        {
            Debug.Log($"Membaca {jsonPath}...");
            if (!File.Exists(jsonPath))
            {
                Debug.LogError("File JSON tidak ditemukan!");
                return;
            }

            string json = File.ReadAllText(jsonPath);
            NoteDataList noteList = JsonUtility.FromJson<NoteDataList>(json);

            // --- Mulai membuat file .osu ---
            StringBuilder sb = new StringBuilder();

            // Salin header dasar dari file .osu yang ada
            sb.AppendLine("osu file format v14");
            sb.AppendLine("[General]");
            sb.AppendLine("AudioFilename: " + audioFileNameInOsu);
            sb.AppendLine("AudioLeadIn: 0");
            sb.AppendLine("Mode: 3"); // Mode 3 = osu!mania
            sb.AppendLine("[Difficulty]");
            sb.AppendLine("CircleSize: 4"); // 4 keys
            sb.AppendLine("[TimingPoints]");
            sb.AppendLine("0,500,4,1,0,100,1,0"); // Timing point sederhana

            // --- Ini bagian penting: [HitObjects] ---
            sb.AppendLine("[HitObjects]");

            noteList.notes.Sort((a, b) => a.timeSec.CompareTo(b.timeSec)); // Urutkan note

            foreach (var note in noteList.notes)
            {
                if (note.type == "obstacle") continue; // Abaikan obstacle

                int x = DirToX(note.dir);
                int y = 192; // Y selalu 192 di mania
                int timeMs = (int)(note.timeSec * 1000);
                int type = (note.type == "hold") ? 128 : 1;

                // Hitung endTime untuk hold note
                int endTimeMs = 0;
                if (note.type == "hold")
                {
                    endTimeMs = (int)((note.timeSec + note.holdDurationSec) * 1000);
                }

                // Format: x,y,time,type,hitSound,objectParams,hitSample
                sb.AppendLine($"{x},{y},{timeMs},{type},0,{endTimeMs}:0:0:0:0:");
            }

            // Tulis file .osu baru
            File.WriteAllText(osuPath, sb.ToString());
            Debug.Log($"BERHASIL! Beatmap baru disimpan di {osuPath}");
        }

        // Helper untuk mengubah "left" -> 64, dll.
        private int DirToX(string dir)
        {
            switch (dir)
            {
                case "left": return 64;
                case "down": return 192;
                case "up": return 320;
                case "right": return 448;
                default: return 64; // Default ke kiri
            }
        }
    }