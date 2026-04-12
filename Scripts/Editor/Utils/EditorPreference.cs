using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BrunoMikoski.ScriptableObjectCollections
{
    /// <summary>
    /// Utility class to simplify the workflow of using EditorPrefs-serialized fields.
    /// </summary>
    public abstract class EditorPreference
    {
        private string path;
        public string Path
        {
            get
            {
                if (IsProjectSpecific)
                    return ProjectPrefix + path;
                return path;
            }
        }

        public bool IsProjectSpecific { get; set; }

        private static string cachedProjectPrefix;
        private static string ProjectPrefix
        {
            get
            {
                if (cachedProjectPrefix == null)
                {
                    string assetsFolder = Application.dataPath;
                    string projectsFolder = assetsFolder.Substring(0, assetsFolder.Length - "/Assets".Length);
                    cachedProjectPrefix = projectsFolder + "/";
                }
                return cachedProjectPrefix;
            }
        }

        private GUIContent label;
        public GUIContent Label => label;

        public abstract object ObjectValue { get; }

        protected EditorPreference(string path, bool isProjectSpecific = false)
        {
            this.path = path;
            IsProjectSpecific = isProjectSpecific;
            string name = System.IO.Path.GetFileName(path).ToHumanReadable();
            label = new GUIContent(name);
        }

        public abstract void DrawGUILayout(GUIContent label, params GUILayoutOption[] options);

        public void DrawGUILayout(params GUILayoutOption[] layoutOptions)
        {
            DrawGUILayout(Label, layoutOptions);
        }

        public void DrawGUILayout(string label, params GUILayoutOption[] options)
        {
            DrawGUILayout(new GUIContent(label), options);
        }
    }

    public abstract class EditorPreferenceGeneric<ValueType> : EditorPreference
    {
        public virtual ValueType Value
        {
            get => ValueRaw;
            set => ValueRaw = value;
        }

        protected ValueType ValueRaw
        {
            get => !EditorPrefs.HasKey(Path) ? defaultValue : UnityPrefsValue;
            set => UnityPrefsValue = value;
        }

        public override object ObjectValue => Value;

        protected abstract ValueType UnityPrefsValue { get; set; }

        private ValueType defaultValue;

        protected EditorPreferenceGeneric(
            string path, ValueType defaultValue = default, bool isProjectSpecific = false)
            : base(path, isProjectSpecific)
        {
            this.defaultValue = defaultValue;
        }
    }

    public class EditorPreferenceBool : EditorPreferenceGeneric<bool>
    {
        protected override bool UnityPrefsValue
        {
            get => EditorPrefs.GetBool(Path);
            set => EditorPrefs.SetBool(Path, value);
        }

        public EditorPreferenceBool(string path, bool defaultValue = default, bool isProjectSpecific = false)
            : base(path, defaultValue, isProjectSpecific) { }

        public override void DrawGUILayout(GUIContent label, params GUILayoutOption[] options)
        {
            Value = EditorGUILayout.Toggle(label, Value, options);
        }

        public void DrawGUILayoutLeft(params GUILayoutOption[] options)
        {
            DrawGUILayoutLeft(Label, options);
        }

        public void DrawGUILayoutLeft(GUIContent label, params GUILayoutOption[] options)
        {
            Value = EditorGUILayout.ToggleLeft(label, Value, options);
        }

        public void DrawGUILayoutLeft(string label, params GUILayoutOption[] options)
        {
            DrawGUILayoutLeft(new GUIContent(label), options);
        }
    }

    public class EditorPreferenceString : EditorPreferenceGeneric<string>
    {
        protected override string UnityPrefsValue
        {
            get => EditorPrefs.GetString(Path);
            set => EditorPrefs.SetString(Path, value);
        }

        public EditorPreferenceString(string path, string defaultValue = default, bool isProjectSpecific = false)
            : base(path, defaultValue, isProjectSpecific) { }

        public override void DrawGUILayout(GUIContent label, params GUILayoutOption[] options)
        {
            Value = EditorGUILayout.TextField(label, Value, options);
        }
    }

    public class EditorPreferenceInt : EditorPreferenceGeneric<int>
    {
        protected override int UnityPrefsValue
        {
            get => EditorPrefs.GetInt(Path);
            set => EditorPrefs.SetInt(Path, value);
        }

        public EditorPreferenceInt(string path, int defaultValue = default, bool isProjectSpecific = false)
            : base(path, defaultValue, isProjectSpecific) { }

        public override void DrawGUILayout(GUIContent label, params GUILayoutOption[] options)
        {
            Value = EditorGUILayout.IntField(label, Value, options);
        }
    }

    public class EditorPreferenceFloat : EditorPreferenceGeneric<float>
    {
        protected override float UnityPrefsValue
        {
            get => EditorPrefs.GetFloat(Path);
            set => EditorPrefs.SetFloat(Path, value);
        }

        public EditorPreferenceFloat(string path, float defaultValue = default, bool isProjectSpecific = false)
            : base(path, defaultValue, isProjectSpecific) { }

        public override void DrawGUILayout(GUIContent label, params GUILayoutOption[] options)
        {
            Value = EditorGUILayout.FloatField(label, Value, options);
        }
    }

    public class EditorPreferenceObject<T> : EditorPreferenceGeneric<T>
        where T : Object
    {
        private T cachedAsset;

        protected override T UnityPrefsValue
        {
            get
            {
                if (cachedAsset == null)
                {
                    string assetPath = EditorPrefs.GetString(Path);
                    if (string.IsNullOrEmpty(assetPath))
                        return null;
                    cachedAsset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
                }
                return cachedAsset;
            }
            set
            {
                T previousValue = UnityPrefsValue;
                if (previousValue != value)
                {
                    string path = value == null ? null : AssetDatabase.GetAssetPath(value);
                    EditorPrefs.SetString(Path, path);
                    cachedAsset = null;
                }
            }
        }

        public EditorPreferenceObject(string path, T defaultValue = default, bool isProjectSpecific = false)
            : base(path, defaultValue, isProjectSpecific) { }

        public override void DrawGUILayout(GUIContent label, params GUILayoutOption[] options)
        {
            Value = (T)EditorGUILayout.ObjectField(label, Value, typeof(T), false, options);
        }
    }
}
