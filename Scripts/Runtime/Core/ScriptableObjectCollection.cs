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
        private string m_Guid;

        /// <summary>
        /// The Unity asset GUID, baked by the postprocessor.
        /// Used as the Addressable address for this collection.
        /// </summary>
        public string Guid => m_Guid;

        /// <summary>
        /// Addressable label applied to all items belonging to this collection.
        /// Same as the collection's asset GUID.
        /// </summary>
        public string AddressableLabel => m_Guid;

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
