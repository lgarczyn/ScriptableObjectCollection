using System;
using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections
{
    /// <summary>
    /// Watches for imported/moved/deleted SOC assets and ensures they are
    /// properly configured as Addressables with the correct labels.
    /// </summary>
    public class SOCAddressablePostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(
            string[] importedAssets, string[] deletedAssets,
            string[] movedAssets, string[] movedFromAssetPaths)
        {
            // Skip during play mode
            if (Application.isPlaying)
                return;

            bool registryDirty = false;

            foreach (string path in importedAssets)
            {
                registryDirty |= ProcessAsset(path);
            }

            foreach (string t in movedAssets)
            {
                registryDirty |= ProcessAsset(t);
            }

            if (registryDirty)
            {
                // Rebuild registry metadata after changes
                SOCAddressableUtility.SyncAllAddressables();
            }
        }

        private static bool ProcessAsset(string assetPath)
        {
            // Skip non-asset files
            if (!assetPath.EndsWith(".asset", StringComparison.Ordinal))
                return false;

            var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
            if (asset == null)
                return false;

            if (asset is ScriptableObjectCollection collection)
            {
                SOCAddressableUtility.EnsureCollectionAddressable(collection, assetPath);
                SOCAddressableUtility.RelabelCollectionItems(collection);
                return true;
            }

            if (asset is ISOCItem item)
            {
                // Find parent collection by folder
                var parentCollection = SOCAddressableUtility.FindCollectionForItemPath(assetPath);
                if (parentCollection != null)
                {
                    // Set the item's collection reference if not already set
                    if (item.Collection == null || item.Collection.GUID != parentCollection.GUID)
                    {
                        item.SetCollection(parentCollection);
                        EditorUtility.SetDirty(asset);
                    }

                    SOCAddressableUtility.EnsureItemAddressable(assetPath, parentCollection.AddressableLabel);
                    return true;
                }
            }

            return false;
        }
    }
}
