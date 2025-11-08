using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class HitJudgement : MonoBehaviour
{
    [Header("Lane Settings")]
    public string targetDirection; // Set di Inspector: "up", "down", "left", "right"
    public Key targetKey;          // Set di Inspector: W, S, A, D

    [Header("Timing Windows (detik)")]
    public float perfectTime = 0.05f; // +/- 50ms
    public float goodTime = 0.1f;     // +/- 100ms

    [Header("UI Popup")]
    public HitPopup popup; // Slot untuk text popup yang sesuai

    private List<Note> notesInTrigger = new List<Note>();
    private SpawnNote spawner; // Referensi ke spawner utama

    // --- Variabel Skor & Combo ---
    public static int score = 0;
    public static int combo = 0;

    void Start()
    {
        // Cari Spawner saat mulai
        spawner = FindFirstObjectByType<SpawnNote>();

        // Reset skor tiap mulai
        score = 0;
        combo = 0;
    }

    void Update()
    {
        // 1. Cek jika spawner tidak ada atau belum siap
        if (spawner == null) return;
        if (spawner.songStartDspTime == 0.0) return;

        // 2. Hitung waktu lagu
        double songTime = AudioSettings.dspTime - spawner.songStartDspTime;

        // --- PEMBERSIH OTOMATIS (AUTO-MISS) ---
        // Membersihkan note yang terlewat (kadaluwarsa) dari antrian
        while (notesInTrigger.Count > 0 && songTime > notesInTrigger[0].hitTime + goodTime)
        {
            // Note ini sudah pasti "Miss" karena waktunya sudah lewat
            Note noteToMiss = notesInTrigger[0];
            notesInTrigger.RemoveAt(0); // Hapus dari antrian

            Debug.Log("Auto-Miss (kadaluwarsa)");
            HandleMiss(noteToMiss); // Proses sebagai "Miss"
        }

        // --- Cek Pencetan Tombol ---
        if (Keyboard.current != null && Keyboard.current[targetKey].wasPressedThisFrame)
        {
            // Cek jika ada note di antrian (yang sekarang sudah pasti TIDAK kadaluwarsa)
            if (notesInTrigger.Count > 0)
            {
                // Ambil note yang paling depan
                Note noteToHit = notesInTrigger[0];

                // LANGSUNG HAPUS note itu dari daftar.
                // Ini "mengkonsumsi" note & mencegah "penyumbatan"
                notesInTrigger.RemoveAt(0);

                // Hitung timing-nya
                double timeDiff = System.Math.Abs(songTime - noteToHit.hitTime);

                // Beri penilaian
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
                    // Ini adalah "Bad Timing" (pencet terlalu cepat/lambat)
                    Debug.Log("Bad Timing! (Miss) Selisih: " + timeDiff.ToString("F3"));
                    HandleMiss(noteToHit);
                }
            }
        }
    }

    // --- FUNGSI LOGIKA SKOR ---
    void HandleHit(string judgement, Note note)
    {
        // 1. Tambah combo
        combo++;

        // 2. Tentukan poin dasar & warna popup
        int baseScore = 0;
        Color popupColor = Color.white;

        if (judgement == "Perfect")
        {
            baseScore = 200; // Poin dasar Perfect
            popupColor = Color.cyan;
            Debug.Log("PERFECT!");
        }
        else if (judgement == "Good")
        {
            baseScore = 50;  // Poin dasar Good
            popupColor = Color.green;
            Debug.Log("GOOD!");
        }

        // 3. Panggil Popup UI
        popup.Setup(judgement + "!", popupColor);
        popup.gameObject.SetActive(true);

        // 4. Hitung skor = Poin Dasar * Kombo
        int pointsGained = baseScore * combo;

        // 5. Tambahkan ke skor total
        score += pointsGained;

        // 6. Tampilkan di Console (untuk cek)
        Debug.Log($"Score +{pointsGained}! Total: {score} (Combo: {combo})");

        // 7. Hancurkan note
        note.isHit = true;
        Destroy(note.gameObject);
    }

    void HandleMiss(Note note)
    {
        // Cek agar tidak memproses note yang sudah diproses
        if (note == null || note.isHit) return;

        // Tampilkan popup "MISS"
        popup.Setup("MISS", Color.red);
        popup.gameObject.SetActive(true);

        Debug.Log("MISS!");
        combo = 0; // Reset combo

        note.isHit = true;
        Destroy(note.gameObject); // Hancurkan note
    }

    // Dipanggil saat Note masuk ke Collider trigger
    private void OnTriggerEnter2D(Collider2D other)
    {
        Note note = other.GetComponent<Note>();
        // Cek apakah note ada, arahnya benar, dan belum di-hit
        if (note != null && note.dir == targetDirection && !note.isHit)
        {
            notesInTrigger.Add(note);
            // Urutkan agar note yang paling cepat hitTime-nya ada di depan
            notesInTrigger.Sort((a, b) => a.hitTime.CompareTo(b.hitTime));
        }
    }

    // Dipanggil saat Note keluar dari Collider trigger
    private void OnTriggerExit2D(Collider2D other)
    {
        Note note = other.GetComponent<Note>();
        // Jika note ini MASIH ADA di dalam daftar antrian kita...
        if (note != null && notesInTrigger.Contains(note))
        {
            // Jika note keluar trigger dan BELUM di-hit (seharusnya ini "Miss")
            if (!note.isHit)
            {
                Debug.Log("Miss (Keluar Trigger)");
                HandleMiss(note);
            }
            // Hapus dari daftar
            notesInTrigger.Remove(note);
        }
    }
}