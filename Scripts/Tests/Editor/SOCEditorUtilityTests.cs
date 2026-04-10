using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections.Tests
{
    /// <summary>
    /// Integration tests for SOCEditorUtility.
    /// Creates real assets on disk via AssetDatabase to test folder-based discovery.
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
            // Create test folder structure
            AssetDatabaseUtils.CreatePathIfDoesntExist(TestFolder);
            AssetDatabaseUtils.CreatePathIfDoesntExist(ItemsFolder);

            // Create a test collection asset
            collection = ScriptableObject.CreateInstance<TestCollection>();
            collection.GenerateNewGUID();
            AssetDatabase.CreateAsset(collection, $"{TestFolder}/TestCollection.asset");
            AssetDatabase.SaveAssets();
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up all test assets
            if (AssetDatabase.IsValidFolder(TestFolder))
            {
                AssetDatabase.DeleteAsset(TestFolder);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [Test]
        public void GetItemsInCollectionFolder_EmptyFolder_ReturnsEmpty()
        {
            List<ScriptableObject> items = SOCEditorUtility.GetItemsInCollectionFolder(collection);
            Assert.AreEqual(0, items.Count);
        }

        [Test]
        public void GetItemsInCollectionFolder_FindsItemsInSubfolder()
        {
            // Create an item in the Items/ subfolder
            var item = ScriptableObject.CreateInstance<TestItem>();
            item.GenerateNewGUID();
            item.name = "Sword";
            AssetDatabase.CreateAsset(item, $"{ItemsFolder}/Sword.asset");
            AssetDatabase.SaveAssets();

            List<ScriptableObject> items = SOCEditorUtility.GetItemsInCollectionFolder(collection);
            Assert.AreEqual(1, items.Count);
            Assert.AreEqual("Sword", items[0].name);
        }

        [Test]
        public void GetItemsInCollectionFolder_FindsMultipleItems()
        {
            for (int i = 0; i < 3; i++)
            {
                var item = ScriptableObject.CreateInstance<TestItem>();
                item.GenerateNewGUID();
                item.name = $"Item{i}";
                AssetDatabase.CreateAsset(item, $"{ItemsFolder}/Item{i}.asset");
            }
            AssetDatabase.SaveAssets();

            List<ScriptableObject> items = SOCEditorUtility.GetItemsInCollectionFolder(collection);
            Assert.AreEqual(3, items.Count);
        }

        [Test]
        public void AddNewItem_CreatesAssetInItemsSubfolder()
        {
            ScriptableObject newItem = SOCEditorUtility.AddNewItem(collection, typeof(TestItem), "MySword");

            Assert.IsNotNull(newItem);
            Assert.AreEqual("MySword", newItem.name);

            string assetPath = AssetDatabase.GetAssetPath(newItem);
            Assert.IsTrue(assetPath.Contains("Items/"), $"Expected item in Items/ folder, got: {assetPath}");

            // Verify it's an ISOCItem with a valid GUID
            Assert.IsTrue(newItem is ISOCItem);
            Assert.IsTrue(((ISOCItem)newItem).GUID.IsValid());
        }

        [Test]
        public void AddNewItem_SetsCollectionReference()
        {
            ScriptableObject newItem = SOCEditorUtility.AddNewItem(collection, typeof(TestItem), "Arrow");

            ISOCItem socItem = newItem as ISOCItem;
            Assert.IsNotNull(socItem);
            Assert.AreEqual(collection, socItem.Collection);
        }

        [Test]
        public void AddNewItem_GeneratesUniqueName()
        {
            ScriptableObject item1 = SOCEditorUtility.AddNewItem(collection, typeof(TestItem), "Potion");
            ScriptableObject item2 = SOCEditorUtility.AddNewItem(collection, typeof(TestItem), "Potion");

            // Second item should get a unique name
            Assert.AreNotEqual(
                AssetDatabase.GetAssetPath(item1),
                AssetDatabase.GetAssetPath(item2));
        }

        [Test]
        public void RemoveItem_DeletesAsset()
        {
            var item = ScriptableObject.CreateInstance<TestItem>();
            item.GenerateNewGUID();
            item.name = "ToDelete";
            string assetPath = $"{ItemsFolder}/ToDelete.asset";
            AssetDatabase.CreateAsset(item, assetPath);
            AssetDatabase.SaveAssets();

            Assert.IsTrue(File.Exists(assetPath));

            SOCEditorUtility.RemoveItem(item, deleteAsset: true);
            AssetDatabase.Refresh();

            Assert.IsNull(AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath));
        }

        [Test]
        public void RemoveItem_ClearsCollectionReference()
        {
            var item = ScriptableObject.CreateInstance<TestItem>();
            item.GenerateNewGUID();
            item.SetCollectionRuntime(collection);
            AssetDatabase.CreateAsset(item, $"{ItemsFolder}/ClearRef.asset");
            AssetDatabase.SaveAssets();

            SOCEditorUtility.RemoveItem(item, deleteAsset: false);

            // After removal without delete, item still exists but collection is cleared
            Assert.IsTrue(AssetDatabase.LoadAssetAtPath<ScriptableObject>($"{ItemsFolder}/ClearRef.asset") != null);
        }

        [Test]
        public void RefreshEditorItems_PopulatesCollectionItems()
        {
            // Create items
            for (int i = 0; i < 2; i++)
            {
                var item = ScriptableObject.CreateInstance<TestItem>();
                item.GenerateNewGUID();
                item.name = $"RefreshItem{i}";
                AssetDatabase.CreateAsset(item, $"{ItemsFolder}/RefreshItem{i}.asset");
            }
            AssetDatabase.SaveAssets();

            SOCEditorUtility.RefreshEditorItems(collection);

            Assert.AreEqual(2, collection.Count);
        }

        [Test]
        public void GetOrAddNewItem_FindsExistingByName()
        {
            // Create an item first
            var existing = ScriptableObject.CreateInstance<TestItem>();
            existing.GenerateNewGUID();
            existing.name = "ExistingItem";
            existing.SetCollection(collection);
            AssetDatabase.CreateAsset(existing, $"{ItemsFolder}/ExistingItem.asset");
            AssetDatabase.SaveAssets();

            ISOCItem found = SOCEditorUtility.GetOrAddNewItem(collection, typeof(TestItem), "ExistingItem");

            Assert.IsNotNull(found);
            Assert.AreEqual("ExistingItem", found.name);
        }

        [Test]
        public void GetOrAddNewItem_CreatesNewIfNotFound()
        {
            ISOCItem created = SOCEditorUtility.GetOrAddNewItem(collection, typeof(TestItem), "BrandNew");

            Assert.IsNotNull(created);
            Assert.AreEqual("BrandNew", created.name);

            // Verify it exists on disk
            List<ScriptableObject> items = SOCEditorUtility.GetItemsInCollectionFolder(collection);
            Assert.IsTrue(items.Exists(i => i.name == "BrandNew"));
        }

        [Test]
        public void FindCollectionForItemPath_FindsParentCollection()
        {
            // Create an item
            var item = ScriptableObject.CreateInstance<TestItem>();
            item.GenerateNewGUID();
            AssetDatabase.CreateAsset(item, $"{ItemsFolder}/FindMe.asset");
            AssetDatabase.SaveAssets();

            ScriptableObjectCollection found = SOCAddressableUtility.FindCollectionForItemPath($"{ItemsFolder}/FindMe.asset");

            Assert.IsNotNull(found);
            Assert.AreEqual(collection.GUID, found.GUID);
        }
    }
}
