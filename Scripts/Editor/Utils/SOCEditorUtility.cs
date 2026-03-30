using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BrunoMikoski.ScriptableObjectCollections
{
    /// <summary>
    /// Editor-only utilities for managing collection items.
    /// Extracted from ScriptableObjectCollection to keep the runtime class minimal.
    /// </summary>
    public static class SOCEditorUtility
    {
        /// <summary>
        /// Find all items of the collection's item type in the collection's folder (and subfolders).
        /// This is the editor-time equivalent of Addressable label-based loading at runtime.
        /// </summary>
        public static List<ScriptableObject> GetItemsInCollectionFolder(ScriptableObjectCollection collection)
        {
            string assetPath = AssetDatabase.GetAssetPath(collection);
            string folder = Path.GetDirectoryName(assetPath);
            return FindItemsInFolder(collection, folder);
        }

        /// <summary>
        /// Find all items of the collection's item type in the given folder.
        /// </summary>
        public static List<ScriptableObject> FindItemsInFolder(ScriptableObjectCollection collection, string folder)
        {
            Type itemType = collection.GetItemType();
            if (itemType == null)
                return new List<ScriptableObject>();

            string[] guids = AssetDatabase.FindAssets($"t:{itemType.Name}", new[] { folder });
            var items = new List<ScriptableObject>();

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var item = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (item is ISOCItem)
                    items.Add(item);
            }

            return items;
        }

        /// <summary>
        /// Create a new item asset in the collection's Items/ subfolder.
        /// </summary>
        public static ScriptableObject AddNewItem(
            ScriptableObjectCollection collection, Type itemType, string assetName = "")
        {
            if (!typeof(ISOCItem).IsAssignableFrom(itemType))
                throw new Exception($"{itemType} does not implement {nameof(ISOCItem)}");

            ScriptableObject newItem = ScriptableObject.CreateInstance(itemType);
            string assetPath = Path.GetDirectoryName(AssetDatabase.GetAssetPath(collection));
            string parentFolderPath = Path.Combine(assetPath, "Items");
            AssetDatabaseUtils.CreatePathIfDoesntExist(parentFolderPath);

            string itemName = string.IsNullOrEmpty(assetName) ? itemType.Name : assetName;

            if (itemName.IsReservedKeyword())
                Debug.LogError($"{itemName} is a reserved keyword name, will cause issues with code generation, please rename it");

            string uniqueAssetPath = AssetDatabase.GenerateUniqueAssetPath(
                Path.Combine(parentFolderPath, itemName + ".asset"));
            string uniqueName = Path.GetFileNameWithoutExtension(uniqueAssetPath);

            newItem.name = uniqueName;

            if (newItem is ISOCItem socItem)
            {
                socItem.GenerateNewGUID();
                socItem.SetCollection(collection);
            }

            AssetDatabase.CreateAsset(newItem, uniqueAssetPath);

            // Ensure the new item is addressable with the collection's label
            SOCAddressableUtility.EnsureItemAddressable(uniqueAssetPath, collection.AddressableLabel);

            // Update the registry
            SOCAddressableUtility.SyncAllAddressables();

            return newItem;
        }

        /// <summary>
        /// Remove an item from a collection. Optionally deletes the asset file.
        /// </summary>
        public static void RemoveItem(ScriptableObject item, bool deleteAsset)
        {
            if (item is ISOCItem socItem)
                socItem.ClearCollection();

            if (deleteAsset)
            {
                string path = AssetDatabase.GetAssetPath(item);
                AssetDatabase.DeleteAsset(path);
            }
        }

        /// <summary>
        /// Rename an item asset.
        /// </summary>
        public static void RenameItem(ISOCItem item, string newName)
        {
            string path = AssetDatabase.GetAssetPath(item as Object);
            newName = Path.GetFileNameWithoutExtension(newName);
            if (!newName.EndsWith(".asset"))
                newName += ".asset";
            AssetDatabase.RenameAsset(path, newName);
        }

        /// <summary>
        /// Populate the collection's editor items list from its folder.
        /// Call this before accessing collection.Items in editor code.
        /// </summary>
        public static void RefreshEditorItems(ScriptableObjectCollection collection)
        {
            var items = GetItemsInCollectionFolder(collection);
            collection.SetEditorItems(items);
        }

        /// <summary>
        /// Find an existing item by name, or create a new one if not found.
        /// Used by the generator system.
        /// </summary>
        public static ISOCItem GetOrAddNewItem(ScriptableObjectCollection collection, Type itemType, string targetName)
        {
            var items = GetItemsInCollectionFolder(collection);
            foreach (var item in items)
            {
                if (item.name.Equals(targetName, StringComparison.Ordinal))
                    return item as ISOCItem;
            }

            return AddNewItem(collection, itemType, targetName) as ISOCItem;
        }

        /// <summary>
        /// Move an item to a different collection's folder.
        /// </summary>
        public static void MoveItemToCollection(ISOCItem item, ScriptableObjectCollection targetCollection)
        {
            var assetObject = item as Object;
            string sourcePath = AssetDatabase.GetAssetPath(assetObject);
            string targetFolder = Path.GetDirectoryName(AssetDatabase.GetAssetPath(targetCollection));
            string itemsFolder = Path.Combine(targetFolder, "Items");
            AssetDatabaseUtils.CreatePathIfDoesntExist(itemsFolder);

            string fileName = Path.GetFileName(sourcePath);
            string targetPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(itemsFolder, fileName));

            item.ClearCollection();
            AssetDatabase.MoveAsset(sourcePath, targetPath);
            item.SetCollection(targetCollection);
            EditorUtility.SetDirty(assetObject);
        }
    }
}
