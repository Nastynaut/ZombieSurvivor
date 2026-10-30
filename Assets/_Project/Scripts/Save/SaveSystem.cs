using System;
using System.IO;
using UnityEngine;

namespace KnightSurvivors.Save
{
    /// <summary>
    /// Loads and saves MetaSaveData as a JSON file in Application.persistentDataPath
    /// (on Windows: %USERPROFILE%\AppData\LocalLow\&lt;Company&gt;\&lt;Product&gt;\meta.json).
    ///
    /// SAFE WRITES: we write to "meta.json.tmp" first and only then swap it in. If the game crashes
    /// mid-save, the old meta.json is still intact. A copy of the previous save is kept as meta.json.bak.
    ///
    /// Plain class (not a MonoBehaviour): the RunManager creates one and registers it in the ServiceLocator.
    /// </summary>
    public class SaveSystem
    {
        public const string FileName = "meta.json";

        public MetaSaveData Data { get; private set; } = new MetaSaveData();

        public string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        public MetaSaveData Load()
        {
            string path = FilePath;
            Data = TryRead(path) ?? TryRead(path + ".bak") ?? new MetaSaveData();
            return Data;
        }

        private static MetaSaveData TryRead(string path)
        {
            try
            {
                if (!File.Exists(path)) return null;
                string json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json)) return null;
                return JsonUtility.FromJson<MetaSaveData>(json);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveSystem] Could not read {path}: {e.Message}");
                return null;
            }
        }

        public void Save()
        {
            string path = FilePath;
            string temp = path + ".tmp";
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(temp, JsonUtility.ToJson(Data, true));
                if (File.Exists(path))
                {
                    // Atomically swap in the new file and keep the old one as a backup.
                    File.Replace(temp, path, path + ".bak");
                }
                else
                {
                    File.Move(temp, path);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Could not save {path}: {e.Message}");
            }
        }
    }
}
