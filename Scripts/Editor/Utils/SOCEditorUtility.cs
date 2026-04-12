using System;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BrunoMikoski.ScriptableObjectCollections
{
    /// <summary>
    /// Editor-only extension methods for managing collection items.
    /// </summary>
    public static class SOCEditorUtility
    {
        /// <summary>
        /// Try to find an item in the collection by asset name.
        /// </summary>
        public static bool TryGetItemByName<T>(this ScriptableObjectCollection collection, string itemName, out T result)
            where T : ScriptableObject, ISOCItem
        {
            var items = collection.ItemsGeneric;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] is T typed && string.Equals(typed.name, itemName, StringComparison.Ordinal))
                {
                    result = typed;
                    return true;
                }
            }

            result = null;
            return false;
        }

        /// <summary>
        /// Try to find an item in the collection by asset name (untyped).
        /// </summary>
        public static bool TryGetItemByName(this ScriptableObjectCollection collection, string itemName, out ScriptableObject result)
        {
            var items = collection.ItemsGeneric;
            for (int i = 0; i < items.Count; i++)
            {
                if (string.Equals(items[i].name, itemName, StringComparison.Ordinal))
                {
                    result = items[i];
                    return true;
                }
            }

            result = null;
            return false;
        }

        /// <summary>
        /// Create a new item asset in the collection's Items/ subfolder.
        /// </summary>
        public static ScriptableObject AddNewItem(
            this ScriptableObjectCollection collection, Type itemType, string assetName = "")
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
                string collectionGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(collection));
                SOCAddressableUtility.EnsureItemAddressable(uniqueAssetPath, collectionGuid);
            }

            return newItem;
        }

        /// <summary>
        /// Remove an item from a collection. Optionally deletes the asset file.
        /// </summary>
        public static void RemoveItem(this ScriptableObjectCollection collection, ScriptableObject item, bool deleteAsset)
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
        public static void RenameItem(this ScriptableObjectCollection collection, ISOCItem item, string newName)
        {
            string path = AssetDatabase.GetAssetPath(item as Object);
            newName = Path.GetFileNameWithoutExtension(newName);
            if (!newName.EndsWith(".asset", StringComparison.Ordinal))
                newName += ".asset";
            AssetDatabase.RenameAsset(path, newName);
        }

        /// <summary>
        /// Get an existing item by name, or create a new one if not found.
        /// </summary>
        public static T GetOrAddNew<T>(this ScriptableObjectCollection collection, string itemName)
            where T : ScriptableObject, ISOCItem
        {
            if (collection.TryGetItemByName<T>(itemName, out var existing))
                return existing;

            return collection.AddNewItem(typeof(T), itemName) as T;
        }

        /// <summary>
        /// Get an existing item by name, or create a new one of the given type if not found.
        /// </summary>
        public static ScriptableObject GetOrAddNew(this ScriptableObjectCollection collection, Type itemType, string itemName)
        {
            if (collection.TryGetItemByName(itemName, out var existing))
                return existing;

            return collection.AddNewItem(itemType, itemName);
        }

        /// <summary>
        /// Move an item to a different collection's folder.
        /// </summary>
        public static void MoveItemToCollection(this ScriptableObjectCollection collection, ISOCItem item, ScriptableObjectCollection targetCollection)
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
