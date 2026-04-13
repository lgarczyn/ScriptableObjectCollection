using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections.Tests
{
    [TestFixture]
    public class SOCAddressablePostprocessorTests
    {
        private const string TestFolder = "Assets/SOCPostprocTestTemp";
        private const string ItemsFolder = "Assets/SOCPostprocTestTemp/Items";
        private TestCollection collection;

        [SetUp]
        public void SetUp()
        {
            AssetDatabaseUtils.CreatePathIfDoesntExist(TestFolder);
            AssetDatabaseUtils.CreatePathIfDoesntExist(ItemsFolder);

            collection = ScriptableObject.CreateInstance<TestCollection>();
            AssetDatabase.CreateAsset(collection, $"{TestFolder}/PostprocCollection.asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
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
        public void FindCollectionForItemPath_FindsCollection()
        {
            var item = ScriptableObject.CreateInstance<TestItem>();
            AssetDatabase.CreateAsset(item, $"{ItemsFolder}/CachedItem.asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var found = SOCAddressablePostprocessor.FindCollectionForItemPath($"{ItemsFolder}/CachedItem.asset");
            Assert.IsNotNull(found);
            Assert.AreEqual(collection, found);
        }

        [Test]
        public void FindCollectionForItemPath_ReturnsNullForOrphanItem()
        {
            AssetDatabaseUtils.CreatePathIfDoesntExist("Assets/SOCOrphanTemp");
            var orphan = ScriptableObject.CreateInstance<TestItem>();
            AssetDatabase.CreateAsset(orphan, "Assets/SOCOrphanTemp/Orphan.asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var found = SOCAddressablePostprocessor.FindCollectionForItemPath("Assets/SOCOrphanTemp/Orphan.asset");
            Assert.IsNull(found);

            AssetDatabase.DeleteAsset("Assets/SOCOrphanTemp");
        }

        [Test]
        public void FindCollectionForItemPath_ReturnsNullForEmptyPath()
        {
            var found = SOCAddressablePostprocessor.FindCollectionForItemPath("");
            Assert.IsNull(found);
        }

        [Test]
        public void FindCollectionForItemPath_FindsCollectionInGrandparentFolder()
        {
            AssetDatabaseUtils.CreatePathIfDoesntExist($"{ItemsFolder}/SubFolder");
            var item = ScriptableObject.CreateInstance<TestItem>();
            AssetDatabase.CreateAsset(item, $"{ItemsFolder}/SubFolder/Deep.asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var found = SOCAddressablePostprocessor.FindCollectionForItemPath($"{ItemsFolder}/SubFolder/Deep.asset");
            Assert.IsNotNull(found);
            Assert.AreEqual(collection, found);
        }
    }
}
