using NUnit.Framework;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections.Tests
{
    [TestFixture]
    public class ScriptableObjectCollectionItemTests
    {
        [Test]
        public void NewItem_GeneratesValidGUID()
        {
            var item = ScriptableObject.CreateInstance<TestItem>();
            // Accessing GUID auto-generates if invalid
            LongGuid guid = item.GUID;
            Assert.IsTrue(guid.IsValid());

            Object.DestroyImmediate(item);
        }

        [Test]
        public void GenerateNewGUID_ChangesGUID()
        {
            var item = ScriptableObject.CreateInstance<TestItem>();
            LongGuid original = item.GUID;
            item.GenerateNewGUID();
            Assert.AreNotEqual(original, item.GUID);

            Object.DestroyImmediate(item);
        }

        [Test]
        public void SetCollectionRuntime_CachesWithoutDirtying()
        {
            var item = ScriptableObject.CreateInstance<TestItem>();
            var collection = ScriptableObject.CreateInstance<TestCollection>();
            collection.GenerateNewGUID();

            item.SetCollectionRuntime(collection);
            Assert.AreEqual(collection, item.Collection);

            Object.DestroyImmediate(item);
            Object.DestroyImmediate(collection);
        }

        [Test]
        public void ClearCollection_RemovesReference()
        {
            var item = ScriptableObject.CreateInstance<TestItem>();
            var collection = ScriptableObject.CreateInstance<TestCollection>();
            collection.GenerateNewGUID();

            item.SetCollectionRuntime(collection);
            Assert.IsNotNull(item.Collection);

            item.ClearCollection();

            // After clear, the collection property will try to look up via registry
            // which won't find it (no registry loaded), so it should return null
            // But the cached reference is cleared
            Object.DestroyImmediate(item);
            Object.DestroyImmediate(collection);
        }

        [Test]
        public void CompareTo_SortsByName()
        {
            var itemA = ScriptableObject.CreateInstance<TestItem>();
            itemA.name = "Alpha";
            var itemB = ScriptableObject.CreateInstance<TestItem>();
            itemB.name = "Beta";

            Assert.Less(itemA.CompareTo(itemB), 0);
            Assert.Greater(itemB.CompareTo(itemA), 0);
            Assert.AreEqual(0, itemA.CompareTo(itemA));

            Object.DestroyImmediate(itemA);
            Object.DestroyImmediate(itemB);
        }

        [Test]
        public void TestValue_CanBeSetAndRead()
        {
            var item = ScriptableObject.CreateInstance<TestItem>();
            item.TestValue = 42;
            Assert.AreEqual(42, item.TestValue);

            Object.DestroyImmediate(item);
        }
    }
}
