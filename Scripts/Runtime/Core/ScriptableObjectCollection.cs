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

        public string AddressableLabel => $"soc_{GUID.ToBase64String()}";

        [NonSerialized] private List<ScriptableObject> loadedItems;
        [NonSerialized] private AsyncOperationHandle<IList<ScriptableObject>> itemsHandle;
        [NonSerialized] private bool isLoaded;

        public bool IsLoaded => isLoaded;

        public IReadOnlyList<ScriptableObject> Items
        {
            get
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
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

        public void LoadSync()
        {
            if (isLoaded) return;

            itemsHandle = Addressables.LoadAssetsAsync<ScriptableObject>(AddressableLabel, null);
            loadedItems = new List<ScriptableObject>(itemsHandle.WaitForCompletion());

            foreach (var item in loadedItems)
                if (item is ISOCItem socItem)
                    socItem.SetCollectionRuntime(this);

            isLoaded = true;
        }

        public void Unload()
        {
            if (!isLoaded) return;

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
            var items = Items;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] != null && string.Equals(items[i].name, targetItemName, StringComparison.Ordinal))
                {
                    result = items[i];
                    return true;
                }
            }
            result = null;
            return false;
        }

        public bool TryGetItemByGUID<T>(LongGuid itemGUID, out T result) where T : ScriptableObject
        {
            if (itemGUID.IsValid())
            {
                var items = Items;
                for (int i = 0; i < items.Count; i++)
                {
                    if (items[i] is ISOCItem socItem && socItem.GUID == itemGUID)
                    {
                        result = items[i] as T;
                        return result != null;
                    }
                }
            }
            result = null;
            return false;
        }

        public bool TryGetItemByGUID(LongGuid itemGUID, out ScriptableObject result)
        {
            return TryGetItemByGUID<ScriptableObject>(itemGUID, out result);
        }

        protected virtual void ClearCachedValues() { }

#if UNITY_EDITOR
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

        public new TObjectType this[int index] => (TObjectType)base[index];

        public IEnumerator<TObjectType> GetEnumerator()
        {
            var items = Items;
            for (int i = 0; i < items.Count; i++)
                if (items[i] is TObjectType typed)
                    yield return typed;
        }

        protected override void ClearCachedValues()
        {
            cachedValues = null;
        }
    }
}
