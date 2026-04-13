using System;
using System.Collections.Generic;
using System.IO;
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
            public string Path; // e.g. "Assets/Data/Enemies/EnemyCollection.asset"
            public string Folder; // e.g. "Assets/Data/Enemies/"
            public string Label; // The collection's asset GUID, used as label for its items
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

                string assetGuid = AssetDatabase.AssetPathToGUID(path);
                CachedCollections.Add(new CollectionInfo
                {
                    Collection = collection,
                    Path = path,
                    Folder = folder,
                    Label = assetGuid,
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

            // Handle deleted collections: strip their labels from orphaned items
            if (deletedAssets.Length > 0)
                CleanUpDeletedCollections(deletedAssets);

            HashSet<string> changedPaths = importedAssets
                .Concat(movedAssets)
                .Where(p => p.EndsWith(".asset", StringComparison.Ordinal))
                .ToHashSet();

            if (changedPaths.Count == 0)
                return;

            RebuildCache(changedPaths);

            int totalSteps = changedPaths.Count;
            int currentStep = 0;

            try
            {
                // Bake asset GUIDs into m_Guid fields
                BakeGuids(changedPaths);

                // Build set of all known collection GUIDs for stale label detection
                var allCollectionGuids = new HashSet<string>();
                foreach (CollectionInfo info in CachedCollections)
                    if (info.Collection)
                        allCollectionGuids.Add(info.Label);

                // Ensure changed collections themselves are addressable
                foreach (CollectionInfo info in CachedCollections)
                {
                    if (info.Collection && changedPaths.Contains(info.Path))
                        SOCAddressableUtility.EnsureCollectionAddressable(info.Path);
                }

                // Process each changed asset
                foreach (string changedPath in changedPaths)
                {
                    currentStep++;
                    if (EditorUtility.DisplayCancelableProgressBar(
                            "SOC Postprocessor",
                            changedPath,
                            (float)currentStep / totalSteps))
                    {
                        break;
                    }

                    Type assetType = AssetDatabase.GetMainAssetTypeAtPath(changedPath);
                    if (assetType == null)
                        continue;

                    // Reconcile item labels
                    if (typeof(ISOCItem).IsAssignableFrom(assetType))
                    {
                        var correctLabels = new HashSet<string>();
                        foreach (CollectionInfo info in CachedCollections)
                        {
                            if (!info.Collection)
                                continue;
                            if (changedPath == info.Path)
                                continue;
                            if (changedPath.StartsWith(info.Folder, StringComparison.Ordinal))
                                correctLabels.Add(info.Label);
                        }

                        if (correctLabels.Count > 0)
                            SOCAddressableUtility.ReconcileItemLabels(changedPath, correctLabels, allCollectionGuids);
                    }

                    // Auto-label IRegisteredSO assets
                    if (typeof(IRegisteredSO).IsAssignableFrom(assetType))
                    {
                        SOCAddressableUtility.EnsureRegisteredAddressable(changedPath);
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private static void CleanUpDeletedCollections(string[] deletedAssets)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) return;

            var deletedCollectionGuids = new HashSet<string>();
            foreach (string deletedPath in deletedAssets)
            {
                if (!deletedPath.EndsWith(".asset", StringComparison.Ordinal))
                    continue;

                string guid = AssetDatabase.AssetPathToGUID(deletedPath);
                if (string.IsNullOrEmpty(guid))
                    continue;

                var entry = settings.FindAssetEntry(guid);
                if (entry != null && entry.labels.Contains(SOCAddressableUtility.CollectionsLabel))
                {
                    deletedCollectionGuids.Add(guid);
                    entry.parentGroup.RemoveAssetEntry(entry);
                }
            }

            if (deletedCollectionGuids.Count == 0)
                return;

            // Strip deleted collection GUIDs as labels from remaining entries
            foreach (var group in settings.groups)
            {
                if (group == null) continue;
                foreach (var entry in group.entries)
                {
                    foreach (string collectionGuid in deletedCollectionGuids)
                        entry.labels.Remove(collectionGuid);
                }
            }

            InvalidateCache();
        }

        private static void BakeGuids(HashSet<string> changedPaths)
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
