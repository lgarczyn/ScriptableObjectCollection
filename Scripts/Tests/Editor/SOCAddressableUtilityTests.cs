using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections.Tests
{
    /// <summary>
    /// Tests for SOCAddressableUtility helper methods that don't require
    /// a full Addressable settings setup.
    /// </summary>
    [TestFixture]
    public class SOCAddressableUtilityTests
    {
        private const string TestFolder = "Assets/SOCAddrTestTemp";

        [SetUp]
        public void SetUp()
        {
            AssetDatabaseUtils.CreatePathIfDoesntExist(TestFolder);
        }

        [TearDown]
        public void TearDown()
        {
            if (AssetDatabase.IsValidFolder(TestFolder))
                AssetDatabase.DeleteAsset(TestFolder);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [Test]
        public void GetCollectionAddress_ContainsGuidBase64()
        {
            var collection = ScriptableObject.CreateInstance<TestCollection>();
            collection.GenerateNewGUID();
            AssetDatabase.CreateAsset(collection, $"{TestFolder}/AddrCollection.asset");

            string address = SOCAddressableUtility.GetCollectionAddress(collection);

            Assert.IsTrue(address.StartsWith("soc_collection_"));
            Assert.IsTrue(address.Length > "soc_collection_".Length);
        }

        [Test]
        public void GetCollectionAddress_IsDeterministic()
        {
            var collection = ScriptableObject.CreateInstance<TestCollection>();
            collection.GenerateNewGUID();
            AssetDatabase.CreateAsset(collection, $"{TestFolder}/DetCollection.asset");

            string addr1 = SOCAddressableUtility.GetCollectionAddress(collection);
            string addr2 = SOCAddressableUtility.GetCollectionAddress(collection);
            Assert.AreEqual(addr1, addr2);
        }

        [Test]
        public void FindCollectionForItemPath_ReturnsNullWhenNoCollection()
        {
            // Create a plain SO in a folder with no collection
            AssetDatabaseUtils.CreatePathIfDoesntExist($"{TestFolder}/NoCollection");
            var item = ScriptableObject.CreateInstance<TestItem>();
            AssetDatabase.CreateAsset(item, $"{TestFolder}/NoCollection/OrphanItem.asset");
            AssetDatabase.SaveAssets();

            var found = SOCAddressableUtility.FindCollectionForItemPath($"{TestFolder}/NoCollection/OrphanItem.asset");
            Assert.IsNull(found);
        }

        [Test]
        public void FindCollectionForItemPath_FindsCollectionInParentFolder()
        {
            // Create collection in TestFolder
            var collection = ScriptableObject.CreateInstance<TestCollection>();
            collection.GenerateNewGUID();
            AssetDatabase.CreateAsset(collection, $"{TestFolder}/ParentCollection.asset");

            // Create item in Items subfolder
            AssetDatabaseUtils.CreatePathIfDoesntExist($"{TestFolder}/Items");
            var item = ScriptableObject.CreateInstance<TestItem>();
            AssetDatabase.CreateAsset(item, $"{TestFolder}/Items/ChildItem.asset");
            AssetDatabase.SaveAssets();

            var found = SOCAddressableUtility.FindCollectionForItemPath($"{TestFolder}/Items/ChildItem.asset");
            Assert.IsNotNull(found);
            Assert.AreEqual(collection.GUID, found.GUID);
        }
    }
}
