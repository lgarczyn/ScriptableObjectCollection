using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections
{
    /// <summary>
    /// Manages Addressable groups, labels, and entries for SOC assets.
    /// Inspired by SmartAddresser - automatically maintains Addressable state.
    /// </summary>
    public static class SOCAddressableUtility
    {
        private const string SOCGroupName = "ScriptableObjectCollections";
        public const string CollectionsLabel = "soc_collections";

        /// <summary>
        /// Full rescan: find all collections, label all items, update registry metadata.
        /// </summary>
        public static void SyncAllAddressables()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("Addressable Asset Settings not found. Please create them via Window > Asset Management > Addressables > Groups.");
                return;
            }

            // Find all collections
            string[] collectionGuids = AssetDatabase.FindAssets($"t:{nameof(ScriptableObjectCollection)}");
            int count = 0;

            foreach (string assetGuid in collectionGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(assetGuid);
                var collection = AssetDatabase.LoadAssetAtPath<ScriptableObjectCollection>(path);
                if (collection == null)
                    continue;

                // Ensure collection itself is addressable
                EnsureCollectionAddressable(collection, path);

                // Label all items in the collection's folder
                RelabelCollectionItems(collection);
                count++;
            }

            Debug.Log($"SOC Addressables synced: {count} collections processed.");
        }

        /// <summary>
        /// Ensure a collection asset is addressable with a stable address.
        /// </summary>
        public static void EnsureCollectionAddressable(ScriptableObjectCollection collection, string assetPath)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) return;

            string assetGuid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(assetGuid)) return;

            var group = GetOrCreateSOCGroup(settings);
            var entry = settings.CreateOrMoveEntry(assetGuid, group, readOnly: false);
            entry.address = GetCollectionAddress(collection);

            settings.AddLabel(CollectionsLabel);
            if (!entry.labels.Contains(CollectionsLabel))
                entry.labels.Add(CollectionsLabel);
        }

        /// <summary>
        /// Ensure an item asset is addressable with the correct collection label.
        /// </summary>
        public static void EnsureItemAddressable(string assetPath, string collectionLabel)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) return;

            string assetGuid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(assetGuid)) return;

            var group = GetOrCreateSOCGroup(settings);
            var entry = settings.CreateOrMoveEntry(assetGuid, group, readOnly: false);
            entry.address = assetGuid; // Use Unity GUID as address for stability

            // Ensure label exists and is applied
            settings.AddLabel(collectionLabel);
            if (!entry.labels.Contains(collectionLabel))
                entry.labels.Add(collectionLabel);
        }

        /// <summary>
        /// Re-label all items in a collection's folder with the collection's Addressable label.
        /// </summary>
        public static void RelabelCollectionItems(ScriptableObjectCollection collection)
        {
            string collectionPath = AssetDatabase.GetAssetPath(collection);
            string folder = Path.GetDirectoryName(collectionPath);

            Type itemType = collection.GetItemType();
            if (itemType == null) return;

            string[] guids = AssetDatabase.FindAssets($"t:{itemType.Name}", new[] { folder });
            var items = new List<ScriptableObject>();
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var item = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (item is ISOCItem)
                    items.Add(item);
            }

            string label = collection.AddressableLabel;

            foreach (var item in items)
            {
                string itemPath = AssetDatabase.GetAssetPath(item);
                EnsureItemAddressable(itemPath, label);
            }
        }

        /// <summary>
        /// Find the parent collection for an item by walking up the folder tree.
        /// </summary>
        public static ScriptableObjectCollection FindCollectionForItemPath(string itemPath)
        {
            string folder = Path.GetDirectoryName(itemPath)?.Replace('\\', '/');

            while (!string.IsNullOrEmpty(folder) && folder.StartsWith("Assets", StringComparison.Ordinal))
            {
                // Search all .asset files in this folder and try to load as collection.
                // We avoid t:ScriptableObjectCollection filter because FindAssets can fail
                // to index assets whose script name doesn't match the type name.
                string[] assetGuids = AssetDatabase.FindAssets("", new[] { folder });
                foreach (string guid in assetGuids)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);

                    if (!assetPath.EndsWith(".asset", StringComparison.Ordinal))
                        continue;

                    // Must be directly in this folder, not a subfolder
                    string assetFolder = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
                    if (assetFolder != folder)
                        continue;

                    var asset = AssetDatabase.LoadAssetAtPath<ScriptableObjectCollection>(assetPath);
                    if (asset != null)
                        return asset;
                }

                folder = Path.GetDirectoryName(folder)?.Replace('\\', '/');
            }

            return null;
        }

        public static string GetCollectionAddress(ScriptableObjectCollection collection)
        {
            return $"soc_collection_{collection.GUID.ToBase64String()}";
        }

        public static AddressableAssetGroup GetOrCreateSOCGroup(AddressableAssetSettings settings)
        {
            var group = settings.FindGroup(SOCGroupName);
            if (group == null)
            {
                group = settings.CreateGroup(SOCGroupName, false, false, true,
                    null, typeof(BundledAssetGroupSchema));
            }
            return group;
        }
    }
}
