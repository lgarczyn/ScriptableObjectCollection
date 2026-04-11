using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BrunoMikoski.ScriptableObjectCollections.Tests
{
    [TestFixture]
    public class ScriptableObjectRegistryTests
    {
        [Test]
        public void RegisteredLabel_HasExpectedValue()
        {
            Assert.AreEqual("soc_registered", ScriptableObjectRegistry.RegisteredLabel);
        }

        [Test]
        public void Load_WithNullGuid_ReturnsNull()
        {
            Assert.IsNull(ScriptableObjectRegistry.Load<TestRegisteredItem>(null));
        }

        [Test]
        public void Load_WithEmptyGuid_ReturnsNull()
        {
            Assert.IsNull(ScriptableObjectRegistry.Load<TestRegisteredItem>(""));
        }

        [Test]
        public void Load_WithInvalidGuid_ReturnsNull()
        {
            LogAssert.ignoreFailingMessages = true;
            var result = ScriptableObjectRegistry.Load<TestRegisteredItem>("nonexistent-guid");
            LogAssert.ignoreFailingMessages = false;

            Assert.IsNull(result);
        }

        [Test]
        public void TryGetItemByGUID_WithNullGuid_ReturnsFalse()
        {
            bool found = ScriptableObjectRegistry.TryGetItemByGUID<TestRegisteredItem>(null, out _);
            Assert.IsFalse(found);
        }

        [Test]
        public void TryGetItemByGUID_WithEmptyGuid_ReturnsFalse()
        {
            bool found = ScriptableObjectRegistry.TryGetItemByGUID<TestRegisteredItem>("", out _);
            Assert.IsFalse(found);
        }

        [Test]
        public void TryGetItemByGUID_WithInvalidGuid_ReturnsFalse()
        {
            LogAssert.ignoreFailingMessages = true;
            bool found = ScriptableObjectRegistry.TryGetItemByGUID<TestRegisteredItem>("nonexistent", out _);
            LogAssert.ignoreFailingMessages = false;

            Assert.IsFalse(found);
        }
    }
}
