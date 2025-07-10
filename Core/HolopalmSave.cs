using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace HolopalmPlus
{
    public class HolopalmSave
    {
        private static HolopalmSave _instance;
        private static bool isLoaded;
        private const int currentVersion = 1;
        public const string filename = "HolopalmSave.json";
        public const string filenameBackup = "HolopalmSave.bak";
        public int saveFileVersion = 1;

        public Dictionary<string, bool> settings = new()
        {
            { "showMapStoryOverlay", true },
            { "showJobPreview", true },
            { "frequentStories", true },
            { "glowJobStories", true },
            { "showSeenText", true },
            { "extendedOutcomePreview", true },
            { "extendedCharaBio", true },
            { "giftInfo", true }
        };

        public Dictionary<string, List<string>> giftedCards = new();

        public Dictionary<string, bool> overlayFilters = new()
        {
            { "charaStory", true },
            { "charaInteraction", true },
            { "charaBirthday", true },
            { "custom", true },
            // { "socksStory", true },
            { "ignoredItems", false }
        };

        public static HolopalmSave instance
        {
            get
            {
                _instance ??= new HolopalmSave();

                return _instance;
            }
            set
            {
                _instance = value;
            }
        }

        public static void UpdateSettings(string key, bool value)
        {
            if (instance.settings.ContainsKey(key))
            {
                instance.settings[key] = value;
            }
            else
            {
                instance.settings.Add(key, value);
            }

            Save();
            UpdateSettingSideEffect(key, value);
        }

        private static void UpdateSettingSideEffect(string key, bool value)
        {
            if (key == "showMapStoryOverlay")
            {
                MapStoryPatches.GetOverlayInstance()?.SetOverlayActive(value, true);
            }
        }

        public static bool GetSetting(string key, bool defaultValue = false)
        {
            if (instance.settings.TryGetValue(key, out bool value))
            {
                return value;
            }

            return defaultValue;
        }

        public static void SetOverlayFilter(string key, bool value)
        {
            if (instance.overlayFilters.ContainsKey(key))
            {
                instance.overlayFilters[key] = value;
            }
            else
            {
                instance.overlayFilters.Add(key, value);
            }

            Save();
        }

        public static bool GetOverlayFilter(string key, bool defaultValue = true)
        {
            if (instance.overlayFilters.TryGetValue(key, out bool value))
            {
                return value;
            }

            return defaultValue;
        }

        public static void AddGiftedCard(string character, string card)
        {
            if (!instance.giftedCards.ContainsKey(character))
            {
                instance.giftedCards[character] = [];
            }

            if (instance.giftedCards[character].Contains(card))
            {
                return;
            }

            instance.giftedCards[character].Add(card);

            Save();
        }

        public static bool HasGiftedCard(string character, string card)
        {
            if (instance.giftedCards.TryGetValue(character, out List<string> cards))
            {
                return cards.Contains(card);
            }

            return false;
        }

        public void Load()
        {
            try
            {
                LoadInner();
            }
            catch (Exception ex)
            {
                ModInstance.Log("HolopalmSave.Load error during FromJsonOverwrite: " + ex);
                isLoaded = true;
                Save();
            }
        }

        public void LoadInner()
        {
            bool flag = false;
            string text = FileManager.LoadFileString(filename, FileManager.documentsPath, warnFileMissing: false);
            if (text.IsNullOrEmptyOrWhitespace())
            {
                text = FileManager.LoadFileString(filenameBackup, FileManager.documentsPath, warnFileMissing: false);
                flag = true;
                if (text.IsNullOrEmptyOrWhitespace())
                {
                    ModInstance.Log($"{filename} not found, no backup, must be a fresh install");
                    isLoaded = true;
                    Save();
                    return;
                }

                ModInstance.Log($"{filename} was empty, loaded from backup {filenameBackup}");
            }

            try
            {
                JsonConvert.PopulateObject(text, this);
            }
            catch (Exception ex)
            {
                if (flag)
                {
                    ModInstance.Log($"{filename} file is corrupt! Resetting to defaults. " + ex.Message);
                    isLoaded = true;
                    Save();
                    return;
                }

                text = FileManager.LoadFileString(filenameBackup, FileManager.documentsPath, warnFileMissing: false);
                flag = true;
                if (text.IsNullOrEmptyOrWhitespace())
                {
                    ModInstance.Log($"{filenameBackup} file is corrupt, no backup. Resetting to defaults. " + ex.Message);
                    isLoaded = true;
                    Save();
                    return;
                }

                try
                {
                    JsonConvert.PopulateObject(text, this);
                }
                catch (Exception ex2)
                {
                    ModInstance.Log($"{filename} file and backup are corrupt! Resetting to defaults. " + ex2.Message);
                    isLoaded = true;
                    Save();
                    return;
                }
            }

            if (saveFileVersion != currentVersion)
            {
                ModInstance.Log($"{filename} version changed from " + saveFileVersion + " to " + currentVersion);
                saveFileVersion = currentVersion;
            }

            isLoaded = true;
            if (flag)
            {
                Save();
            }
        }

        private void SaveThread(string path)
        {
            FileManager.SaveFile(JsonConvert.SerializeObject(this), filename, path);
        }

        public static void Save(bool threaded = true)
        {
            try
            {
                if (!isLoaded)
                {
                    ModInstance.Log("HolopalmSave.Save but hasn't been loaded yet, not saving");
                    return;
                }

                HolopalmSave clone = instance.DeepClone();
                string path = FileManager.documentsPath;
                if (threaded)
                {
                    ThreadWorker.ExecuteInThread(delegate
                    {
                        clone.SaveThread(path);
                    }, ThreadProcessID.saveFile);
                }
                else
                {
                    clone.SaveThread(path);
                }
            }
            catch (Exception e)
            {
                ModInstance.Log($"Error saving HolopalmSave: {e.Message}");
                if (e.InnerException != null)
                {
                    ModInstance.Log($"Inner Exception: {e.InnerException.Message}");
                }
            }
        }

        public HolopalmSave DeepClone()
        {
            HolopalmSave save = (HolopalmSave)MemberwiseClone();

            save.settings = new Dictionary<string, bool>(settings);
            save.overlayFilters = new Dictionary<string, bool>(overlayFilters);

            save.giftedCards = [];
            foreach (var kvp in giftedCards)
            {
                save.giftedCards[kvp.Key] = [.. kvp.Value];
            }

            return save;
        }
    }
}
