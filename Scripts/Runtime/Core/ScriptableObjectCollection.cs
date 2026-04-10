using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

        [NonSerialized] private List<ScriptableObject> loadedItems;
        [NonSerialized] private AsyncOperationHandle<IList<ScriptableObject>> itemsHandle;
        [NonSerialized] private bool isLoaded;

        public bool IsLoaded => isLoaded;

        public void LoadSync()
        {
            if (isLoaded) return;

            try
            {
                itemsHandle = Addressables.LoadAssetsAsync<ScriptableObject>(AddressableLabel, null);
                var result = itemsHandle.WaitForCompletion();
                loadedItems = result != null ? new List<ScriptableObject>(result) : new List<ScriptableObject>();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to load items for collection '{name}': {e.Message}");
                loadedItems = new List<ScriptableObject>();
            }
            isLoaded = true;
        }

        public virtual void Unload()
        {
            if (!isLoaded) return;

            if (itemsHandle.IsValid())
                Addressables.Release(itemsHandle);

            loadedItems = null;
            isLoaded = false;
            ClearCache();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void OnSubsystemRegistration()
        {
            // Clear caches on domain reload and play mode enter
            ClearCache();
        }

        /// <summary>
        /// Untyped access to loaded items. Prefer Values on the generic subclass.
        /// </summary>
        public IReadOnlyList<ScriptableObject> GetLoadedItems()
        {
            if (!isLoaded)
                LoadSync();
            return loadedItems;
        }

        /// <summary>
        /// Load all collections via Addressables using the shared label.
        /// </summary>
        public static List<ScriptableObjectCollection> FindAll()
        {
            var handle = Addressables.LoadAssetsAsync<ScriptableObjectCollection>(AllCollectionsLabel, null);
            var results = handle.WaitForCompletion();
            return new List<ScriptableObjectCollection>(results);
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

        private static class Cache<T> where T : ScriptableObject
        {
            public static List<T> values;
            public static List<T> ofType;

            public static void Clear()
            {
                values = null;
                ofType = null;
            }
        }

        private static readonly List<Action> cacheClearActions = new();

        /// <summary>
        /// Get all items of a given type across ALL collections.
        /// Results are cached; call ClearCache() or Unload collections to invalidate.
        /// </summary>
        public static IReadOnlyList<T> OfType<T>() where T : ScriptableObject
        {
            if (Cache<T>.ofType != null)
                return Cache<T>.ofType;

            var result = new List<T>();
            foreach (var collection in FindAll())
            {
                var items = collection.GetLoadedItems();
                for (int i = 0; i < items.Count; i++)
                    if (items[i] is T typed)
                        result.Add(typed);
            }
            Cache<T>.ofType = result;
            RegisterCacheClear<T>();
            return result;
        }

        private static void RegisterCacheClear<T>() where T : ScriptableObject
        {
            cacheClearActions.Add(Cache<T>.Clear);
        }

        /// <summary>
        /// Clear the static Values/OfType caches.
        /// </summary>
        public static void ClearCache()
        {
            for (int i = 0; i < cacheClearActions.Count; i++)
                cacheClearActions[i]();
            cacheClearActions.Clear();
        }

        /// <summary>
        /// Find an item by its baked asset GUID across this collection's loaded items.
        /// </summary>
        public bool TryGetItemByGUID<T>(string guid, out T result) where T : ScriptableObject, ISOCItem
        {
            var items = GetLoadedItems();
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] is T typed && typed is ISOCItem socItem && socItem.Guid == guid)
                {
                    result = typed;
                    return true;
                }
            }
            result = null;
            return false;
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

        public virtual Type GetItemType()
        {
            Type baseType = GetType().BaseType;
            while (baseType != null)
            {
                if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == typeof(ScriptableObjectCollection<>))
                    return baseType.GetGenericArguments().First();
                baseType = baseType.BaseType;
            }
            return null;
        }
    }

    public class ScriptableObjectCollection<TObjectType> : ScriptableObjectCollection, IEnumerable<TObjectType>
        where TObjectType : ScriptableObject, ISOCItem
    {
        /// <summary>
        /// All items of this collection's type across all collections. Cached.
        /// </summary>
        public static IReadOnlyList<TObjectType> Values => OfType<TObjectType>();

        public override void Unload()
        {
            base.Unload();
        }
        public IEnumerable<TObjectType> Items => GetLoadedItems().OfType<TObjectType>();
        // TODO: clean this mess
        public IEnumerator<TObjectType> GetEnumerator() => GetLoadedItems().OfType<TObjectType>().GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
