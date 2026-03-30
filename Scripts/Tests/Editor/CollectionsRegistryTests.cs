using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections.Tests
{
    [TestFixture]
    public class CollectionsRegistryTests
    {
        private CollectionsRegistry registry;

        [SetUp]
        public void SetUp()
        {
            registry = ScriptableObject.CreateInstance<CollectionsRegistry>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(registry);
        }

        [Test]
        public void NewRegistry_HasNoEntries()
        {
            Assert.AreEqual(0, registry.Entries.Count);
        }

        [Test]
        public void AddOrUpdateEntry_AddsNewEntry()
        {
            LongGuid guid = LongGuid.NewGuid();
            var meta = new CollectionMetadata(guid, "Type", "ItemType", "addr", "label");

            registry.AddOrUpdateEntry(meta);

            Assert.AreEqual(1, registry.Entries.Count);
            Assert.AreEqual(guid, registry.Entries[0].GUID);
        }

        [Test]
        public void AddOrUpdateEntry_UpdatesExistingEntry()
        {
            LongGuid guid = LongGuid.NewGuid();
            var meta1 = new CollectionMetadata(guid, "TypeV1", "ItemType", "addr1", "label1");
            var meta2 = new CollectionMetadata(guid, "TypeV2", "ItemType", "addr2", "label2");

            registry.AddOrUpdateEntry(meta1);
            registry.AddOrUpdateEntry(meta2);

            Assert.AreEqual(1, registry.Entries.Count);
            Assert.AreEqual("TypeV2", registry.Entries[0].CollectionTypeName);
            Assert.AreEqual("addr2", registry.Entries[0].AddressableAddress);
        }

        [Test]
        public void RemoveEntry_RemovesCorrectEntry()
        {
            LongGuid guid1 = LongGuid.NewGuid();
            LongGuid guid2 = LongGuid.NewGuid();

            registry.AddOrUpdateEntry(new CollectionMetadata(guid1, "T1", "I1", "a1", "l1"));
            registry.AddOrUpdateEntry(new CollectionMetadata(guid2, "T2", "I2", "a2", "l2"));

            Assert.AreEqual(2, registry.Entries.Count);

            registry.RemoveEntry(guid1);

            Assert.AreEqual(1, registry.Entries.Count);
            Assert.AreEqual(guid2, registry.Entries[0].GUID);
        }

        [Test]
        public void RemoveEntry_NoOpForMissingGuid()
        {
            LongGuid guid = LongGuid.NewGuid();
            registry.AddOrUpdateEntry(new CollectionMetadata(guid, "T", "I", "a", "l"));

            registry.RemoveEntry(LongGuid.NewGuid()); // Different GUID

            Assert.AreEqual(1, registry.Entries.Count);
        }

        [Test]
        public void SetEntries_ReplacesAll()
        {
            registry.AddOrUpdateEntry(new CollectionMetadata(LongGuid.NewGuid(), "Old", "I", "a", "l"));

            var newEntries = new List<CollectionMetadata>
            {
                new CollectionMetadata(LongGuid.NewGuid(), "New1", "I", "a1", "l1"),
                new CollectionMetadata(LongGuid.NewGuid(), "New2", "I", "a2", "l2"),
            };

            registry.SetEntries(newEntries);

            Assert.AreEqual(2, registry.Entries.Count);
            Assert.AreEqual("New1", registry.Entries[0].CollectionTypeName);
            Assert.AreEqual("New2", registry.Entries[1].CollectionTypeName);
        }

        [Test]
        public void RegistryAddress_IsExpectedConstant()
        {
            Assert.AreEqual("SOC_Registry", CollectionsRegistry.RegistryAddress);
        }

        [Test]
        public void GetOrLoadCollection_ReturnsNullForUnknownGuid()
        {
            // No entries, so any GUID should fail
            var result = registry.GetOrLoadCollection(LongGuid.NewGuid());
            Assert.IsNull(result);
        }

        [Test]
        public void TryGetCollectionByGUID_ReturnsFalseForInvalidGuid()
        {
            bool found = registry.TryGetCollectionByGUID(default, out ScriptableObjectCollection result);
            Assert.IsFalse(found);
            Assert.IsNull(result);
        }
    }
}
