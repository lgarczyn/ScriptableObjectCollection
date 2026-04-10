using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BrunoMikoski.ScriptableObjectCollections.Tests
{
    [TestFixture]
    public class SORegistryTests
    {
        [Test]
        public void RegisteredLabel_HasExpectedValue()
        {
            Assert.AreEqual("soc_registered", SORegistry.RegisteredLabel);
        }

        [Test]
        public void Load_WithNullGuid_ReturnsNull()
        {
            Assert.IsNull(SORegistry.Load<ScriptableObject>(null));
        }

        [Test]
        public void Load_WithEmptyGuid_ReturnsNull()
        {
            Assert.IsNull(SORegistry.Load<ScriptableObject>(""));
        }

        [Test]
        public void Load_WithInvalidGuid_ReturnsNull()
        {
            LogAssert.ignoreFailingMessages = true;
            var result = SORegistry.Load<ScriptableObject>("nonexistent-guid");
            LogAssert.ignoreFailingMessages = false;

            Assert.IsNull(result);
        }
    }
}
