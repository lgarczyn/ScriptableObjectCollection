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
            foreach (CollectionMetadata t in entries)
            {
                var collection = CollectionsRegistry.Instance.GetOrLoadCollection(t.GUID);
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
