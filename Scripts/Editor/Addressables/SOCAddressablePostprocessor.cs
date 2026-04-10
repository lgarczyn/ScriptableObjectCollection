using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections
{
    /// <summary>
    /// Watches for imported/moved/deleted SOC assets and ensures they are
    /// properly configured as Addressables with the correct labels.
    /// Caches known collections to avoid full project rescans.
    /// </summary>
    public class SOCAddressablePostprocessor : AssetPostprocessor
    {
        private static readonly List<CollectionInfo> CachedCollections = new();
        private static bool cacheInitialized;

        private struct CollectionInfo
        {
            public ScriptableObjectCollection Collection;
            public string Path;     // e.g. "Assets/Data/Enemies/EnemyCollection.asset"
            public string Folder;   // e.g. "Assets/Data/Enemies/"
        }

        /// <summary>
        /// Force the collection cache to rebuild. Called automatically by the postprocessor,
        /// but exposed for tests and manual refresh.
        /// </summary>
        public static void InvalidateCache()
        {
            cacheInitialized = false;
            CachedCollections.Clear();
        }

        private static void RebuildCache(HashSet<string> changedPaths)
        {
            // If cache exists and no changed path is a collection, skip rebuild
            if (cacheInitialized)
            {
                bool collectionChanged = changedPaths.Any(p =>
                    p.EndsWith(".asset", StringComparison.Ordinal) &&
                    AssetDatabase.GetMainAssetTypeAtPath(p) is Type t &&
                    typeof(ScriptableObjectCollection).IsAssignableFrom(t));

                if (!collectionChanged)
                    return;
            }

            cacheInitialized = true;
            CachedCollections.Clear();

            // FindAssets with t: filter is cheap — it doesn't load assets, just queries the DB
            string[] guids = AssetDatabase.FindAssets($"t:{nameof(ScriptableObjectCollection)}");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string folder = Path.GetDirectoryName(path)?.Replace('\\', '/');
                if (string.IsNullOrEmpty(folder))
                    continue;

                folder += "/";

                // Load the collection — needed for GUID and label
                var collection = AssetDatabase.LoadAssetAtPath<ScriptableObjectCollection>(path);
                if (collection == null)
                    continue;

                CachedCollections.Add(new CollectionInfo
                {
                    Collection = collection,
                    Path = path,
                    Folder = folder,
                });
            }
        }

        private static void OnPostprocessAllAssets(
            string[] importedAssets, string[] deletedAssets,
            string[] movedAssets, string[] movedFromAssetPaths)
        {
            if (Application.isPlaying)
                return;

            if (UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings == null)
                return;

            HashSet<string> changedPaths = importedAssets
                .Concat(movedAssets)
                .Where(p => p.EndsWith(".asset", StringComparison.Ordinal))
                .ToHashSet();

            if (changedPaths.Count == 0)
                return;

            RebuildCache(changedPaths);

            // Bake asset GUIDs into m_Guid fields
            BakeGuids(changedPaths);

            // Process each collection that has changed assets in its folder
            foreach (CollectionInfo info in CachedCollections)
            {
                if (!info.Collection)
                    continue;

                bool collectionItselfChanged = changedPaths.Contains(info.Path);
                if (collectionItselfChanged)
                {
                    SOCAddressableUtility.EnsureCollectionAddressable(info.Collection, info.Path);
                }

                // Check if any changed asset is inside this collection's folder
                foreach (string changedPath in changedPaths)
                {
                    if (changedPath == info.Path)
                        continue;

                    if (changedPath.StartsWith(info.Folder, StringComparison.Ordinal))
                    {
                        Type assetType = AssetDatabase.GetMainAssetTypeAtPath(changedPath);
                        if (assetType != null && typeof(ISOCItem).IsAssignableFrom(assetType))
                        {
                            SOCAddressableUtility.EnsureItemAddressable(changedPath, info.Collection.AddressableLabel);
                        }
                    }
                }
            }

            // Auto-label IRegisteredSO assets
            foreach (string changedPath in changedPaths)
            {
                Type assetType = AssetDatabase.GetMainAssetTypeAtPath(changedPath);
                if (assetType != null && typeof(IRegisteredSO).IsAssignableFrom(assetType))
                {
                    SOCAddressableUtility.EnsureItemAddressable(changedPath, SORegistry.RegisteredLabel);
                }
            }
        }

        private static bool isBaking;

        private static void BakeGuids(HashSet<string> changedPaths)
        {
            if (isBaking)
                return;

            isBaking = true;
            try
            {
                foreach (string path in changedPaths)
                {
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
            finally
            {
                isBaking = false;
            }
        }

        /// <summary>
        /// Find the parent collection for an item path using the cached collection list.
        /// Falls back to a folder walk if cache isn't ready.
        /// </summary>
        public static ScriptableObjectCollection FindCollectionForItemPath(string itemPath)
        {
            if (string.IsNullOrEmpty(itemPath))
                return null;

            if (!cacheInitialized)
                RebuildCache(new HashSet<string>());

            if (cacheInitialized)
            {
                string itemFolder = Path.GetDirectoryName(itemPath)?.Replace('\\', '/');
                if (string.IsNullOrEmpty(itemFolder))
                    return null;

                // Walk up folder tree, checking against cached collection folders
                while (!string.IsNullOrEmpty(itemFolder) && itemFolder.StartsWith("Assets", StringComparison.Ordinal))
                {
                    string folderWithSlash = itemFolder + "/";
                    foreach (CollectionInfo info in CachedCollections)
                    {
                        if (info.Collection && info.Folder == folderWithSlash)
                            return info.Collection;
                    }
                    itemFolder = Path.GetDirectoryName(itemFolder)?.Replace('\\', '/');
                }
            }

            // Fallback: uncached lookup
            return SOCAddressableUtility.FindCollectionForItemPath(itemPath);
        }
    }
}
