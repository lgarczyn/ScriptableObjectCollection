using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections.Tests
{
    /// <summary>
    /// Tests for the SOCAddressablePostprocessor cache and FindCollectionForItemPath.
    /// </summary>
    [TestFixture]
    public class SOCAddressablePostprocessorTests
    {
        private const string TestFolder = "Assets/SOCPostprocTestTemp";
        private const string ItemsFolder = "Assets/SOCPostprocTestTemp/Items";
        private TestCollection collection;

        [SetUp]
        public void SetUp()
        {
            SOCAddressablePostprocessor.InvalidateCache();

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
            SOCAddressablePostprocessor.InvalidateCache();

            if (AssetDatabase.IsValidFolder(TestFolder))
                AssetDatabase.DeleteAsset(TestFolder);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        // --- FindCollectionForItemPath with cache ---

        [Test]
        public void FindCollectionForItemPath_FindsCollectionViaCachedLookup()
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
            AssetDatabaseUtils.CreatePathIfDoesntExist($"{TestFolder}/Orphan");
            var item = ScriptableObject.CreateInstance<TestItem>();
            AssetDatabase.CreateAsset(item, $"{TestFolder}/Orphan/NoParent.asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Orphan folder has no collection — even though TestFolder does,
            // we check that the item is in a subfolder of the collection
            // Actually, since TestFolder has a collection, walking up will find it.
            // Use a completely separate folder instead:
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

        // --- Cache invalidation ---

        [Test]
        public void InvalidateCache_ForcesRebuildOnNextLookup()
        {
            // Prime cache
            SOCAddressablePostprocessor.FindCollectionForItemPath($"{ItemsFolder}/Anything.asset");

            // Invalidate
            SOCAddressablePostprocessor.InvalidateCache();

            // Create a new collection in a different folder
            AssetDatabaseUtils.CreatePathIfDoesntExist("Assets/SOCNewCollTemp");
            AssetDatabaseUtils.CreatePathIfDoesntExist("Assets/SOCNewCollTemp/Items");
            var newCollection = ScriptableObject.CreateInstance<TestCollection>();

            AssetDatabase.CreateAsset(newCollection, "Assets/SOCNewCollTemp/NewCollection.asset");
            var newItem = ScriptableObject.CreateInstance<TestItem>();
            AssetDatabase.CreateAsset(newItem, "Assets/SOCNewCollTemp/Items/NewItem.asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // After invalidation and new asset creation, should find the new collection
            var found = SOCAddressablePostprocessor.FindCollectionForItemPath("Assets/SOCNewCollTemp/Items/NewItem.asset");
            Assert.IsNotNull(found);
            Assert.AreEqual(newCollection, found);

            AssetDatabase.DeleteAsset("Assets/SOCNewCollTemp");
        }

        // --- Cache consistency ---

        [Test]
        public void FindCollectionForItemPath_ConsistentAcrossMultipleCalls()
        {
            var item = ScriptableObject.CreateInstance<TestItem>();
            AssetDatabase.CreateAsset(item, $"{ItemsFolder}/Consistent.asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var found1 = SOCAddressablePostprocessor.FindCollectionForItemPath($"{ItemsFolder}/Consistent.asset");
            var found2 = SOCAddressablePostprocessor.FindCollectionForItemPath($"{ItemsFolder}/Consistent.asset");

            Assert.IsNotNull(found1);
            Assert.AreEqual(found1, found2);
        }

        [Test]
        public void FindCollectionForItemPath_FindsCollectionInGrandparentFolder()
        {
            // Item nested two levels deep under the collection folder
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
