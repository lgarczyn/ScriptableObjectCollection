using System;
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
        private LongGuid guid;
        public LongGuid GUID
        {
            get
            {
#if UNITY_EDITOR
                if (!guid.IsValid())
                    GenerateNewGUID();
#endif
                return guid;
            }
        }

        public string AddressableLabel => $"soc_{GUID.ToBase64String()}";
        public string AddressableAddress => GetAddressableAddress(GUID);

        public static string GetAddressableAddress(LongGuid collectionGuid)
        {
            return $"soc_collection_{collectionGuid.ToBase64String()}";
        }

        public static ScriptableObjectCollection LoadByGUID(LongGuid collectionGuid)
        {
            if (!collectionGuid.IsValid())
                return null;

            string address = GetAddressableAddress(collectionGuid);
            var handle = Addressables.LoadAssetAsync<ScriptableObjectCollection>(address);
            return handle.WaitForCompletion();
        }

        /// <summary>
        /// Load all collections via Addressables using the shared label.
        /// Works in both editor and runtime.
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

        [NonSerialized] private List<ScriptableObject> loadedItems;
        [NonSerialized] private AsyncOperationHandle<IList<ScriptableObject>> itemsHandle;
        [NonSerialized] private bool isLoaded;

        public bool IsLoaded => isLoaded;

        public IReadOnlyList<ScriptableObject> Items
        {
            get
            {
                if (!isLoaded)
                    LoadSync();
                return loadedItems;
            }
        }

        public int Count => Items.Count;
        public ScriptableObject this[int index] => Items[index];

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

        public void Unload()
        {
            if (!isLoaded) return;

            if (itemsHandle.IsValid())
                Addressables.Release(itemsHandle);

            loadedItems = null;
            isLoaded = false;
        }

#if UNITY_EDITOR
        public void GenerateNewGUID()
        {
            guid = LongGuid.NewGuid();
            ObjectUtility.SetDirty(this);
        }
#endif

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

        public bool TryGetItemByName(string targetItemName, out ScriptableObject result)
        {
            return TryGetItemByName<ScriptableObject>(targetItemName, out result);
        }

        public bool TryGetItemByName<T>(string targetItemName, out T result) where T : ScriptableObject
        {
            var items = Items;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] != null && string.Equals(items[i].name, targetItemName, StringComparison.Ordinal) && items[i] is T typed)
                {
                    result = typed;
                    return true;
                }
            }
            result = null;
            return false;
        }
    }

    public class ScriptableObjectCollection<TObjectType> : ScriptableObjectCollection
        where TObjectType : ScriptableObject, ISOCItem
    {
        public new TObjectType this[int index] => (TObjectType)base[index];

        public IEnumerator<TObjectType> GetEnumerator()
        {
            var items = Items;
            for (int i = 0; i < items.Count; i++)
                if (items[i] is TObjectType typed)
                    yield return typed;
        }
    }
}
