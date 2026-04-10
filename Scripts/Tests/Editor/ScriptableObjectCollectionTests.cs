using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

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

        [Test]
        public void AddressableLabel_ContainsBase64Guid()
        {
            string label = collection.AddressableLabel;
            Assert.IsTrue(label.StartsWith("soc_"));
            Assert.IsTrue(label.Length > 4); // "soc_" + base64 content
        }

        [Test]
        public void AddressableLabel_IsDeterministic()
        {
            string label1 = collection.AddressableLabel;
            string label2 = collection.AddressableLabel;
            Assert.AreEqual(label1, label2);
        }

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

        [Test]
        public void SetEditorItems_PopulatesItems()
        {
            var item1 = ScriptableObject.CreateInstance<TestItem>();
            item1.name = "Item1";
            var item2 = ScriptableObject.CreateInstance<TestItem>();
            item2.name = "Item2";

            collection.SetEditorItems(new List<ScriptableObject> { item1, item2 });

            Assert.AreEqual(2, collection.Count);
            Assert.AreEqual(item1, collection[0]);
            Assert.AreEqual(item2, collection[1]);

            Object.DestroyImmediate(item1);
            Object.DestroyImmediate(item2);
        }

        [Test]
        public void TryGetItemByGUID_FindsItem()
        {
            var item = ScriptableObject.CreateInstance<TestItem>();
            item.GenerateNewGUID();
            LongGuid itemGuid = item.GUID;

            collection.SetEditorItems(new List<ScriptableObject> { item });

            bool found = collection.TryGetItemByGUID(itemGuid, out ScriptableObject result);
            Assert.IsTrue(found);
            Assert.AreEqual(item, result);

            Object.DestroyImmediate(item);
        }

        [Test]
        public void TryGetItemByGUID_ReturnsFalseForMissing()
        {
            collection.SetEditorItems(new List<ScriptableObject>());

            bool found = collection.TryGetItemByGUID(LongGuid.NewGuid(), out ScriptableObject result);
            Assert.IsFalse(found);
            Assert.IsNull(result);
        }

        [Test]
        public void TryGetItemByGUID_ReturnsFalseForInvalidGuid()
        {
            var item = ScriptableObject.CreateInstance<TestItem>();
            item.GenerateNewGUID();
            collection.SetEditorItems(new List<ScriptableObject> { item });

            bool found = collection.TryGetItemByGUID(default, out ScriptableObject result);
            Assert.IsFalse(found);

            Object.DestroyImmediate(item);
        }

        [Test]
        public void TryGetItemByName_FindsItem()
        {
            var item = ScriptableObject.CreateInstance<TestItem>();
            item.name = "Sword";

            collection.SetEditorItems(new List<ScriptableObject> { item });

            bool found = collection.TryGetItemByName("Sword", out ScriptableObject result);
            Assert.IsTrue(found);
            Assert.AreEqual(item, result);

            Object.DestroyImmediate(item);
        }

        [Test]
        public void TryGetItemByName_ReturnsFalseForMissing()
        {
            collection.SetEditorItems(new List<ScriptableObject>());

            bool found = collection.TryGetItemByName("Nonexistent", out ScriptableObject result);
            Assert.IsFalse(found);
            Assert.IsNull(result);
        }

        [Test]
        public void TryGetItemByName_IsCaseSensitive()
        {
            var item = ScriptableObject.CreateInstance<TestItem>();
            item.name = "Sword";

            collection.SetEditorItems(new List<ScriptableObject> { item });

            bool found = collection.TryGetItemByName("sword", out ScriptableObject _);
            Assert.IsFalse(found);

            Object.DestroyImmediate(item);
        }

        [Test]
        public void GenericTryGetItemByGUID_ReturnsTypedResult()
        {
            var item = ScriptableObject.CreateInstance<TestItem>();
            item.GenerateNewGUID();
            LongGuid itemGuid = item.GUID;

            collection.SetEditorItems(new List<ScriptableObject> { item });

            bool found = collection.TryGetItemByGUID<TestItem>(itemGuid, out TestItem result);
            Assert.IsTrue(found);
            Assert.AreEqual(item, result);

            Object.DestroyImmediate(item);
        }

        [Test]
        public void Count_ReflectsEditorItems()
        {
            var item = ScriptableObject.CreateInstance<TestItem>();
            collection.SetEditorItems(new List<ScriptableObject> { item });
            Assert.AreEqual(1, collection.Count);

            collection.SetEditorItems(new List<ScriptableObject>());
            Assert.AreEqual(0, collection.Count);

            Object.DestroyImmediate(item);
        }

        [Test]
        public void Indexer_ReturnsCorrectItem()
        {
            var item0 = ScriptableObject.CreateInstance<TestItem>();
            item0.name = "A";
            var item1 = ScriptableObject.CreateInstance<TestItem>();
            item1.name = "B";

            collection.SetEditorItems(new List<ScriptableObject> { item0, item1 });

            Assert.AreEqual("A", collection[0].name);
            Assert.AreEqual("B", collection[1].name);

            Object.DestroyImmediate(item0);
            Object.DestroyImmediate(item1);
        }
    }
}
