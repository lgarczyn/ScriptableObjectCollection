using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections
{
    /// <summary>
    /// Clears collection caches on play mode transitions.
    /// </summary>
    [InitializeOnLoad]
    static class SOCPlayModeHandler
    {
        static SOCPlayModeHandler()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            ScriptableObjectCollection.ClearCache();
        }
    }

    /// <summary>
    /// Watches for imported/moved/deleted SOC assets.
    /// Bakes GUIDs on changed assets, then runs a full Addressable sync
    /// whenever any SOC-managed asset changes.
    /// </summary>
    public class SOCAddressablePostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(
            string[] importedAssets, string[] deletedAssets,
            string[] movedAssets, string[] movedFromAssetPaths)
        {
            if (Application.isPlaying)
                return;

            if (AddressableAssetSettingsDefaultObject.Settings == null)
                return;


            BakeGuids(importedAssets);

            var changedPaths = importedAssets
                .Concat(movedAssets)
                .Where(p => p.EndsWith(".asset", StringComparison.Ordinal))
                .ToArray();


            var deletedPaths = movedAssets
                .Concat(deletedAssets)
                .Where(p => p.EndsWith(".asset", StringComparison.Ordinal))
                .ToArray();

            if (changedPaths.Length == 0 && deletedPaths.Length == 0)
                return;

            // Check if any collection was created/moved/deleted → full sync
            bool needsFullSync = false;

            foreach (var deleted in deletedPaths)
            {
                string guid = AssetDatabase.AssetPathToGUID(deleted);
                if (string.IsNullOrEmpty(guid)) continue;
                var entry = AddressableAssetSettingsDefaultObject.Settings.FindAssetEntry(guid);
                if (entry != null && entry.labels.Contains(SOCAddressableUtility.CollectionsLabel))
                {
                    needsFullSync = true;
                    break;
                }
            }

            foreach (string path in changedPaths)
            {
                if (typeof(ScriptableObjectCollection).IsAssignableFrom(AssetDatabase.GetMainAssetTypeAtPath(path)))
                {
                    needsFullSync = true;
                    break;
                }
            }

            if (needsFullSync)
            {
                SOCAddressableUtility.SyncAllAddressables();
                return;
            }

            // Incremental: only items changed — bake GUIDs and ensure each item is addressable
            var itemPaths = new List<string>();
            foreach (string path in changedPaths)
            {
                Type assetType = AssetDatabase.GetMainAssetTypeAtPath(path);
                if (assetType != null && typeof(ISOCItem).IsAssignableFrom(assetType))
                    itemPaths.Add(path);
            }

            if (itemPaths.Count == 0)
                return;

            foreach (string itemPath in itemPaths)
            {
                var parentCollection = FindCollectionForItemPath(itemPath);
                if (parentCollection == null) continue;

                string collectionGuid = AssetDatabase.AssetPathToGUID(
                    AssetDatabase.GetAssetPath(parentCollection));
                if (!string.IsNullOrEmpty(collectionGuid))
                    SOCAddressableUtility.EnsureItemAddressable(itemPath, collectionGuid);
            }
        }

        private static void BakeGuids(string[] importedAssets)
        {
            foreach (string path in importedAssets)
            {
                if (!path.EndsWith(".asset", StringComparison.Ordinal))
                {
                    return;
                }

                Type assetType = AssetDatabase.GetMainAssetTypeAtPath(path);
                if (assetType == null)
                    continue;

                bool isManaged = typeof(ScriptableObjectCollection).IsAssignableFrom(assetType)
                                 || typeof(ISOCItem).IsAssignableFrom(assetType)
                                 || typeof(IRegisteredSO).IsAssignableFrom(assetType);
                if (!isManaged)
                    continue;

                string assetGuid = AssetDatabase.AssetPathToGUID(path);
                if (string.IsNullOrEmpty(assetGuid))
                    continue;

                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (asset == null)
                    continue;

                var so = new SerializedObject(asset);
                var guidProp = so.FindProperty("m_Guid");
                if (guidProp == null || guidProp.stringValue == assetGuid)
                    continue;

                guidProp.stringValue = assetGuid;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        /// <summary>
        /// Find the parent collection for an item by walking up the folder tree.
        /// </summary>
        public static ScriptableObjectCollection FindCollectionForItemPath(string itemPath)
        {
            if (string.IsNullOrEmpty(itemPath))
                return null;

            return SOCAddressableUtility.FindCollectionForItemPath(itemPath);
        }
    }
}
