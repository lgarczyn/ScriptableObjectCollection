using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections
{
    [CustomEditor(typeof(CollectionsRegistry), true)]
    public sealed class CollectionRegistryCustomEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button("Sync Addressables"))
                SOCAddressableUtility.SyncAllAddressables();

            if (GUILayout.Button("Generate All Existent Static Access Files"))
                GenerateAllExistentStaticAccessFiles();
        }

        private void GenerateAllExistentStaticAccessFiles()
        {
            var entries = CollectionsRegistry.Instance.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                var collection = CollectionsRegistry.Instance.GetOrLoadCollection(entries[i].GUID);
                if (collection == null)
                    continue;

                if (!CodeGenerationUtility.DoesStaticFileForCollectionExist(collection))
                    continue;

                SOCEditorUtility.RefreshEditorItems(collection);
                CodeGenerationUtility.GenerateStaticCollectionScript(collection);
            }
        }
    }
}
