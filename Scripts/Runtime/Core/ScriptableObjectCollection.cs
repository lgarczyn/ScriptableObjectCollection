using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public abstract class ScriptableObjectCollection : ScriptableObject
    {
        public const string AllCollectionsLabel = "soc_collections";

        [SerializeField, HideInInspector]
        private string m_Guid;

        /// <summary>
        /// The Unity asset GUID, baked by the postprocessor.
        /// </summary>
        public string Guid => m_Guid;

        /// <summary>
        /// Addressable label applied to all items belonging to this collection.
        /// </summary>
        public string AddressableLabel => m_Guid;

        public abstract bool IsLoaded { get; }
        public abstract void Load();
        public abstract void Unload();

        /// <summary>
        /// Untyped access to loaded items for editor code.
        /// Returns the same objects as the typed Items property on the generic subclass.
        /// </summary>
        public abstract IReadOnlyList<ScriptableObject> ItemsGeneric { get; }

        public abstract Type GetItemType();

        // ---- Static: FindAll (cached with handle) ----

        private static AsyncOperationHandle<IList<ScriptableObjectCollection>> findAllHandle;
        private static List<ScriptableObjectCollection> cachedFindAll;

        /// <summary>
        /// Load all collections via Addressables. Cached — only loads once.
        /// </summary>
        public static IReadOnlyList<ScriptableObjectCollection> FindAll()
        {
            if (cachedFindAll != null)
                return cachedFindAll;

            try
            {
                findAllHandle = Addressables.LoadAssetsAsync<ScriptableObjectCollection>(AllCollectionsLabel, null);
                var results = findAllHandle.WaitForCompletion();
                cachedFindAll = results != null
                    ? new List<ScriptableObjectCollection>(results)
                    : new List<ScriptableObjectCollection>();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to load collections: {e.Message}");
                cachedFindAll = new List<ScriptableObjectCollection>();
            }

            return cachedFindAll;
        }

        /// <summary>
        /// Load all collections whose item type matches the given type.
        /// </summary>
        public static List<ScriptableObjectCollection> FindByItemType(Type targetItemType)
        {
            var result = new List<ScriptableObjectCollection>();
            foreach (var collection in FindAll())
            {
                Type itemType = collection.GetItemType();
                if (itemType != null && itemType.IsAssignableFrom(targetItemType))
                    result.Add(collection);
            }
            return result;
        }

        // ---- Static: OfType cache ----

        private static class Cache<T> where T : ScriptableObject
        {
            public static List<T> ofType;
            public static void Clear() => ofType = null;
        }

        private static readonly List<Action> cacheClearActions = new();

        /// <summary>
        /// Get all items of a given type across ALL collections. Cached.
        /// </summary>
        public static IReadOnlyList<T> OfType<T>() where T : ScriptableObject
        {
            if (Cache<T>.ofType != null)
                return Cache<T>.ofType;

            var result = new List<T>();
            var seen = new HashSet<T>();
            foreach (var collection in FindAll())
            {
                var items = collection.ItemsGeneric;
                for (int i = 0; i < items.Count; i++)
                    if (items[i] is T typed && seen.Add(typed))
                        result.Add(typed);
            }
            Cache<T>.ofType = result;
            cacheClearActions.Add(Cache<T>.Clear);
            return result;
        }

        // ---- Static: GUID lookups ----

        /// <summary>
        /// Load an item by its asset GUID via Addressables.
        /// </summary>
        public static bool TryGetItemByGUID<T>(string guid, out T result) where T : class, ISOCItem
        {
            if (string.IsNullOrEmpty(guid))
            {
                result = null;
                return false;
            }

            result = Addressables.LoadAssetAsync<T>(guid).WaitForCompletion();
            return result != null;
        }

        /// <summary>
        /// Find a collection by its baked asset GUID.
        /// </summary>
        public static bool TryGetCollectionByGUID(string guid, out ScriptableObjectCollection result)
        {
            foreach (var collection in FindAll())
            {
                if (collection.Guid == guid)
                {
                    result = collection;
                    return true;
                }
            }
            result = null;
            return false;
        }

        /// <summary>
        /// Find a collection by its baked asset GUID, typed.
        /// </summary>
        public static bool TryGetCollectionByGUID<T>(string guid, out T result) where T : ScriptableObjectCollection
        {
            if (TryGetCollectionByGUID(guid, out var collection) && collection is T typed)
            {
                result = typed;
                return true;
            }
            result = null;
            return false;
        }

        // ---- Static: cache lifecycle ----

        /// <summary>
        /// Clear all static caches. Called on domain reload and play mode exit.
        /// Does NOT unload individual collections.
        /// </summary>
        public static void ClearCache()
        {
            for (int i = 0; i < cacheClearActions.Count; i++)
                cacheClearActions[i]();
            cacheClearActions.Clear();

            cachedFindAll = null;
            if (findAllHandle.IsValid())
            {
                Addressables.Release(findAllHandle);
                findAllHandle = default;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void OnSubsystemRegistration()
        {
            ClearCache();
        }
    }

    public class ScriptableObjectCollection<TObjectType> : ScriptableObjectCollection, IEnumerable<TObjectType>
        where TObjectType : ScriptableObject, ISOCItem
    {
        [NonSerialized] private List<TObjectType> loadedItems;
        [NonSerialized] private AsyncOperationHandle<IList<ScriptableObject>> itemsHandle;
        [NonSerialized] private bool isLoaded;

        public override bool IsLoaded => isLoaded;

        /// <summary>
        /// Typed items in this collection. Loads on first access.
        /// </summary>
        public override IReadOnlyList<ScriptableObject> ItemsGeneric => Items;

        public IReadOnlyList<TObjectType> Items
        {
            get
            {
                if (!isLoaded)
                    Load();
                return loadedItems;
            }
        }

        /// <summary>
        /// All items of this collection's type across all collections. Cached.
        /// </summary>
        public static IReadOnlyList<TObjectType> Values => OfType<TObjectType>();

        public override void Load()
        {
            if (isLoaded) return;

            try
            {
                if (string.IsNullOrEmpty(AddressableLabel))
                {
                    Debug.LogError($"Collection '{name}' has empty AddressableLabel (m_Guid not baked). Run Sync All Addressables.");
                    loadedItems = new List<TObjectType>();
                    isLoaded = true;
                    return;
                }

                itemsHandle = Addressables.LoadAssetsAsync<ScriptableObject>(AddressableLabel, null);
                var result = itemsHandle.WaitForCompletion();

                loadedItems = new List<TObjectType>();
                if (result != null)
                {
                    foreach (var item in result)
                        if (item is TObjectType typed)
                            loadedItems.Add(typed);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to load items for collection '{name}': {e.Message}");
                loadedItems = new List<TObjectType>();
            }

            isLoaded = true;
        }

        public override void Unload()
        {
            if (!isLoaded) return;

            if (itemsHandle.IsValid())
                Addressables.Release(itemsHandle);

            loadedItems = null;
            isLoaded = false;
            ClearCache();
        }

        public override Type GetItemType() => typeof(TObjectType);

        public IEnumerator<TObjectType> GetEnumerator() => Items.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
