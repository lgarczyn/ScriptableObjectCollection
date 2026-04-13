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
        private const string CollectionsGroupName = "SOC_Collections";
        private const string RegisteredGroupName = "SOC_Registered";
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

            AssetDatabase.StartAssetEditing();
            try
            {
                int removed = RemoveStaleEntries();
                int collections = SyncCollections();
                int registered = SyncRegisteredObjects();
                BakeAllGuids();
                Debug.Log($"SOC Addressables synced: {collections} collections, {registered} registered SOs. {removed} stale entries removed.");
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
        }

        /// <summary>
        /// Remove Addressable entries whose assets no longer exist on disk.
        /// </summary>
        private static int RemoveStaleEntries()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) return 0;

            int removed = 0;
            foreach (var group in settings.groups)
            {
                if (group == null) continue;

                var entries = new List<AddressableAssetEntry>(group.entries);
                foreach (var entry in entries)
                {
                    string path = AssetDatabase.GUIDToAssetPath(entry.guid);
                    if (string.IsNullOrEmpty(path) || AssetDatabase.GetMainAssetTypeAtPath(path) == null)
                    {
                        group.RemoveAssetEntry(entry);
                        removed++;
                    }
                }
            }

            return removed;
        }

        /// <summary>
        /// Ensure all collections and their items are addressable with correct labels.
        /// </summary>
        private static int SyncCollections()
        {
            string[] collectionGuids = AssetDatabase.FindAssets($"t:{nameof(ScriptableObjectCollection)}");
            var allCollectionGuids = new HashSet<string>(collectionGuids);
            var collections = new List<(ScriptableObjectCollection collection, string path, string guid)>();

            foreach (string assetGuid in collectionGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(assetGuid);
                var collection = AssetDatabase.LoadAssetAtPath<ScriptableObjectCollection>(path);
                if (collection == null)
                    continue;

                EnsureCollectionAddressable(path);

                string folder = Path.GetDirectoryName(path)?.Replace('\\', '/');
                if (!string.IsNullOrEmpty(folder))
                    collections.Add((collection, path, assetGuid));
            }

            var itemLabels = new Dictionary<string, HashSet<string>>();

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
                    }
                    labels.Add(guid);
                }
            }

            foreach (var (itemPath, labels) in itemLabels)
                ReconcileItemLabels(itemPath, labels, allCollectionGuids);

            return collections.Count;
        }

        /// <summary>
        /// Ensure all IRegisteredSO assets are addressable in the registered group.
        /// </summary>
        private static int SyncRegisteredObjects()
        {
            string[] registeredGuids = AssetDatabase.FindAssets($"t:{nameof(ScriptableObject)}");
            int count = 0;

            foreach (string registeredGuid in registeredGuids)
            {
                string registeredPath = AssetDatabase.GUIDToAssetPath(registeredGuid);
                Type registeredType = AssetDatabase.GetMainAssetTypeAtPath(registeredPath);
                if (registeredType != null && typeof(IRegisteredSO).IsAssignableFrom(registeredType))
                {
                    EnsureRegisteredAddressable(registeredPath);
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Bake Unity asset GUIDs into the m_Guid serialized field of all SOC-managed assets.
        /// Called during SyncAllAddressables (pre-build and manual sync).
        /// </summary>
        private static void BakeAllGuids()
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
        /// Get or create an Addressable entry in the given group. Skips CreateOrMoveEntry if already correct.
        /// </summary>
        private static AddressableAssetEntry GetOrCreateEntry(AddressableAssetSettings settings, string assetGuid, AddressableAssetGroup group, bool postEvent = true)
        {
            var existing = settings.FindAssetEntry(assetGuid);
            if (existing != null && existing.parentGroup == group)
                return existing;

            var entry = settings.CreateOrMoveEntry(assetGuid, group, readOnly: false, postEvent: postEvent);
            entry.address = assetGuid;
            return entry;
        }

        private static void EnsureLabel(AddressableAssetSettings settings, AddressableAssetEntry entry, string label, bool postEvent = true)
        {
            settings.AddLabel(label, postEvent);
            if (!entry.labels.Contains(label))
                entry.labels.Add(label);
        }

        /// <summary>
        /// Ensure a collection asset is addressable in the shared collections group.
        /// </summary>
        public static void EnsureCollectionAddressable(string assetPath)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) return;

            string assetGuid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(assetGuid)) return;

            var group = GetOrCreateCollectionsGroup(settings);
            var entry = GetOrCreateEntry(settings, assetGuid, group);
            EnsureLabel(settings, entry, CollectionsLabel);
        }

        /// <summary>
        /// Ensure an item asset is addressable in the shared collections group.
        /// </summary>
        public static void EnsureItemAddressable(string assetPath, string collectionLabel)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) return;

            string assetGuid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(assetGuid)) return;

            var group = GetOrCreateCollectionsGroup(settings);
            var entry = GetOrCreateEntry(settings, assetGuid, group);
            EnsureLabel(settings, entry, collectionLabel);
        }

        /// <summary>
        /// Set the correct collection labels for an item, removing any stale collection labels.
        /// Called from bulk sync — uses postEvent:false for performance.
        /// </summary>
        public static void ReconcileItemLabels(string assetPath, HashSet<string> correctLabels, HashSet<string> allCollectionGuids)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) return;

            string assetGuid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(assetGuid)) return;

            var group = GetOrCreateCollectionsGroup(settings);
            var entry = GetOrCreateEntry(settings, assetGuid, group, postEvent: false);

            // Remove stale collection labels
            var currentLabels = entry.labels.ToList();
            foreach (string label in currentLabels)
            {
                if (allCollectionGuids.Contains(label) && !correctLabels.Contains(label))
                    entry.labels.Remove(label);
            }

            // Add correct collection labels
            foreach (string label in correctLabels)
                EnsureLabel(settings, entry, label, postEvent: false);
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
        /// Ensure a registered SO is addressable in the shared registered group.
        /// </summary>
        public static void EnsureRegisteredAddressable(string assetPath)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) return;

            string assetGuid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(assetGuid)) return;

            var group = GetOrCreateRegisteredGroup(settings);
            var entry = GetOrCreateEntry(settings, assetGuid, group);
            EnsureLabel(settings, entry, ScriptableObjectRegistry.RegisteredLabel);
        }

        private static AddressableAssetGroup GetOrCreateCollectionsGroup(AddressableAssetSettings settings)
        {
            return GetOrCreatePackSeparatelyGroup(settings, CollectionsGroupName);
        }

        private static AddressableAssetGroup GetOrCreateRegisteredGroup(AddressableAssetSettings settings)
        {
            return GetOrCreatePackSeparatelyGroup(settings, RegisteredGroupName);
        }

        private static AddressableAssetGroup GetOrCreatePackSeparatelyGroup(AddressableAssetSettings settings, string groupName)
        {
            var group = settings.FindGroup(groupName);
            if (group == null)
            {
                group = settings.CreateGroup(groupName, false, false, true,
                    null, typeof(BundledAssetGroupSchema));

                var schema = group.GetSchema<BundledAssetGroupSchema>();
                if (schema != null)
                    schema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackSeparately;
            }
            return group;
        }
    }
}
