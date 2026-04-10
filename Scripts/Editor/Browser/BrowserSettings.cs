using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections.Browser
{
    [Serializable]
    public class BrowserSettings
    {
        private const string PATH = "UserSettings/ScriptableObjectCollectionBrowser.json";

        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            return new SettingsProvider("Project/Scriptable Object Collection/Browser", SettingsScope.Project)
            {
                label = "Browser",
                guiHandler = Instance.OnGUI,
                keywords = new string[] { "SOC", "Scriptable Objects", "Scriptable Objects Collection", "Browser" }
            };
        }

        private static BrowserSettings instance;

        public static BrowserSettings Instance
        {
            get
            {
                if (instance != null)
                    return instance;

                if (File.Exists(PATH))
                {
                    string json = File.ReadAllText(PATH);
                    instance = JsonUtility.FromJson<BrowserSettings>(json);
                }
                else
                {
                    instance = new BrowserSettings();
                }

                return instance;
            }
        }

        public event Action SettingsChanged;

        private void OnGUI(string searchContext)
        {
        }

        private void Save()
        {
            string json = EditorJsonUtility.ToJson(this, prettyPrint: true);
            File.WriteAllText(PATH, json);
            SettingsChanged?.Invoke();
        }
    }
}
