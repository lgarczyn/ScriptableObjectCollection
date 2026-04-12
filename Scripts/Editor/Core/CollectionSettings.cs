using System;
using UnityEditor;

namespace BrunoMikoski.ScriptableObjectCollections
{
    [Serializable]
    public class CollectionSettings
    {
        public string Namespace;
        public string StaticFilename;
        public bool UseBaseClassForItems;

        private AssetImporter importer;

        public CollectionSettings()
        {
        }

        public CollectionSettings(ScriptableObjectCollection targetCollection)
        {
            Type itemType = targetCollection != null ? targetCollection.GetItemType() : null;
            string targetNamespace = itemType?.Namespace;
            if (string.IsNullOrEmpty(targetNamespace) && !string.IsNullOrEmpty(SOCSettings.Instance.NamespacePrefix))
                targetNamespace = $"{SOCSettings.Instance.NamespacePrefix}";

            Namespace = targetNamespace;
            StaticFilename = $"{targetCollection.GetType().Name}Static".FirstToUpper();
            UseBaseClassForItems = false;
            Save();
        }

        public void SetImporter(AssetImporter targetImporter)
        {
            importer = targetImporter;
        }

        public void Save()
        {
            if (importer == null)
                return;

            importer.userData = EditorJsonUtility.ToJson(this);
            AssetDatabase.WriteImportSettingsIfDirty(importer.assetPath);
        }

        public void SetStaticFilename(string targetNewName)
        {
            if (string.Equals(StaticFilename, targetNewName, StringComparison.Ordinal))
                return;

            StaticFilename = targetNewName;
            Save();
        }

        public void SetNamespace(string targetNamespace)
        {
            if (string.Equals(Namespace, targetNamespace, StringComparison.Ordinal))
                return;

            Namespace = targetNamespace;
            Save();
        }

        public void SetUseBaseClassForItems(bool useBaseClass)
        {
            if (UseBaseClassForItems == useBaseClass)
                return;

            UseBaseClassForItems = useBaseClass;
            Save();
        }
    }
}
