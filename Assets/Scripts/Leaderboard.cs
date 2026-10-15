using System;
using System.Collections.Generic;
using UnityEngine;

namespace Shatterline
{
    // Local leaderboard: one best completed run per player name on this computer.
    public static class Leaderboard
    {
        [Serializable]
        public sealed class Entry
        {
            public string name;
            public int score;
            public int seconds;
            public int deaths;
        }

        private const string Key = "Shatterline.Leaderboard.v2.";

        public static List<Entry> Read()
        {
            try
            {
                var entries = new List<Entry>();
                int count = Math.Min(5000, Math.Max(0, PlayerPrefs.GetInt(Key + "Count", 0)));
                for (int i = 0; i < count; i++)
                {
                    string slot = Key + i + ".";
                    string name = PlayerPrefs.GetString(slot + "Name", "");
                    if (string.IsNullOrEmpty(name)) continue;
                    entries.Add(new Entry
                    {
                        name = name,
                        score = PlayerPrefs.GetInt(slot + "Score", 0),
                        seconds = PlayerPrefs.GetInt(slot + "Seconds", 0),
                        deaths = PlayerPrefs.GetInt(slot + "Deaths", 0)
                    });
                }
                entries.Sort((a, b) => b.score.CompareTo(a.score));
                return entries;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Could not read local leaderboard: " + ex.Message);
                return new List<Entry>();
            }
        }

        public static int SaveBest(string playerName, int score, int seconds, int deaths)
        {
            playerName = (playerName ?? "").Trim();
            if (playerName.Length == 0) return 0;
            if (playerName.Length > 16) playerName = playerName.Substring(0, 16);
            // Strip control characters so a name cannot break the compact UI.
            playerName = new string(Array.FindAll(playerName.ToCharArray(), c => !char.IsControl(c)));
            if (playerName.Length == 0) return 0;
            var entries = Read();
            int oldIndex = entries.FindIndex(e => string.Equals(e.name, playerName, StringComparison.OrdinalIgnoreCase));
            if (oldIndex >= 0)
            {
                if (entries[oldIndex].score >= score) return oldIndex + 1;
                entries.RemoveAt(oldIndex);
            }
            entries.Add(new Entry { name = playerName, score = score, seconds = seconds, deaths = deaths });
            entries.Sort((a, b) => b.score.CompareTo(a.score));
            try
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    string slot = Key + i + ".";
                    PlayerPrefs.SetString(slot + "Name", entries[i].name);
                    PlayerPrefs.SetInt(slot + "Score", entries[i].score);
                    PlayerPrefs.SetInt(slot + "Seconds", entries[i].seconds);
                    PlayerPrefs.SetInt(slot + "Deaths", entries[i].deaths);
                }
                PlayerPrefs.SetInt(Key + "Count", entries.Count);
                PlayerPrefs.Save();
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Could not save local leaderboard: " + ex.Message);
                return -1;
            }
            return entries.FindIndex(e => string.Equals(e.name, playerName, StringComparison.OrdinalIgnoreCase)) + 1;
        }

        public static string[] TopTenCells()
        {
            var cells = new string[55];
            for (int i = 0; i < cells.Length; i++) cells[i] = "";
            string[] headings = { "#", "PLAYER", "SCORE", "TIME", "DEATHS" };
            for (int i = 0; i < 5; i++) cells[i] = headings[i];
            var rows = Read();
            for (int i = 0; i < rows.Count && i < 10; i++)
            {
                var row = rows[i]; int offset = (i + 1) * 5;
                cells[offset] = (i + 1).ToString(); cells[offset + 1] = row.name;
                cells[offset + 2] = row.score.ToString();
                cells[offset + 3] = row.seconds / 60 + ":" + (row.seconds % 60).ToString("00");
                cells[offset + 4] = row.deaths.ToString();
            }
            if (rows.Count == 0) cells[6] = "No completed runs.";
            return cells;
        }
    }
}
