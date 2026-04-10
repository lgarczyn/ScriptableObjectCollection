using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections.Tests
{
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
        public void FindCollectionForItemPath_ReturnsNullWhenNoCollection()
        {
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
            var collection = ScriptableObject.CreateInstance<TestCollection>();
            AssetDatabase.CreateAsset(collection, $"{TestFolder}/ParentCollection.asset");
            AssetDatabase.SaveAssets();

            AssetDatabaseUtils.CreatePathIfDoesntExist($"{TestFolder}/Items");
            var item = ScriptableObject.CreateInstance<TestItem>();
            AssetDatabase.CreateAsset(item, $"{TestFolder}/Items/ChildItem.asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var found = SOCAddressableUtility.FindCollectionForItemPath($"{TestFolder}/Items/ChildItem.asset");
            Assert.IsNotNull(found);
            Assert.AreEqual(collection, found);
        }
    }
}
