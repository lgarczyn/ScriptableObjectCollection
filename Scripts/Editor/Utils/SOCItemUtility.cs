using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public static class SOCItemUtility
    {
        public static void MoveItem(ScriptableObject item, ScriptableObjectCollection targetCollection,
            Action onCompleteCallback = null)
        {
            if (item is ISOCItem iItem)
                MoveItem(iItem, targetCollection, onCompleteCallback);
        }

        public static void MoveItem(ISOCItem item, ScriptableObjectCollection targetCollection, Action onCompleteCallback = null)
        {
            Undo.RecordObject(item as ScriptableObject, "Move Item");

            string itemPath = AssetDatabase.GetAssetPath(item as ScriptableObject);
            string targetCollectionPath = AssetDatabase.GetAssetPath(targetCollection);

            if (!string.IsNullOrEmpty(itemPath) && !string.IsNullOrEmpty(targetCollectionPath))
            {
                string directory = Path.GetDirectoryName(targetCollectionPath);
                string itemsFolderPath = Path.Combine(directory, "Items");
                AssetDatabaseUtils.CreatePathIfDoesntExist(itemsFolderPath);

                string fileName = Path.GetFileName(itemPath);
                string newPathCandidate = Path.Combine(itemsFolderPath, fileName);

                string normalizedItem = Path.GetFullPath(itemPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).ToUpperInvariant();
                string normalizedCandidate = Path.GetFullPath(newPathCandidate).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).ToUpperInvariant();

                if (normalizedItem != normalizedCandidate)
                {
                    string newPath = AssetDatabase.GenerateUniqueAssetPath(newPathCandidate);
                    AssetDatabase.MoveAsset(itemPath, newPath);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            onCompleteCallback?.Invoke();
        }
    }
}
