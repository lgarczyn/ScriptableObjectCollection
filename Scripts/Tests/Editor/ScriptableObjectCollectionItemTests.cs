using NUnit.Framework;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections.Tests
{
    [TestFixture]
    public class ScriptableObjectCollectionItemTests
    {
        [Test]
        public void NewItem_ImplementsISOCItem()
        {
            var item = ScriptableObject.CreateInstance<TestItem>();
            Assert.IsInstanceOf<ISOCItem>(item);
            Object.DestroyImmediate(item);
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
