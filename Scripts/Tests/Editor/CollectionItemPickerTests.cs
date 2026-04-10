using System.Collections.Generic;
using BrunoMikoski.ScriptableObjectCollections.Picker;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections.Tests
{
    /// <summary>
    /// Tests for CollectionItemPicker using real assets so AssetReference resolution works.
    /// </summary>
    [TestFixture]
    public class CollectionItemPickerTests
    {
        private const string TestFolder = "Assets/SOCPickerTestTemp";
        private const string ItemsFolder = "Assets/SOCPickerTestTemp/Items";
        private TestCollection collection;
        private TestItem itemA;
        private TestItem itemB;
        private TestItem itemC;

        [SetUp]
        public void SetUp()
        {
            AssetDatabaseUtils.CreatePathIfDoesntExist(TestFolder);
            AssetDatabaseUtils.CreatePathIfDoesntExist(ItemsFolder);

            collection = ScriptableObject.CreateInstance<TestCollection>();

            AssetDatabase.CreateAsset(collection, $"{TestFolder}/PickerTestCollection.asset");

            itemA = ScriptableObject.CreateInstance<TestItem>();
            itemA.name = "Alpha";
            itemA.TestValue = 1;
            AssetDatabase.CreateAsset(itemA, $"{ItemsFolder}/Alpha.asset");

            itemB = ScriptableObject.CreateInstance<TestItem>();
            itemB.name = "Beta";
            itemB.TestValue = 2;
            AssetDatabase.CreateAsset(itemB, $"{ItemsFolder}/Beta.asset");

            itemC = ScriptableObject.CreateInstance<TestItem>();
            itemC.name = "Charlie";
            itemC.TestValue = 3;
            AssetDatabase.CreateAsset(itemC, $"{ItemsFolder}/Charlie.asset");

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

        // --- Basic operations ---

        [Test]
        public void NewPicker_IsEmpty()
        {
            var picker = new CollectionItemPicker<TestItem>();
            Assert.AreEqual(0, picker.Count);
        }

        [Test]
        public void Add_IncreasesCount()
        {
            var picker = new CollectionItemPicker<TestItem>();
            picker.Add(itemA);
            Assert.AreEqual(1, picker.Count);
        }

        [Test]
        public void Add_DuplicateIsIgnored()
        {
            var picker = new CollectionItemPicker<TestItem>();
            picker.Add(itemA);
            picker.Add(itemA);
            Assert.AreEqual(1, picker.Count);
        }

        [Test]
        public void Contains_FindsAddedItem()
        {
            var picker = new CollectionItemPicker<TestItem>();
            picker.Add(itemA);
            Assert.IsTrue(picker.Contains(itemA));
            Assert.IsFalse(picker.Contains(itemB));
        }

        [Test]
        public void Remove_DecreasesCount()
        {
            var picker = new CollectionItemPicker<TestItem>();
            picker.Add(itemA);
            picker.Add(itemB);
            bool removed = picker.Remove(itemA);
            Assert.IsTrue(removed);
            Assert.AreEqual(1, picker.Count);
            Assert.IsFalse(picker.Contains(itemA));
        }

        [Test]
        public void Remove_NonexistentReturnsFalse()
        {
            var picker = new CollectionItemPicker<TestItem>();
            picker.Add(itemA);
            Assert.IsFalse(picker.Remove(itemB));
        }

        [Test]
        public void Clear_EmptiesPicker()
        {
            var picker = new CollectionItemPicker<TestItem>();
            picker.Add(itemA);
            picker.Add(itemB);
            picker.Clear();
            Assert.AreEqual(0, picker.Count);
        }

        [Test]
        public void Indexer_ReturnsCorrectItem()
        {
            var picker = new CollectionItemPicker<TestItem>();
            picker.Add(itemA);
            picker.Add(itemB);
            Assert.AreEqual(itemA, picker[0]);
            Assert.AreEqual(itemB, picker[1]);
        }

        [Test]
        public void IndexOf_ReturnsCorrectIndex()
        {
            var picker = new CollectionItemPicker<TestItem>();
            picker.Add(itemA);
            picker.Add(itemB);
            Assert.AreEqual(0, picker.IndexOf(itemA));
            Assert.AreEqual(1, picker.IndexOf(itemB));
            Assert.AreEqual(-1, picker.IndexOf(itemC));
        }

        [Test]
        public void Insert_PlacesAtCorrectIndex()
        {
            var picker = new CollectionItemPicker<TestItem>();
            picker.Add(itemA);
            picker.Add(itemC);
            picker.Insert(1, itemB);
            Assert.AreEqual(3, picker.Count);
            Assert.AreEqual(itemB, picker[1]);
        }

        [Test]
        public void Insert_DuplicateIsIgnored()
        {
            var picker = new CollectionItemPicker<TestItem>();
            picker.Add(itemA);
            picker.Insert(0, itemA);
            Assert.AreEqual(1, picker.Count);
        }

        [Test]
        public void RemoveAt_RemovesCorrectItem()
        {
            var picker = new CollectionItemPicker<TestItem>();
            picker.Add(itemA);
            picker.Add(itemB);
            picker.RemoveAt(0);
            Assert.AreEqual(1, picker.Count);
            Assert.AreEqual(itemB, picker[0]);
        }

        // --- Constructor ---

        [Test]
        public void Constructor_WithItems_PopulatesPicker()
        {
            var picker = new CollectionItemPicker<TestItem>(itemA, itemB);
            Assert.AreEqual(2, picker.Count);
            Assert.IsTrue(picker.Contains(itemA));
            Assert.IsTrue(picker.Contains(itemB));
        }

        // --- HasAny / HasAll / HasNone ---

        [Test]
        public void HasAny_ReturnsTrueWhenOneMatches()
        {
            var picker = new CollectionItemPicker<TestItem>(itemA);
            Assert.IsTrue(picker.HasAny(itemA, itemB));
        }

        [Test]
        public void HasAny_ReturnsFalseWhenNoneMatch()
        {
            var picker = new CollectionItemPicker<TestItem>(itemA);
            Assert.IsFalse(picker.HasAny(itemB, itemC));
        }

        [Test]
        public void HasAll_ReturnsTrueWhenAllPresent()
        {
            var picker = new CollectionItemPicker<TestItem>(itemA, itemB);
            Assert.IsTrue(picker.HasAll(itemA, itemB));
        }

        [Test]
        public void HasAll_ReturnsFalseWhenOneMissing()
        {
            var picker = new CollectionItemPicker<TestItem>(itemA);
            Assert.IsFalse(picker.HasAll(itemA, itemB));
        }

        [Test]
        public void HasNone_ReturnsTrueWhenNonePresent()
        {
            var picker = new CollectionItemPicker<TestItem>(itemA);
            Assert.IsTrue(picker.HasNone(itemB, itemC));
        }

        [Test]
        public void HasNone_ReturnsFalseWhenOnePresent()
        {
            var picker = new CollectionItemPicker<TestItem>(itemA, itemB);
            Assert.IsFalse(picker.HasNone(itemB, itemC));
        }

        // --- Operators ---

        [Test]
        public void OperatorPlus_CombinesPickers()
        {
            var picker1 = new CollectionItemPicker<TestItem>(itemA);
            var picker2 = new CollectionItemPicker<TestItem>(itemB);
            var combined = picker1 + picker2;
            Assert.AreEqual(2, combined.Count);
            Assert.IsTrue(combined.Contains(itemA));
            Assert.IsTrue(combined.Contains(itemB));
        }

        [Test]
        public void OperatorPlus_NoDuplicates()
        {
            var picker1 = new CollectionItemPicker<TestItem>(itemA, itemB);
            var picker2 = new CollectionItemPicker<TestItem>(itemB, itemC);
            var combined = picker1 + picker2;
            Assert.AreEqual(3, combined.Count);
        }

        [Test]
        public void OperatorMinus_RemovesItems()
        {
            var picker1 = new CollectionItemPicker<TestItem>(itemA, itemB, itemC);
            var picker2 = new CollectionItemPicker<TestItem>(itemB);
            var result = picker1 - picker2;
            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Contains(itemA));
            Assert.IsFalse(result.Contains(itemB));
            Assert.IsTrue(result.Contains(itemC));
        }

        [Test]
        public void OperatorPlusItem_AddsItem()
        {
            var picker = new CollectionItemPicker<TestItem>(itemA);
            picker = picker + itemB;
            Assert.AreEqual(2, picker.Count);
        }

        [Test]
        public void OperatorMinusItem_RemovesItem()
        {
            var picker = new CollectionItemPicker<TestItem>(itemA, itemB);
            picker = picker - itemA;
            Assert.AreEqual(1, picker.Count);
            Assert.IsFalse(picker.Contains(itemA));
        }

        // --- Events ---

        [Test]
        public void Add_FiresOnItemTypeAddedEvent()
        {
            var picker = new CollectionItemPicker<TestItem>();
            TestItem addedItem = null;
            picker.OnItemTypeAddedEvent += item => addedItem = item;
            picker.Add(itemA);
            Assert.AreEqual(itemA, addedItem);
        }

        [Test]
        public void Remove_FiresOnItemTypeRemovedEvent()
        {
            var picker = new CollectionItemPicker<TestItem>(itemA);
            TestItem removedItem = null;
            picker.OnItemTypeRemovedEvent += item => removedItem = item;
            picker.Remove(itemA);
            Assert.AreEqual(itemA, removedItem);
        }

        [Test]
        public void Add_FiresOnChangedEvent()
        {
            var picker = new CollectionItemPicker<TestItem>();
            bool changed = false;
            picker.OnChangedEvent += () => changed = true;
            picker.Add(itemA);
            Assert.IsTrue(changed);
        }

        [Test]
        public void Clear_FiresOnChangedEvent()
        {
            var picker = new CollectionItemPicker<TestItem>(itemA);
            bool changed = false;
            picker.OnChangedEvent += () => changed = true;
            picker.Clear();
            Assert.IsTrue(changed);
        }

        // --- Equality ---

        [Test]
        public void Equals_SameItems_ReturnsTrue()
        {
            var picker1 = new CollectionItemPicker<TestItem>(itemA, itemB);
            var picker2 = new CollectionItemPicker<TestItem>(itemA, itemB);
            Assert.IsTrue(picker1.Equals(picker2));
        }

        [Test]
        public void Equals_DifferentItems_ReturnsFalse()
        {
            var picker1 = new CollectionItemPicker<TestItem>(itemA);
            var picker2 = new CollectionItemPicker<TestItem>(itemB);
            Assert.IsFalse(picker1.Equals(picker2));
        }

        [Test]
        public void Equals_DifferentCount_ReturnsFalse()
        {
            var picker1 = new CollectionItemPicker<TestItem>(itemA);
            var picker2 = new CollectionItemPicker<TestItem>(itemA, itemB);
            Assert.IsFalse(picker1.Equals(picker2));
        }

        [Test]
        public void Equals_IList_SameItems_ReturnsTrue()
        {
            var picker = new CollectionItemPicker<TestItem>(itemA, itemB);
            IList<TestItem> list = new List<TestItem> { itemA, itemB };
            Assert.IsTrue(picker.Equals(list));
        }

        // --- Implicit conversion ---

        [Test]
        public void ImplicitConversion_ToList()
        {
            var picker = new CollectionItemPicker<TestItem>(itemA, itemB);
            List<TestItem> list = picker;
            Assert.AreEqual(2, list.Count);
        }

        // --- Enumeration ---

        [Test]
        public void GetEnumerator_IteratesAllItems()
        {
            var picker = new CollectionItemPicker<TestItem>(itemA, itemB, itemC);
            int count = 0;
            foreach (var item in picker)
                count++;
            Assert.AreEqual(3, count);
        }

        // --- CopyTo ---

        [Test]
        public void CopyTo_CopiesItems()
        {
            var picker = new CollectionItemPicker<TestItem>(itemA, itemB);
            var array = new TestItem[2];
            picker.CopyTo(array, 0);
            Assert.AreEqual(itemA, array[0]);
            Assert.AreEqual(itemB, array[1]);
        }

        // --- IsReadOnly ---

        [Test]
        public void IsReadOnly_ReturnsFalse()
        {
            var picker = new CollectionItemPicker<TestItem>();
            Assert.IsFalse(picker.IsReadOnly);
        }
    }
}
