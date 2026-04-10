using System;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections
{
    /// <summary>
    /// Lightweight serializable descriptor for a collection.
    /// Stored in the CollectionsRegistry so the registry does not hold
    /// direct references to collection assets (letting them stay unloaded).
    /// </summary>
    [Serializable]
    public struct CollectionMetadata
    {
        [SerializeField]
        private LongGuid guid;
        public LongGuid GUID => guid;

        [SerializeField]
        private string collectionTypeName;
        public string CollectionTypeName => collectionTypeName;

        [SerializeField]
        private string itemTypeName;
        public string ItemTypeName => itemTypeName;

        /// <summary>
        /// Addressable address used to load the collection asset itself.
        /// </summary>
        [SerializeField]
        private string addressableAddress;
        public string AddressableAddress => addressableAddress;

        /// <summary>
        /// Addressable label applied to all items belonging to this collection.
        /// </summary>
        [SerializeField]
        private string itemLabel;
        public string ItemLabel => itemLabel;

        public CollectionMetadata(
            LongGuid guid,
            string collectionTypeName,
            string itemTypeName,
            string addressableAddress,
            string itemLabel)
        {
            this.guid = guid;
            this.collectionTypeName = collectionTypeName;
            this.itemTypeName = itemTypeName;
            this.addressableAddress = addressableAddress;
            this.itemLabel = itemLabel;
        }
    }
}
