using NUnit.Framework;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections.Tests
{
    [TestFixture]
    public class CollectionItemIndirectReferenceTests
    {
        [Test]
        public void Default_IsNotValid()
        {
            var reference = new CollectionItemIndirectReference<TestItem>();
            Assert.IsFalse(reference.IsValid());
        }

        [Test]
        public void FromItem_IsValid()
        {
            var item = ScriptableObject.CreateInstance<TestItem>();
            item.GenerateNewGUID();

            var collection = ScriptableObject.CreateInstance<TestCollection>();
            collection.GenerateNewGUID();
            item.SetCollectionRuntime(collection);

            var reference = new CollectionItemIndirectReference<TestItem>(item);
            Assert.IsTrue(reference.IsValid());

            Object.DestroyImmediate(item);
            Object.DestroyImmediate(collection);
        }

        [Test]
        public void Equality_SameItem_AreEqual()
        {
            var item = ScriptableObject.CreateInstance<TestItem>();
            item.GenerateNewGUID();

            var collection = ScriptableObject.CreateInstance<TestCollection>();
            collection.GenerateNewGUID();
            item.SetCollectionRuntime(collection);

            var ref1 = new CollectionItemIndirectReference<TestItem>(item);
            var ref2 = new CollectionItemIndirectReference<TestItem>(item);

            Assert.AreEqual(ref1, ref2);

            Object.DestroyImmediate(item);
            Object.DestroyImmediate(collection);
        }

        [Test]
        public void Equality_DifferentItems_AreNotEqual()
        {
            var collection = ScriptableObject.CreateInstance<TestCollection>();
            collection.GenerateNewGUID();

            var item1 = ScriptableObject.CreateInstance<TestItem>();
            item1.GenerateNewGUID();
            item1.SetCollectionRuntime(collection);

            var item2 = ScriptableObject.CreateInstance<TestItem>();
            item2.GenerateNewGUID();
            item2.SetCollectionRuntime(collection);

            var ref1 = new CollectionItemIndirectReference<TestItem>(item1);
            var ref2 = new CollectionItemIndirectReference<TestItem>(item2);

            Assert.AreNotEqual(ref1, ref2);

            Object.DestroyImmediate(item1);
            Object.DestroyImmediate(item2);
            Object.DestroyImmediate(collection);
        }

        [Test]
        public void EqualsISOCItem_MatchesCorrectItem()
        {
            var collection = ScriptableObject.CreateInstance<TestCollection>();
            collection.GenerateNewGUID();

            var item = ScriptableObject.CreateInstance<TestItem>();
            item.GenerateNewGUID();
            item.SetCollectionRuntime(collection);

            var reference = new CollectionItemIndirectReference<TestItem>(item);

            Assert.IsTrue(reference.Equals((ISOCItem)item));

            Object.DestroyImmediate(item);
            Object.DestroyImmediate(collection);
        }
    }
}
