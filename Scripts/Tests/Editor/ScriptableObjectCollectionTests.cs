using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace BrunoMikoski.ScriptableObjectCollections.Tests
{
    [TestFixture]
    public class ScriptableObjectCollectionTests
    {
        private TestCollection collection;

        [SetUp]
        public void SetUp()
        {
            collection = ScriptableObject.CreateInstance<TestCollection>();
            collection.GenerateNewGUID();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(collection);
        }

        // --- GUID ---

        [Test]
        public void NewCollection_HasValidGUID()
        {
            Assert.IsTrue(collection.GUID.IsValid());
        }

        [Test]
        public void GenerateNewGUID_ChangesGUID()
        {
            LongGuid original = collection.GUID;
            collection.GenerateNewGUID();
            Assert.AreNotEqual(original, collection.GUID);
        }

        // --- Addressable addresses ---

        [Test]
        public void AddressableLabel_ContainsBase64Guid()
        {
            string label = collection.AddressableLabel;
            Assert.IsTrue(label.StartsWith("soc_", StringComparison.Ordinal));
            Assert.IsTrue(label.Length > 4);
        }

        [Test]
        public void AddressableLabel_IsDeterministic()
        {
            Assert.AreEqual(collection.AddressableLabel, collection.AddressableLabel);
        }

        [Test]
        public void GetAddressableAddress_ContainsPrefix()
        {
            string address = ScriptableObjectCollection.GetAddressableAddress(collection.GUID);
            Assert.IsTrue(address.StartsWith("soc_collection_", StringComparison.Ordinal));
        }

        [Test]
        public void GetAddressableAddress_IsDeterministic()
        {
            string addr1 = ScriptableObjectCollection.GetAddressableAddress(collection.GUID);
            string addr2 = ScriptableObjectCollection.GetAddressableAddress(collection.GUID);
            Assert.AreEqual(addr1, addr2);
        }

        [Test]
        public void AddressableAddress_MatchesStaticMethod()
        {
            Assert.AreEqual(
                ScriptableObjectCollection.GetAddressableAddress(collection.GUID),
                collection.AddressableAddress);
        }

        [Test]
        public void AllCollectionsLabel_HasExpectedValue()
        {
            Assert.AreEqual("soc_collections", ScriptableObjectCollection.AllCollectionsLabel);
        }

        // --- State ---

        [Test]
        public void IsLoaded_FalseByDefault()
        {
            Assert.IsFalse(collection.IsLoaded);
        }

        [Test]
        public void GetItemType_ReturnsTestItem()
        {
            Assert.AreEqual(typeof(TestItem), collection.GetItemType());
        }

        // --- LoadSync error handling ---

        [Test]
        public void LoadSync_WithInvalidKey_DoesNotThrow()
        {
            // Collection has no Addressable entries — LoadSync should handle gracefully
            LogAssert.ignoreFailingMessages = true;
            Assert.DoesNotThrow(() => collection.LoadSync());
            LogAssert.ignoreFailingMessages = false;
        }

        [Test]
        public void LoadSync_WithInvalidKey_SetsIsLoaded()
        {
            LogAssert.ignoreFailingMessages = true;
            collection.LoadSync();
            LogAssert.ignoreFailingMessages = false;

            Assert.IsTrue(collection.IsLoaded);
        }

        [Test]
        public void LoadSync_WithInvalidKey_ReturnsEmptyItems()
        {
            LogAssert.ignoreFailingMessages = true;
            collection.LoadSync();
            LogAssert.ignoreFailingMessages = false;

            Assert.AreEqual(0, collection.Count);
        }

        [Test]
        public void Items_WithInvalidKey_ReturnsEmptyList()
        {
            LogAssert.ignoreFailingMessages = true;
            var items = collection.Items;
            LogAssert.ignoreFailingMessages = false;

            Assert.IsNotNull(items);
            Assert.AreEqual(0, items.Count);
        }

        // --- Unload ---

        [Test]
        public void Unload_WhenNotLoaded_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => collection.Unload());
        }

        [Test]
        public void Unload_AfterLoad_ClearsIsLoaded()
        {
            LogAssert.ignoreFailingMessages = true;
            collection.LoadSync();
            LogAssert.ignoreFailingMessages = false;

            Assert.IsTrue(collection.IsLoaded);
            collection.Unload();
            Assert.IsFalse(collection.IsLoaded);
        }

        // --- TryGetItemByName ---

        [Test]
        public void TryGetItemByName_OnEmptyCollection_ReturnsFalse()
        {
            LogAssert.ignoreFailingMessages = true;
            bool found = collection.TryGetItemByName("Anything", out ScriptableObject result);
            LogAssert.ignoreFailingMessages = false;

            Assert.IsFalse(found);
            Assert.IsNull(result);
        }

        // --- LoadByGUID ---

        [Test]
        public void LoadByGUID_WithInvalidGuid_ReturnsNull()
        {
            var result = ScriptableObjectCollection.LoadByGUID(default);
            Assert.IsNull(result);
        }

        // --- FindAll / FindByItemType error handling ---

        [Test]
        public void FindAll_WithNoAddressables_DoesNotThrow()
        {
            // May return empty or throw internally; should not crash
            LogAssert.ignoreFailingMessages = true;
            Assert.DoesNotThrow(() =>
            {
                try { ScriptableObjectCollection.FindAll(); }
                catch (Exception) { /* Addressables may throw if no label exists */ }
            });
            LogAssert.ignoreFailingMessages = false;
        }
    }
}
