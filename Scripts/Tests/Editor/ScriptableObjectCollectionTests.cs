using System;
using NUnit.Framework;
using UnityEngine;
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
            Assert.IsTrue(label.StartsWith("soc_", StringComparison.Ordinal));
            Assert.IsTrue(label.Length > 4);
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
    }
}
