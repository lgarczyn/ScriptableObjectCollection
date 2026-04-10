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

#if UNITY_EDITOR
        /// <summary>
        /// Find all collections in the project. Only loads items for collections that need them.
        /// </summary>
        public static List<ScriptableObjectCollection> FindAllInEditor()
        {
            var result = new List<ScriptableObjectCollection>();
            string[] guids = UnityEditor.AssetDatabase.FindAssets($"t:{nameof(ScriptableObjectCollection)}");
            foreach (string assetGuid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(assetGuid);
                var collection = UnityEditor.AssetDatabase.LoadAssetAtPath<ScriptableObjectCollection>(path);
                if (collection != null)
                    result.Add(collection);
            }
            return result;
        }

        /// <summary>
        /// Find collections whose item type matches, and ensure their items are loaded.
        /// Only loads items for matching collections — skips the rest.
        /// </summary>
        public static List<ScriptableObjectCollection> FindByItemTypeInEditor(Type targetItemType)
        {
            var result = new List<ScriptableObjectCollection>();
            foreach (var collection in FindAllInEditor())
            {
                Type itemType = collection.GetItemType();
                if (itemType != null && itemType.IsAssignableFrom(targetItemType))
                {
                    EnsureEditorItemsLoaded(collection);
                    result.Add(collection);
                }
            }
            return result;
        }

        /// <summary>
        /// Find a collection by GUID and ensure its items are loaded.
        /// </summary>
        public static bool TryFindByGUIDInEditor(LongGuid targetGUID, out ScriptableObjectCollection result)
        {
            foreach (var collection in FindAllInEditor())
            {
                if (collection.GUID == targetGUID)
                {
                    EnsureEditorItemsLoaded(collection);
                    result = collection;
                    return true;
                }
            }
            result = null;
            return false;
        }

        /// <summary>
        /// Ensures a collection's Items are populated from its folder in editor.
        /// Inlined here so the runtime assembly doesn't need to reference the editor assembly.
        /// </summary>
        private static void EnsureEditorItemsLoaded(ScriptableObjectCollection collection)
        {
            if (collection.Items.Count > 0)
                return;

            string collectionPath = UnityEditor.AssetDatabase.GetAssetPath(collection);
            string folder = System.IO.Path.GetDirectoryName(collectionPath);
            Type itemType = collection.GetItemType();
            if (itemType == null)
                return;

            string[] itemGuids = UnityEditor.AssetDatabase.FindAssets($"t:{itemType.Name}", new[] { folder });
            var items = new List<ScriptableObject>();
            foreach (string itemGuid in itemGuids)
            {
                string itemPath = UnityEditor.AssetDatabase.GUIDToAssetPath(itemGuid);
                var item = UnityEditor.AssetDatabase.LoadAssetAtPath<ScriptableObject>(itemPath);
                if (item is ISOCItem)
                    items.Add(item);
            }
            collection.SetEditorItems(items);
        }
#endif

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
