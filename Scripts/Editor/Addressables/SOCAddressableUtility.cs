using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections
{
    /// <summary>
    /// Manages Addressable groups, labels, and entries for SOC assets.
    /// </summary>
    public static class SOCAddressableUtility
    {
        private const string SOCGroupPrefix = "SOC_";
        public const string CollectionsLabel = "soc_collections";

        /// <summary>
        /// Full rescan: find all collections and items, ensure everything is Addressable.
        /// Used by pre-build and manual sync — not called during normal editing
        /// (the postprocessor handles incremental updates).
        /// </summary>
        [MenuItem("Assets/Create/ScriptableObject Collection/Sync All Addressables", false, 200)]
        public static void SyncAllAddressables()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("Addressable Asset Settings not found. Please create them via Window > Asset Management > Addressables > Groups.");
                return;
            }

            // Build collection info list
            string[] collectionGuids = AssetDatabase.FindAssets($"t:{nameof(ScriptableObjectCollection)}");
            var allCollectionGuids = new HashSet<string>(collectionGuids);
            var collections = new List<(ScriptableObjectCollection collection, string path, string guid)>();

            foreach (string assetGuid in collectionGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(assetGuid);
                var collection = AssetDatabase.LoadAssetAtPath<ScriptableObjectCollection>(path);
                if (collection == null)
                    continue;

                EnsureCollectionAddressable(collection, path);

                string folder = Path.GetDirectoryName(path)?.Replace('\\', '/');
                if (!string.IsNullOrEmpty(folder))
                    collections.Add((collection, path, assetGuid));
            }

            // Build item → labels map and track primary collection name per item
            var itemLabels = new Dictionary<string, HashSet<string>>();
            var itemPrimaryCollectionName = new Dictionary<string, string>();

            foreach (var (collection, path, guid) in collections)
            {
                Type itemType = collection.GetItemType();
                if (itemType == null)
                    continue;

                string[] itemGuids = AssetDatabase.FindAssets($"t:{itemType.Name}", new[] { Path.GetDirectoryName(path) });
                foreach (string itemGuid in itemGuids)
                {
                    string itemPath = AssetDatabase.GUIDToAssetPath(itemGuid);
                    Type itemAssetType = AssetDatabase.GetMainAssetTypeAtPath(itemPath);
                    if (itemAssetType == null || !typeof(ISOCItem).IsAssignableFrom(itemAssetType))
                        continue;

                    if (!itemLabels.TryGetValue(itemPath, out var labels))
                    {
                        labels = new HashSet<string>();
                        itemLabels[itemPath] = labels;
                        itemPrimaryCollectionName[itemPath] = collection.name;
                    }
                    labels.Add(guid);
                }
            }

            // Apply labels atomically per item, removing stale collection labels
            foreach (var (itemPath, labels) in itemLabels)
                ReconcileItemLabels(itemPath, labels, allCollectionGuids, itemPrimaryCollectionName[itemPath]);

            int count = collections.Count;

            // Label all IRegisteredSO assets
            string[] registeredGuids = AssetDatabase.FindAssets($"t:{nameof(ScriptableObject)}");
            int registeredCount = 0;
            foreach (string registeredGuid in registeredGuids)
            {
                string registeredPath = AssetDatabase.GUIDToAssetPath(registeredGuid);
                Type registeredType = AssetDatabase.GetMainAssetTypeAtPath(registeredPath);
                if (registeredType != null && typeof(IRegisteredSO).IsAssignableFrom(registeredType))
                {
                    EnsureRegisteredAddressable(registeredPath);
                    registeredCount++;
                }
            }

            // Bake Unity asset GUIDs into m_Guid fields
            BakeAllGuids();

            Debug.Log($"SOC Addressables synced: {count} collections, {registeredCount} registered SOs processed.");
        }

        /// <summary>
        /// Bake Unity asset GUIDs into the m_Guid serialized field of all SOC-managed assets.
        /// Called during SyncAllAddressables (pre-build and manual sync).
        /// </summary>
        public static void BakeAllGuids()
        {
            string[] allGuids = AssetDatabase.FindAssets("t:ScriptableObject");
            foreach (string guid in allGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Type assetType = AssetDatabase.GetMainAssetTypeAtPath(path);
                if (assetType == null)
                    continue;

                bool isManaged = typeof(ScriptableObjectCollection).IsAssignableFrom(assetType)
                                 || typeof(ISOCItem).IsAssignableFrom(assetType)
                                 || typeof(IRegisteredSO).IsAssignableFrom(assetType);

                if (!isManaged)
                    continue;

                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (asset == null)
                    continue;

                var so = new SerializedObject(asset);
                var guidProp = so.FindProperty("m_Guid");
                if (guidProp == null || guidProp.stringValue == guid)
                    continue;

                guidProp.stringValue = guid;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Ensure a collection asset is addressable in its own dedicated group.
        /// </summary>
        public static void EnsureCollectionAddressable(ScriptableObjectCollection collection, string assetPath)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) return;

            string assetGuid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(assetGuid)) return;

            string groupName = SOCGroupPrefix + collection.name;
            var group = GetOrCreateGroup(settings, groupName);
            var entry = settings.CreateOrMoveEntry(assetGuid, group, readOnly: false);
            entry.address = assetGuid;

            settings.AddLabel(CollectionsLabel);
            if (!entry.labels.Contains(CollectionsLabel))
                entry.labels.Add(CollectionsLabel);
        }

        /// <summary>
        /// Ensure an item asset is addressable in its parent collection's group.
        /// </summary>
        public static void EnsureItemAddressable(string assetPath, string collectionLabel, string collectionName)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) return;

            string assetGuid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(assetGuid)) return;

            string groupName = SOCGroupPrefix + collectionName;
            var group = GetOrCreateGroup(settings, groupName);
            var entry = settings.CreateOrMoveEntry(assetGuid, group, readOnly: false);
            entry.address = assetGuid;

            settings.AddLabel(collectionLabel);
            if (!entry.labels.Contains(collectionLabel))
                entry.labels.Add(collectionLabel);
        }

        /// <summary>
        /// Set the correct collection labels for an item, removing any stale collection labels.
        /// An item belongs to every collection whose folder is an ancestor of the item's path.
        /// Places the item in the first matching collection's group.
        /// </summary>
        public static void ReconcileItemLabels(string assetPath, HashSet<string> correctLabels, HashSet<string> allCollectionGuids, string primaryCollectionName)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) return;

            string assetGuid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(assetGuid)) return;

            string groupName = SOCGroupPrefix + primaryCollectionName;
            var group = GetOrCreateGroup(settings, groupName);
            var entry = settings.CreateOrMoveEntry(assetGuid, group, readOnly: false);
            entry.address = assetGuid;

            // Remove stale collection labels (labels that are collection GUIDs but shouldn't be)
            var currentLabels = entry.labels.ToList();
            foreach (string label in currentLabels)
            {
                if (allCollectionGuids.Contains(label) && !correctLabels.Contains(label))
                    entry.labels.Remove(label);
            }

            // Add correct collection labels
            foreach (string label in correctLabels)
            {
                settings.AddLabel(label);
                if (!entry.labels.Contains(label))
                    entry.labels.Add(label);
            }
        }

        /// <summary>
        /// Find the parent collection for an item by walking up the folder tree.
        /// Loads .asset files and checks if they're collections — used as fallback
        /// when the postprocessor cache isn't available.
        /// </summary>
        public static ScriptableObjectCollection FindCollectionForItemPath(string itemPath)
        {
            string folder = Path.GetDirectoryName(itemPath)?.Replace('\\', '/');

            while (!string.IsNullOrEmpty(folder) && folder.StartsWith("Assets", StringComparison.Ordinal))
            {
                string[] assetGuids = AssetDatabase.FindAssets("", new[] { folder });
                foreach (string guid in assetGuids)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);

                    if (!assetPath.EndsWith(".asset", StringComparison.Ordinal))
                        continue;

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

        /// <summary>
        /// Ensure a registered SO is addressable in its own dedicated group.
        /// </summary>
        public static void EnsureRegisteredAddressable(string assetPath)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) return;

            string assetGuid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(assetGuid)) return;

            string assetName = Path.GetFileNameWithoutExtension(assetPath);
            string groupName = SOCGroupPrefix + assetName;
            var group = GetOrCreateGroup(settings, groupName);
            var entry = settings.CreateOrMoveEntry(assetGuid, group, readOnly: false);
            entry.address = assetGuid;

            settings.AddLabel(ScriptableObjectRegistry.RegisteredLabel);
            if (!entry.labels.Contains(ScriptableObjectRegistry.RegisteredLabel))
                entry.labels.Add(ScriptableObjectRegistry.RegisteredLabel);
        }

        public static AddressableAssetGroup GetOrCreateGroup(AddressableAssetSettings settings, string groupName)
        {
            var group = settings.FindGroup(groupName);
            if (group == null)
            {
                group = settings.CreateGroup(groupName, false, false, true,
                    null, typeof(BundledAssetGroupSchema));
            }
            return group;
        }
    }
}
