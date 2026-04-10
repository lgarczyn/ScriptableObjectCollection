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

            // Ensure registry is addressable
            var registry = CollectionsRegistry.Instance;
            if (registry != null)
                EnsureRegistryAddressable(registry);

            // Find all collections
            string[] collectionGuids = AssetDatabase.FindAssets($"t:{nameof(ScriptableObjectCollection)}");
            var metadataEntries = new List<CollectionMetadata>();

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

                // Build metadata
                metadataEntries.Add(new CollectionMetadata(
                    collection.GUID,
                    collection.GetType().AssemblyQualifiedName,
                    collection.GetItemType()?.AssemblyQualifiedName ?? "",
                    GetCollectionAddress(collection),
                    collection.AddressableLabel
                ));
            }

            // Update registry
            if (registry != null)
            {
                registry.SetEntries(metadataEntries);
                EditorUtility.SetDirty(registry);
                AssetDatabase.SaveAssets();
            }

            Debug.Log($"SOC Addressables synced: {metadataEntries.Count} collections processed.");
        }

        /// <summary>
        /// Ensure the registry asset is addressable with the known address.
        /// </summary>
        public static void EnsureRegistryAddressable(CollectionsRegistry registry)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) return;

            string assetPath = AssetDatabase.GetAssetPath(registry);
            string assetGuid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(assetGuid)) return;

            var group = GetOrCreateSOCGroup(settings);
            var entry = settings.CreateOrMoveEntry(assetGuid, group, readOnly: false);
            entry.address = CollectionsRegistry.RegistryAddress;
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
            var items = SOCEditorUtility.FindItemsInFolder(collection, folder);
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
            string folder = Path.GetDirectoryName(itemPath);

            while (!string.IsNullOrEmpty(folder) && folder.StartsWith("Assets", StringComparison.Ordinal))
            {
                string[] collectionGuids = AssetDatabase.FindAssets(
                    $"t:{nameof(ScriptableObjectCollection)}", new[] { folder });

                foreach (string guid in collectionGuids)
                {
                    string collectionPath = AssetDatabase.GUIDToAssetPath(guid);
                    // Collection must be in this folder (not a subfolder's subfolder)
                    if (Path.GetDirectoryName(collectionPath) == folder)
                    {
                        return AssetDatabase.LoadAssetAtPath<ScriptableObjectCollection>(collectionPath);
                    }
                }

                folder = Path.GetDirectoryName(folder);
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
