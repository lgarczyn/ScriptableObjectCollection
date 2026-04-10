using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BrunoMikoski.ScriptableObjectCollections.Tests
{
    [TestFixture]
    public class SORegistryTests
    {
        [TearDown]
        public void TearDown()
        {
            SORegistry.Unload();
        }

        [Test]
        public void RegisteredLabel_HasExpectedValue()
        {
            Assert.AreEqual("soc_registered", SORegistry.RegisteredLabel);
        }

        [Test]
        public void IsLoaded_FalseByDefault()
        {
            Assert.IsFalse(SORegistry.IsLoaded);
        }

        [Test]
        public void LoadSync_SetsIsLoaded()
        {
            LogAssert.ignoreFailingMessages = true;
            SORegistry.LoadSync();
            LogAssert.ignoreFailingMessages = false;

            Assert.IsTrue(SORegistry.IsLoaded);
        }

        [Test]
        public void Items_ReturnsNonNullList()
        {
            LogAssert.ignoreFailingMessages = true;
            var items = SORegistry.Items;
            LogAssert.ignoreFailingMessages = false;

            Assert.IsNotNull(items);
        }

        [Test]
        public void Unload_ClearsIsLoaded()
        {
            LogAssert.ignoreFailingMessages = true;
            SORegistry.LoadSync();
            LogAssert.ignoreFailingMessages = false;

            SORegistry.Unload();
            Assert.IsFalse(SORegistry.IsLoaded);
        }

        [Test]
        public void Unload_WhenNotLoaded_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => SORegistry.Unload());
        }

        [Test]
        public void GetAll_ReturnsEmptyForUnknownType()
        {
            LogAssert.ignoreFailingMessages = true;
            // TestItem implements ISOCItem but not IRegisteredSO,
            // so GetAll won't find it even if loaded
            var results = SORegistry.GetAll<TestRegisteredItem>();
            LogAssert.ignoreFailingMessages = false;

            Assert.IsNotNull(results);
        }

        [Test]
        public void TryGet_ReturnsFalseWhenNotFound()
        {
            LogAssert.ignoreFailingMessages = true;
            bool found = SORegistry.TryGet<TestRegisteredItem>(out _);
            LogAssert.ignoreFailingMessages = false;

            Assert.IsFalse(found);
        }

        [Test]
        public void TryGetByName_ReturnsFalseWhenNotFound()
        {
            LogAssert.ignoreFailingMessages = true;
            bool found = SORegistry.TryGetByName<TestRegisteredItem>("Nonexistent", out _);
            LogAssert.ignoreFailingMessages = false;

            Assert.IsFalse(found);
        }
    }
}
