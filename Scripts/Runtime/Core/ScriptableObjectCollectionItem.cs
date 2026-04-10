using System;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public class ScriptableObjectCollectionItem : ScriptableObject, IComparable<ScriptableObjectCollectionItem>, ISOCItem
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

        [SerializeField, CollectionReferenceLongGuid]
        private LongGuid collectionGUID;


        [NonSerialized]
        private bool hasCachedCollection;
        [NonSerialized]
        private ScriptableObjectCollection cachedCollection;
        public ScriptableObjectCollection Collection
        {
            get
            {
                if (!hasCachedCollection)
                {
                    if (collectionGUID.IsValid())
                    {
#if UNITY_EDITOR
                        if (!Application.isPlaying)
                        {
                            ScriptableObjectCollection.TryFindByGUIDInEditor(collectionGUID, out cachedCollection);
                        }
                        else
#endif
                        {
                            cachedCollection = ScriptableObjectCollection.LoadByGUID(collectionGUID);
                        }
                    }

                    hasCachedCollection = cachedCollection != null;
                }

                return cachedCollection;
            }
        }

        public void SetCollection(ScriptableObjectCollection collection)
        {
            cachedCollection = collection;
            collectionGUID = cachedCollection.GUID;
            hasCachedCollection = true;
            ObjectUtility.SetDirty(this);
        }

        /// <summary>
        /// Runtime-only: sets the cached collection reference without dirtying the asset.
        /// Used during Addressables loading to avoid modifying assets at runtime.
        /// </summary>
        public void SetCollectionRuntime(ScriptableObjectCollection collection)
        {
            cachedCollection = collection;
            hasCachedCollection = true;
        }

        public void ClearCollection()
        {
            cachedCollection = null;
            hasCachedCollection = false;
            collectionGUID = default;
            ObjectUtility.SetDirty(this);
        }

        public void GenerateNewGUID()
        {
            guid = LongGuid.NewGuid();
            ObjectUtility.SetDirty(this);
        }

        public int CompareTo(ScriptableObjectCollectionItem other)
        {
            return string.Compare(name, other.name, StringComparison.Ordinal);
        }
    }
}
