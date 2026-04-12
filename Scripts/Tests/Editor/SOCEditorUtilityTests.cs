using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections.Tests
{
    /// <summary>
    /// Integration tests for SOCEditorUtility.
    /// Creates real assets on disk via AssetDatabase.
    /// </summary>
    [TestFixture]
    public class SOCEditorUtilityTests
    {
        private const string TestFolder = "Assets/SOCTestTemp";
        private const string ItemsFolder = "Assets/SOCTestTemp/Items";
        private TestCollection collection;

        [SetUp]
        public void SetUp()
        {
            AssetDatabaseUtils.CreatePathIfDoesntExist(TestFolder);
            AssetDatabaseUtils.CreatePathIfDoesntExist(ItemsFolder);

            collection = ScriptableObject.CreateInstance<TestCollection>();

            AssetDatabase.CreateAsset(collection, $"{TestFolder}/TestCollection.asset");
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
        public void AddNewItem_CreatesAssetInItemsSubfolder()
        {
            ScriptableObject newItem = SOCEditorUtility.AddNewItem(collection, typeof(TestItem), "MySword");

            Assert.IsNotNull(newItem);
            Assert.AreEqual("MySword", newItem.name);

            string assetPath = AssetDatabase.GetAssetPath(newItem);
            Assert.IsTrue(assetPath.Contains("Items/"), $"Expected item in Items/ folder, got: {assetPath}");
            Assert.IsTrue(newItem is ISOCItem);
        }

        [Test]
        public void AddNewItem_GeneratesUniqueName()
        {
            ScriptableObject item1 = SOCEditorUtility.AddNewItem(collection, typeof(TestItem), "Potion");
            ScriptableObject item2 = SOCEditorUtility.AddNewItem(collection, typeof(TestItem), "Potion");

            Assert.AreNotEqual(
                AssetDatabase.GetAssetPath(item1),
                AssetDatabase.GetAssetPath(item2));
        }

        [Test]
        public void RemoveItem_DeletesAsset()
        {
            var item = ScriptableObject.CreateInstance<TestItem>();
            item.name = "ToDelete";
            string assetPath = $"{ItemsFolder}/ToDelete.asset";
            AssetDatabase.CreateAsset(item, assetPath);
            AssetDatabase.SaveAssets();

            Assert.IsTrue(File.Exists(assetPath));

            collection.RemoveItem(item, deleteAsset: true);
            AssetDatabase.Refresh();

            Assert.IsNull(AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath));
        }


        [Test]
        public void TryGetItemByName_FindsExistingItem()
        {
            collection.AddNewItem(typeof(TestItem), "Sword");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Reload collection so ItemsGeneric picks up the new item
            collection = AssetDatabase.LoadAssetAtPath<TestCollection>($"{TestFolder}/TestCollection.asset");

            bool found = collection.TryGetItemByName<TestItem>("Sword", out var result);

            Assert.IsTrue(found);
            Assert.IsNotNull(result);
            Assert.AreEqual("Sword", result.name);
        }

        [Test]
        public void TryGetItemByName_ReturnsFalseForMissing()
        {
            bool found = collection.TryGetItemByName<TestItem>("DoesNotExist", out var result);

            Assert.IsFalse(found);
            Assert.IsNull(result);
        }

        [Test]
        public void GetOrAddNew_ReturnsExistingItem()
        {
            collection.AddNewItem(typeof(TestItem), "Shield");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            collection = AssetDatabase.LoadAssetAtPath<TestCollection>($"{TestFolder}/TestCollection.asset");

            var item = collection.GetOrAddNew<TestItem>("Shield");

            Assert.IsNotNull(item);
            Assert.AreEqual("Shield", item.name);
        }

        [Test]
        public void GetOrAddNew_CreatesNewItemWhenMissing()
        {
            var item = collection.GetOrAddNew<TestItem>("NewBow");

            Assert.IsNotNull(item);
            Assert.AreEqual("NewBow", item.name);

            string assetPath = AssetDatabase.GetAssetPath(item);
            Assert.IsTrue(assetPath.Contains("Items/"), $"Expected item in Items/ folder, got: {assetPath}");
        }

        [Test]
        public void GetOrAddNew_ByType_ReturnsExistingItem()
        {
            collection.AddNewItem(typeof(TestItem), "Axe");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            collection = AssetDatabase.LoadAssetAtPath<TestCollection>($"{TestFolder}/TestCollection.asset");

            var item = collection.GetOrAddNew(typeof(TestItem), "Axe");

            Assert.IsNotNull(item);
            Assert.AreEqual("Axe", item.name);
        }

        [Test]
        public void GetOrAddNew_ByType_CreatesNewItemWhenMissing()
        {
            var item = collection.GetOrAddNew(typeof(TestItem), "Mace");

            Assert.IsNotNull(item);
            Assert.AreEqual("Mace", item.name);
            Assert.IsTrue(item is TestItem);
        }

        [Test]
        public void FindCollectionForItemPath_FindsParentCollection()
        {
            var item = ScriptableObject.CreateInstance<TestItem>();
            AssetDatabase.CreateAsset(item, $"{ItemsFolder}/FindMe.asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            ScriptableObjectCollection found = SOCAddressableUtility.FindCollectionForItemPath($"{ItemsFolder}/FindMe.asset");

            Assert.IsNotNull(found);
            Assert.AreEqual(collection, found);
        }
    }
}
