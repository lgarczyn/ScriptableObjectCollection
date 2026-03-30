using NUnit.Framework;

namespace BrunoMikoski.ScriptableObjectCollections.Tests
{
    [TestFixture]
    public class CollectionMetadataTests
    {
        [Test]
        public void Constructor_SetsAllFields()
        {
            LongGuid guid = LongGuid.NewGuid();
            var meta = new CollectionMetadata(
                guid,
                "TestCollectionType",
                "TestItemType",
                "soc_collection_abc",
                "soc_xyz"
            );

            Assert.AreEqual(guid, meta.GUID);
            Assert.AreEqual("TestCollectionType", meta.CollectionTypeName);
            Assert.AreEqual("TestItemType", meta.ItemTypeName);
            Assert.AreEqual("soc_collection_abc", meta.AddressableAddress);
            Assert.AreEqual("soc_xyz", meta.ItemLabel);
        }

        [Test]
        public void Default_HasInvalidGuid()
        {
            CollectionMetadata meta = default;
            Assert.IsFalse(meta.GUID.IsValid());
            Assert.IsNull(meta.AddressableAddress);
        }
    }
}
