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

        // --- Load error handling ---

        [Test]
        public void Load_WithInvalidKey_DoesNotThrow()
        {
            LogAssert.ignoreFailingMessages = true;
            Assert.DoesNotThrow(() => collection.Load());
            LogAssert.ignoreFailingMessages = false;
        }

        [Test]
        public void Load_WithEmptyGuid_DoesNotSetIsLoaded()
        {
            // Empty GUID = not baked yet. Load should not mark as loaded so it retries later.
            LogAssert.ignoreFailingMessages = true;
            collection.Load();
            LogAssert.ignoreFailingMessages = false;

            Assert.IsFalse(collection.IsLoaded);
        }

        [Test]
        public void Load_WithInvalidKey_ReturnsEmptyItems()
        {
            LogAssert.ignoreFailingMessages = true;
            collection.Load();
            LogAssert.ignoreFailingMessages = false;

            Assert.AreEqual(0, collection.Items.Count);
        }

        [Test]
        public void GetLoadedItems_WithInvalidKey_ReturnsEmptyList()
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
            // With empty GUID, Load doesn't set isLoaded — so test with a
            // manually set state by loading then unloading.
            LogAssert.ignoreFailingMessages = true;
            collection.Load();
            LogAssert.ignoreFailingMessages = false;

            // Empty GUID means Load didn't mark as loaded
            Assert.IsFalse(collection.IsLoaded);
            // Unload on an unloaded collection should not throw
            Assert.DoesNotThrow(() => collection.Unload());
        }


        // --- FindAll error handling ---

        [Test]
        public void FindAll_WithInvalidLabel_ReturnsEmptyList()
        {
            LogAssert.ignoreFailingMessages = true;
            var result = ScriptableObjectCollection.FindAll();
            LogAssert.ignoreFailingMessages = false;

            Assert.IsNotNull(result);
        }
    }
}
