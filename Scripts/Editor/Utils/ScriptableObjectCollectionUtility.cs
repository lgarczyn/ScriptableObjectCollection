using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public static class ScriptableObjectCollectionUtility
    {
        private const string COLLECTION_CUSTOM_EDITOR_GO_TO_ITEM_INDEX_KEY = "COLLECTION_CUSTOM_EDITOR_GO_TO_ITEM_INDEX";

        public static T CreateScriptableObjectOfType<T>(DefaultAsset parentFolder, string name) where T : ScriptableObject
        {
            return CreateScriptableObjectOfType(typeof(T), AssetDatabase.GetAssetPath(parentFolder), name) as T;
        }
        
        public static T CreateScriptableObjectOfType<T>(string path, string name) where T : ScriptableObject
        {
            return CreateScriptableObjectOfType(typeof(T), path, name) as T;
        }
        
        public static ScriptableObject CreateScriptableObjectOfType(Type targetType, string path, string name)
        {
            ScriptableObject targetCollection = ScriptableObject.CreateInstance(targetType);
            targetCollection.name = name;
            
            AssetDatabaseUtils.CreatePathIfDoesntExist(Path.Combine(path, "Items"));

            string collectionAssetPath = Path.Combine(path, $"{name}.asset");
            AssetDatabase.CreateAsset(targetCollection, collectionAssetPath);
            return targetCollection;
        }

        public static void GoToItem(ISOCItem socItem)
        {
            string itemPath = AssetDatabase.GetAssetPath(socItem as ScriptableObject);
            ScriptableObjectCollection collection = SOCAddressableUtility.FindCollectionForItemPath(itemPath);
            if (collection == null)
                return;

            var items = collection.Items;
            int index = -1;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] == (ScriptableObject)socItem)
                {
                    index = i;
                    break;
                }
            }
            SessionState.SetInt(COLLECTION_CUSTOM_EDITOR_GO_TO_ITEM_INDEX_KEY, index);
            Selection.activeObject = collection;
        }

        public static bool IsTryingToGoToItem(out int targetIndex)
        {
            targetIndex = SessionState.GetInt(COLLECTION_CUSTOM_EDITOR_GO_TO_ITEM_INDEX_KEY, -1);
            return targetIndex != -1;
        }

        public static void ClearGoToItem()
        {
            SessionState.EraseInt(COLLECTION_CUSTOM_EDITOR_GO_TO_ITEM_INDEX_KEY);
        }
    }
}
