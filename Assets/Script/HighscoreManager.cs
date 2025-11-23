using System;
using System.Collections.Generic;
using UnityEngine;

public static class HighscoreManager
{
    [Serializable]
    public class ScoreEntry
    {
        public long score;
        public string date; // Format string "DD-MM-YYYY"
    }

    private static string GetKey(string beatmapName)
    {
        return "Highscore_" + beatmapName;
    }

    // ====================================================================================
    // SIMPAN SCORE BARU
    // ====================================================================================
    public static void AddScore(string beatmapName, long newScore)
    {
        string key = GetKey(beatmapName);

        List<ScoreEntry> list = LoadTop3(beatmapName);

        // Tambah score baru
        list.Add(new ScoreEntry
        {
            score = newScore,
            date = DateTime.Now.ToString("dd-MM-yyyy")
        });

        // Sort besar ke kecil
        list.Sort((a, b) => b.score.CompareTo(a.score));

        // Ambil top 3 saja
        if (list.Count > 3)
            list = list.GetRange(0, 3);

        // Simpan kembali dalam JSON
        string json = JsonUtility.ToJson(new Wrapper { scores = list });
        PlayerPrefs.SetString(key, json);
        PlayerPrefs.Save();
    }

    // ====================================================================================
    // LOAD TOP 3
    // ====================================================================================
    public static List<ScoreEntry> LoadTop3(string beatmapName)
    {
        string key = GetKey(beatmapName);

        if (!PlayerPrefs.HasKey(key))
            return new List<ScoreEntry>();

        string json = PlayerPrefs.GetString(key);
        Wrapper wrapper = JsonUtility.FromJson<Wrapper>(json);

        return wrapper?.scores ?? new List<ScoreEntry>();
    }

    [Serializable]
    private class Wrapper
    {
        public List<ScoreEntry> scores;
    }
}
