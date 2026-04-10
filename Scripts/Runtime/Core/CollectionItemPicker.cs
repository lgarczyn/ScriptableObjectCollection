using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace BrunoMikoski.ScriptableObjectCollections.Picker
{
    /// <summary>
    /// Collection Item Picker lets you pick one or more items from a collection, similar to how an enum field would
    /// work if the enum had the [Flags] attribute applied to it.
    /// Uses AssetReference for lazy Addressable-based loading.
    /// </summary>
    [Serializable]
    public class CollectionItemPicker<TItemType> : IList<TItemType>, IEquatable<IList<TItemType>>, IEquatable<CollectionItemPicker<TItemType>>
        where TItemType : ScriptableObject, ISOCItem
    {
        [SerializeField]
        private List<AssetReference> itemReferences = new();

        public event Action<TItemType> OnItemTypeAddedEvent;
        public event Action<TItemType> OnItemTypeRemovedEvent;
        public event Action OnChangedEvent;

        private bool isDirty = true;
        private List<TItemType> cachedItems = new();
        public List<TItemType> Items
        {
            get
            {
                if (!Application.isPlaying || isDirty)
                {
                    cachedItems.Clear();

                    for (int i = itemReferences.Count - 1; i >= 0; i--)
                    {
                        AssetReference assetRef = itemReferences[i];
                        TItemType item = ResolveReference(assetRef);
                        if (item == null)
                        {
                            itemReferences.RemoveAt(i);
                            continue;
                        }

                        cachedItems.Add(item);
                    }

                    // Reverse because we iterated backwards
                    cachedItems.Reverse();
                    isDirty = false;
                }

                return cachedItems;
            }
        }

        private static TItemType ResolveReference(AssetReference assetRef)
        {
            if (assetRef == null || !assetRef.RuntimeKeyIsValid())
                return null;

#if UNITY_EDITOR
            if (!Application.isPlaying)
                return assetRef.editorAsset as TItemType;
#endif
            if (assetRef.Asset != null)
                return assetRef.Asset as TItemType;

            return assetRef.LoadAssetAsync<TItemType>().WaitForCompletion();
        }

        public CollectionItemPicker()
        {
        }

        public CollectionItemPicker(params TItemType[] items)
        {
            for (int i = 0; i < items.Length; i++)
                Add(items[i]);
        }

        #region Booleans and Checks
        public bool HasAll(params TItemType[] itemTypes)
        {
            for (int i = 0; i < itemTypes.Length; i++)
            {
                if (!Contains(itemTypes[i]))
                    return false;
            }

            return true;
        }

        public bool HasAll(IList<TItemType> itemTypes)
        {
            for (int i = 0; i < itemTypes.Count; i++)
            {
                if (!Contains(itemTypes[i]))
                    return false;
            }

            return true;
        }

        #endregion

        public static implicit operator List<TItemType>(CollectionItemPicker<TItemType> targetPicker)
        {
            return targetPicker.Items;
        }

        #region IList members implementation

        public IEnumerator<TItemType> GetEnumerator()
        {
            return Items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Items.GetEnumerator();
        }

        public void Add(TItemType item)
        {
            if (Contains(item))
                return;

            string guid = GetAssetGUID(item);
            if (string.IsNullOrEmpty(guid))
                return;

            itemReferences.Add(new AssetReference(guid));
            isDirty = true;
            OnItemTypeAddedEvent?.Invoke(item);
            OnChangedEvent?.Invoke();
        }

        public void Clear()
        {
            itemReferences.Clear();
            isDirty = true;
            OnChangedEvent?.Invoke();
        }

        public bool Contains(TItemType item)
        {
            for (int i = 0; i < itemReferences.Count; i++)
            {
                TItemType resolved = ResolveReference(itemReferences[i]);
                if (resolved == item)
                    return true;
            }

            return false;
        }

        public void CopyTo(TItemType[] array, int arrayIndex)
        {
            var items = Items;
            for (int i = 0; i < items.Count; i++)
                array[arrayIndex + i] = items[i];
        }

        public bool Remove(TItemType item)
        {
            for (int i = 0; i < itemReferences.Count; i++)
            {
                TItemType resolved = ResolveReference(itemReferences[i]);
                if (resolved == item)
                {
                    itemReferences.RemoveAt(i);
                    isDirty = true;
                    OnChangedEvent?.Invoke();
                    OnItemTypeRemovedEvent?.Invoke(item);
                    return true;
                }
            }

            return false;
        }

        public int Count => itemReferences.Count;

        public bool IsReadOnly => false;

        public int IndexOf(TItemType item)
        {
            for (int i = 0; i < itemReferences.Count; i++)
            {
                TItemType resolved = ResolveReference(itemReferences[i]);
                if (resolved == item)
                    return i;
            }
            return -1;
        }

        public void Insert(int index, TItemType item)
        {
            if (Contains(item))
                return;

            string guid = GetAssetGUID(item);
            if (string.IsNullOrEmpty(guid))
                return;

            itemReferences.Insert(index, new AssetReference(guid));
            isDirty = true;
        }

        public void RemoveAt(int index)
        {
            if (index < 0 || index >= itemReferences.Count)
                return;

            TItemType resolved = ResolveReference(itemReferences[index]);
            itemReferences.RemoveAt(index);
            isDirty = true;
            OnChangedEvent?.Invoke();
            if (resolved != null)
                OnItemTypeRemovedEvent?.Invoke(resolved);
        }

        public TItemType this[int index]
        {
            get => ResolveReference(itemReferences[index]);
            set
            {
                string guid = GetAssetGUID(value);
                if (!string.IsNullOrEmpty(guid))
                {
                    itemReferences[index] = new AssetReference(guid);
                    isDirty = true;
                }
            }
        }

        #endregion

        private static string GetAssetGUID(TItemType item)
        {
#if UNITY_EDITOR
            string path = UnityEditor.AssetDatabase.GetAssetPath(item);
            return UnityEditor.AssetDatabase.AssetPathToGUID(path);
#else
            return null;
#endif
        }

        public bool Equals(IList<TItemType> other)
        {
            if (other == null)
                return false;

            if (other.Count != Count)
                return false;

            for (int i = 0; i < other.Count; i++)
            {
                if (!Contains(other[i]))
                    return false;
            }

            return true;
        }

        public bool Equals(CollectionItemPicker<TItemType> other)
        {
            if (other == null)
                return false;

            if (other.Count != Count)
                return false;

            for (int i = 0; i < other.Count; i++)
            {
                if (!Contains(other[i]))
                    return false;
            }

            return true;
        }
    }
}
