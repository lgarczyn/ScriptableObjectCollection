using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.Scripting;

namespace BrunoMikoski.ScriptableObjectCollections
{
    [Preserve]
    public class CollectionsRegistry : ScriptableObject
    {
        public const string RegistryAddress = "SOC_Registry";

        [SerializeField]
        private List<CollectionMetadata> entries = new List<CollectionMetadata>();
        public IReadOnlyList<CollectionMetadata> Entries => entries;

        // Runtime cache of loaded collections (lazy-loaded via Addressables)
        [NonSerialized]
        private Dictionary<LongGuid, ScriptableObjectCollection> loadedCollections
            = new Dictionary<LongGuid, ScriptableObjectCollection>();

        [NonSerialized]
        private Dictionary<LongGuid, AsyncOperationHandle<ScriptableObjectCollection>> collectionHandles
            = new Dictionary<LongGuid, AsyncOperationHandle<ScriptableObjectCollection>>();

        private static CollectionsRegistry instance;
        public static CollectionsRegistry Instance
        {
            get
            {
                if (instance == null)
                {
#if UNITY_EDITOR
                    if (!Application.isPlaying)
                    {
                        // In editor, load via AssetDatabase
                        instance = LoadInEditor();
                        return instance;
                    }
#endif
                    var handle = Addressables.LoadAssetAsync<CollectionsRegistry>(RegistryAddress);
                    instance = handle.WaitForCompletion();
                }
                return instance;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            // Pre-load registry (lightweight, just metadata).
            _ = Instance;
        }

#if UNITY_EDITOR
        private static CollectionsRegistry LoadInEditor()
        {
            string[] assets = UnityEditor.AssetDatabase.FindAssets($"t:{nameof(CollectionsRegistry)}");
            if (assets.Length > 0)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(assets[0]);
                return UnityEditor.AssetDatabase.LoadAssetAtPath<CollectionsRegistry>(path);
            }

            // Create if missing
            var newInstance = CreateInstance<CollectionsRegistry>();
            AssetDatabaseUtils.CreatePathIfDoesntExist("Assets/SOC");
            UnityEditor.AssetDatabase.CreateAsset(newInstance, "Assets/SOC/CollectionsRegistry.asset");
            UnityEditor.AssetDatabase.SaveAssets();
            return newInstance;
        }
#endif

        /// <summary>
        /// Load a collection by its GUID. Uses WaitForCompletion() for synchronous access.
        /// Caches the result for subsequent calls.
        /// </summary>
        public ScriptableObjectCollection GetOrLoadCollection(LongGuid guid)
        {
            if (loadedCollections.TryGetValue(guid, out var cached))
                return cached;

            var meta = FindMetadata(guid);
            if (string.IsNullOrEmpty(meta.AddressableAddress))
                return null;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                // In editor, load via AssetDatabase using address to find it
                return LoadCollectionInEditor(meta);
            }
#endif

            var handle = Addressables.LoadAssetAsync<ScriptableObjectCollection>(meta.AddressableAddress);
            var collection = handle.WaitForCompletion();
            if (collection != null)
            {
                loadedCollections[guid] = collection;
                collectionHandles[guid] = handle;
            }
            return collection;
        }

#if UNITY_EDITOR
        private ScriptableObjectCollection LoadCollectionInEditor(CollectionMetadata meta)
        {
            string[] guids = UnityEditor.AssetDatabase.FindAssets($"t:{nameof(ScriptableObjectCollection)}");
            foreach (string assetGuid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(assetGuid);
                var collection = UnityEditor.AssetDatabase.LoadAssetAtPath<ScriptableObjectCollection>(path);
                if (collection != null && collection.GUID == meta.GUID)
                {
                    loadedCollections[meta.GUID] = collection;
                    return collection;
                }
            }
            return null;
        }
#endif

        public void UnloadCollection(LongGuid guid)
        {
            if (loadedCollections.TryGetValue(guid, out var collection))
            {
                collection.Unload();
                loadedCollections.Remove(guid);
            }
            if (collectionHandles.TryGetValue(guid, out var handle))
            {
                if (handle.IsValid())
                    Addressables.Release(handle);
                collectionHandles.Remove(guid);
            }
        }

        private CollectionMetadata FindMetadata(LongGuid guid)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].GUID == guid)
                    return entries[i];
            }
            return default;
        }

        public bool TryGetCollectionByGUID<T>(LongGuid targetGUID, out T resultCollection)
            where T : ScriptableObjectCollection
        {
            if (targetGUID.IsValid())
            {
                var collection = GetOrLoadCollection(targetGUID);
                if (collection is T typed)
                {
                    resultCollection = typed;
                    return true;
                }
            }

            resultCollection = null;
            return false;
        }

        public bool TryGetCollectionByGUID(LongGuid targetGUID, out ScriptableObjectCollection resultCollection)
        {
            if (targetGUID.IsValid())
            {
                resultCollection = GetOrLoadCollection(targetGUID);
                return resultCollection != null;
            }

            resultCollection = null;
            return false;
        }

        public ScriptableObjectCollection GetCollectionByGUID(LongGuid guid)
        {
            return GetOrLoadCollection(guid);
        }

        public bool TryGetCollectionFromItemType(Type targetType, out ScriptableObjectCollection resultCollection)
        {
            // Load all collections and find the one whose item type matches
            foreach (var meta in entries)
            {
                var collection = GetOrLoadCollection(meta.GUID);
                if (collection == null) continue;

                Type itemType = collection.GetItemType();
                if (itemType != null && itemType.IsAssignableFrom(targetType))
                {
                    resultCollection = collection;
                    return true;
                }
            }

            resultCollection = null;
            return false;
        }

        public bool TryGetCollectionOfType(Type type, out ScriptableObjectCollection resultCollection)
        {
            foreach (var meta in entries)
            {
                var collection = GetOrLoadCollection(meta.GUID);
                if (collection != null && collection.GetType() == type)
                {
                    resultCollection = collection;
                    return true;
                }
            }

            resultCollection = null;
            return false;
        }

        public bool TryGetCollectionOfType<T>(out T resultCollection)
            where T : ScriptableObjectCollection
        {
            bool didFind = TryGetCollectionOfType(typeof(T), out ScriptableObjectCollection baseCollection);
            resultCollection = baseCollection as T;
            return didFind;
        }

        public List<T> GetAllCollectionItemsOfType<T>() where T : ScriptableObject, ISOCItem
        {
            List<T> result = new List<T>();
            foreach (var meta in entries)
            {
                var collection = GetOrLoadCollection(meta.GUID);
                if (collection == null) continue;

                Type collectionItemType = collection.GetItemType();
                if (collectionItemType == null || !typeof(T).IsAssignableFrom(collectionItemType))
                    continue;

                var items = collection.Items;
                for (int i = 0; i < items.Count; i++)
                {
                    if (items[i] is T typed)
                        result.Add(typed);
                }
            }

            return result;
        }

        public List<ScriptableObjectCollection> GetCollectionsByItemType(Type targetCollectionItemType)
        {
            List<ScriptableObjectCollection> result = new List<ScriptableObjectCollection>();
            foreach (var meta in entries)
            {
                var collection = GetOrLoadCollection(meta.GUID);
                if (collection == null) continue;

                if (collection.GetItemType().IsAssignableFrom(targetCollectionItemType))
                    result.Add(collection);
            }
            return result;
        }

        public bool TryGetCollectionsOfItemType(Type targetType, out List<ScriptableObjectCollection> results)
        {
            List<ScriptableObjectCollection> availables = new();
            int minDistance = int.MaxValue;

            foreach (var meta in entries)
            {
                var collection = GetOrLoadCollection(meta.GUID);
                if (collection == null) continue;

                Type itemType = collection.GetItemType();
                if (itemType == null || !itemType.IsAssignableFrom(targetType))
                    continue;

                int distance = GetInheritanceDistance(targetType, itemType);
                if (distance < minDistance)
                {
                    availables.Clear();
                    availables.Add(collection);
                    minDistance = distance;
                }
                else if (distance == minDistance)
                {
                    availables.Add(collection);
                }
            }

            if (availables.Count == 0)
            {
                results = null;
                return false;
            }

            results = availables;
            return true;
        }

        private int GetInheritanceDistance(Type fromType, Type toType)
        {
            int distance = 0;
            Type currentType = fromType;
            while (currentType != null && currentType != toType)
            {
                currentType = currentType.BaseType;
                distance++;
            }
            if (currentType == toType)
                return distance;
            return int.MaxValue;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor-only: Replace the entries list with new metadata.
        /// Called by SOCAddressableUtility when syncing.
        /// </summary>
        public void SetEntries(List<CollectionMetadata> newEntries)
        {
            entries = newEntries;
            ObjectUtility.SetDirty(this);
        }

        /// <summary>
        /// Editor-only: Add or update a single entry.
        /// </summary>
        public void AddOrUpdateEntry(CollectionMetadata metadata)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].GUID == metadata.GUID)
                {
                    entries[i] = metadata;
                    ObjectUtility.SetDirty(this);
                    return;
                }
            }
            entries.Add(metadata);
            ObjectUtility.SetDirty(this);
        }

        /// <summary>
        /// Editor-only: Remove entry by GUID.
        /// </summary>
        public void RemoveEntry(LongGuid guid)
        {
            entries.RemoveAll(e => e.GUID == guid);
            ObjectUtility.SetDirty(this);
        }
#endif
    }
}
