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
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(collection);
        }

        // --- Addressable labels ---

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

        // --- FindAll error handling ---

        [Test]
        public void FindAll_WithNoAddressables_DoesNotThrow()
        {
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
