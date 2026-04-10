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

        [NonSerialized]
        private Dictionary<LongGuid, ScriptableObjectCollection> loadedCollections = new();

        [NonSerialized]
        private Dictionary<LongGuid, AsyncOperationHandle<ScriptableObjectCollection>> collectionHandles = new();

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

            var newInstance = CreateInstance<CollectionsRegistry>();
            AssetDatabaseUtils.CreatePathIfDoesntExist("Assets/SOC");
            UnityEditor.AssetDatabase.CreateAsset(newInstance, "Assets/SOC/CollectionsRegistry.asset");
            UnityEditor.AssetDatabase.SaveAssets();
            return newInstance;
        }
#endif

        public ScriptableObjectCollection GetOrLoadCollection(LongGuid guid)
        {
            if (!guid.IsValid())
                return null;

            if (loadedCollections.TryGetValue(guid, out var cached))
                return cached;

            var meta = FindMetadata(guid);
            if (string.IsNullOrEmpty(meta.AddressableAddress))
                return null;

#if UNITY_EDITOR
            if (!Application.isPlaying)
                return LoadCollectionInEditor(meta);
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

        /// <summary>
        /// Primary lookup: get a collection by GUID, loading it if needed.
        /// All other lookup methods are convenience wrappers around this.
        /// </summary>
        public bool TryGetCollectionByGUID(LongGuid targetGUID, out ScriptableObjectCollection resultCollection)
        {
            resultCollection = GetOrLoadCollection(targetGUID);
            return resultCollection != null;
        }

        public bool TryGetCollectionFromItemType(Type targetType, out ScriptableObjectCollection resultCollection)
        {
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

        public List<T> GetAllCollectionItemsOfType<T>() where T : ScriptableObject, ISOCItem
        {
            var result = new List<T>();
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
            var result = new List<ScriptableObjectCollection>();
            foreach (var meta in entries)
            {
                var collection = GetOrLoadCollection(meta.GUID);
                if (collection != null && collection.GetItemType().IsAssignableFrom(targetCollectionItemType))
                    result.Add(collection);
            }
            return result;
        }

#if UNITY_EDITOR
        public void SetEntries(List<CollectionMetadata> newEntries)
        {
            entries = newEntries;
            ObjectUtility.SetDirty(this);
        }

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

        public void RemoveEntry(LongGuid guid)
        {
            entries.RemoveAll(e => e.GUID == guid);
            ObjectUtility.SetDirty(this);
        }
#endif
    }
}
