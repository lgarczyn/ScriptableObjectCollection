using System;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BrunoMikoski.ScriptableObjectCollections
{
    /// <summary>
    /// Editor-only utilities for managing collection items.
    /// </summary>
    public static class SOCEditorUtility
    {
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

            AssetDatabase.CreateAsset(newItem, uniqueAssetPath);

            // Ensure the new item is addressable with the collection's label
            if (AddressableAssetSettingsDefaultObject.Settings != null)
            {
                SOCAddressableUtility.EnsureItemAddressable(uniqueAssetPath, collection.AddressableLabel);
            }

            return newItem;
        }

        /// <summary>
        /// Remove an item from a collection. Optionally deletes the asset file.
        /// </summary>
        public static void RemoveItem(ScriptableObject item, bool deleteAsset)
        {
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
        /// Find an existing item by name, or create a new one if not found.
        /// Used by the generator system.
        /// </summary>
        public static ISOCItem GetOrAddNewItem(ScriptableObjectCollection collection, Type itemType, string targetName)
        {
            if (collection.TryGetItemByName(targetName, out ScriptableObject existing))
                return existing as ISOCItem;

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

            AssetDatabase.MoveAsset(sourcePath, targetPath);
        }
    }
}
