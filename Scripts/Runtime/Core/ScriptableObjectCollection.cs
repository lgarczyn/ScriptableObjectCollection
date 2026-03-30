using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public abstract class ScriptableObjectCollection : ScriptableObject
    {
        [SerializeField, HideInInspector]
        private LongGuid guid;
        public LongGuid GUID
        {
            get
            {
                if (guid.IsValid())
                    return guid;

                GenerateNewGUID();
                return guid;
            }
        }

        /// <summary>
        /// The Addressable label used to tag all items belonging to this collection.
        /// </summary>
        public string AddressableLabel => $"soc_{GUID.ToBase64String()}";

        // Runtime: items loaded via Addressables, not serialized.
        [NonSerialized] private List<ScriptableObject> loadedItems;
        [NonSerialized] private AsyncOperationHandle<IList<ScriptableObject>> itemsHandle;
        [NonSerialized] private bool isLoaded;

        public bool IsLoaded => isLoaded;

        /// <summary>
        /// Access the items in this collection. At runtime, triggers a synchronous
        /// Addressables load on first access. In editor, must be populated via
        /// editor utilities (folder scan).
        /// </summary>
        public IReadOnlyList<ScriptableObject> Items
        {
            get
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    // In editor (not playing), items are populated externally
                    // via SOCEditorUtility.GetItemsInCollectionFolder()
                    if (loadedItems == null)
                        loadedItems = new List<ScriptableObject>();
                    return loadedItems;
                }
#endif
                if (!isLoaded)
                    LoadSync();
                return loadedItems;
            }
        }

        public int Count => Items.Count;

        public ScriptableObject this[int index] => Items[index];

        /// <summary>
        /// Synchronously load all items tagged with this collection's label via Addressables.
        /// Uses WaitForCompletion() which may cause a frame hitch on first call.
        /// </summary>
        public void LoadSync()
        {
            if (isLoaded)
                return;

            itemsHandle = Addressables.LoadAssetsAsync<ScriptableObject>(
                AddressableLabel, null);
            var results = itemsHandle.WaitForCompletion();
            loadedItems = new List<ScriptableObject>(results);

            foreach (var item in loadedItems)
            {
                if (item is ISOCItem socItem)
                    socItem.SetCollectionRuntime(this);
            }

            isLoaded = true;
        }

        /// <summary>
        /// Release all loaded item handles and clear the cache.
        /// </summary>
        public void Unload()
        {
            if (!isLoaded)
                return;

            if (itemsHandle.IsValid())
                Addressables.Release(itemsHandle);

            loadedItems = null;
            isLoaded = false;
            ClearCachedValues();
        }

        public void GenerateNewGUID()
        {
            guid = LongGuid.NewGuid();
            ObjectUtility.SetDirty(this);
        }

        public virtual Type GetItemType()
        {
            Type itemType = GetGenericItemType();
            return itemType?.GetGenericArguments().First();
        }

        private Type GetGenericItemType()
        {
            Type baseType = GetType().BaseType;

            while (baseType != null)
            {
                if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == typeof(ScriptableObjectCollection<>))
                    return baseType;
                baseType = baseType.BaseType;
            }
            return null;
        }

        public bool TryGetItemByName(string targetItemName, out ScriptableObject scriptableObjectCollectionItem)
        {
            var items = Items;
            for (int i = 0; i < items.Count; i++)
            {
                ScriptableObject item = items[i];
                if (item != null && string.Equals(item.name, targetItemName, StringComparison.Ordinal))
                {
                    scriptableObjectCollectionItem = item;
                    return true;
                }
            }

            scriptableObjectCollectionItem = null;
            return false;
        }

        public bool TryGetItemByGUID<T>(LongGuid itemGUID, out T scriptableObjectCollectionItem)
            where T : ScriptableObject
        {
            if (itemGUID.IsValid())
            {
                var items = Items;
                for (int i = 0; i < items.Count; i++)
                {
                    ScriptableObject item = items[i];
                    if (item is ISOCItem socItem && socItem.GUID == itemGUID)
                    {
                        scriptableObjectCollectionItem = item as T;
                        return scriptableObjectCollectionItem != null;
                    }
                }
            }

            scriptableObjectCollectionItem = null;
            return false;
        }

        public bool TryGetItemByGUID(LongGuid itemGUID, out ScriptableObject scriptableObjectCollectionItem)
        {
            return TryGetItemByGUID<ScriptableObject>(itemGUID, out scriptableObjectCollectionItem);
        }

        protected virtual void ClearCachedValues()
        {
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor-only: set the loaded items list from an external source (folder scan).
        /// </summary>
        public void SetEditorItems(List<ScriptableObject> items)
        {
            loadedItems = items;
        }
#endif
    }

    public class ScriptableObjectCollection<TObjectType> : ScriptableObjectCollection
        where TObjectType : ScriptableObject, ISOCItem
    {
        private static List<TObjectType> cachedValues;
        public static IReadOnlyList<TObjectType> Values
        {
            get
            {
                if (cachedValues == null)
                    cachedValues = CollectionsRegistry.Instance.GetAllCollectionItemsOfType<TObjectType>();
                return cachedValues;
            }
        }

        private static Dictionary<Type, IReadOnlyList<TObjectType>> cacheByType = new Dictionary<Type, IReadOnlyList<TObjectType>>();

        public static IReadOnlyList<TSubType> OfType<TSubType>() where TSubType : TObjectType
        {
            if (cacheByType.TryGetValue(typeof(TSubType), out IReadOnlyList<TObjectType> cachedList))
            {
                return (IReadOnlyList<TSubType>)cachedList;
            }
            List<TSubType> newList = Values.OfType<TSubType>().ToList();
            cacheByType[typeof(TSubType)] = newList;
            return newList;
        }

        public new TObjectType this[int index] => (TObjectType)base[index];

        public TObjectType GetItemByGUID(LongGuid targetGUID)
        {
            var items = Items;
            for (int i = 0; i < items.Count; i++)
            {
                ScriptableObject item = items[i];
                if (item is ISOCItem socItem && socItem.GUID == targetGUID)
                    return (TObjectType)item;
            }

            return null;
        }

        public bool TryGetItemByName<T>(string targetItemName, out T scriptableObjectCollectionItem) where T : TObjectType
        {
            var items = Items;
            for (int i = 0; i < items.Count; i++)
            {
                ScriptableObject item = items[i];
                if (string.Equals(item.name, targetItemName, StringComparison.Ordinal))
                {
                    scriptableObjectCollectionItem = item as T;
                    return scriptableObjectCollectionItem != null;
                }
            }

            scriptableObjectCollectionItem = null;
            return false;
        }

        public IEnumerator<TObjectType> GetEnumerator()
        {
            var items = Items;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] is TObjectType typed)
                    yield return typed;
            }
        }

        protected override void ClearCachedValues()
        {
            cachedValues = null;
            cacheByType.Clear();
        }
    }
}
