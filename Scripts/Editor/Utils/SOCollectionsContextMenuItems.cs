using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public static class SOCollectionsProjectContextMenus
    {
        [MenuItem("Assets/Move to Different Collection", true, priority = 10000)]
        private static bool ValidateMoveToDifferentCollection()
        {
            Object[] selectedObjects = Selection.objects;
            if (selectedObjects == null || selectedObjects.Length == 0)
                return false;

            foreach (Object obj in selectedObjects)
            {
                if (obj is not ISOCItem)
                    return false;
            }

            List<ScriptableObjectCollection> possibleCollections =
                ScriptableObjectCollection.FindByItemType(selectedObjects[0].GetType());

            return possibleCollections != null && possibleCollections.Count > 1;
        }

        [MenuItem("Assets/Move to Different Collection", priority = 10000)]
        private static void MoveToDifferentCollection()
        {
            Object[] selectedObjects = Selection.objects;
            List<ISOCItem> items = new List<ISOCItem>();

            foreach (Object obj in selectedObjects)
            {
                if (obj is ISOCItem item)
                    items.Add(item);
            }

            if (items.Count == 0)
                return;

            List<ScriptableObjectCollection> possibleCollections =
                ScriptableObjectCollection.FindByItemType(items[0].GetType());

            if (possibleCollections == null || possibleCollections.Count == 0)
            {
                EditorUtility.DisplayDialog("Move to Different Collection", "No collections available.", "OK");
                return;
            }

            // Find the current collection from the item's folder
            string itemPath = AssetDatabase.GetAssetPath(items[0] as Object);
            ScriptableObjectCollection currentCollection = SOCAddressablePostprocessor.FindCollectionForItemPath(itemPath);

            List<ScriptableObjectCollection> filteredCollections = new List<ScriptableObjectCollection>();
            foreach (ScriptableObjectCollection collection in possibleCollections)
            {
                if (collection != currentCollection)
                    filteredCollections.Add(collection);
            }

            if (filteredCollections.Count == 0)
            {
                EditorUtility.DisplayDialog("Move to Different Collection", "No other collections available.", "OK");
                return;
            }

            MoveToCollectionWindow.ShowWindow(items, filteredCollections);
        }


        [MenuItem("Assets/Select Collection", true, priority = 10000)]
        private static bool ValidateSelectCollection()
        {
            Object[] selectedObjects = Selection.objects;
            if (selectedObjects == null || selectedObjects.Length != 1)
                return false;
            if (selectedObjects[0] is not ISOCItem)
                return false;
            string itemPath = AssetDatabase.GetAssetPath(selectedObjects[0]);
            return SOCAddressablePostprocessor.FindCollectionForItemPath(itemPath) != null;
        }

        [MenuItem("Assets/Select Collection", priority = 10000)]
        private static void SelectCollection()
        {
            Object[] selectedObjects = Selection.objects;
            if (selectedObjects == null || selectedObjects.Length != 1)
                return;
            if (selectedObjects[0] is not ISOCItem)
                return;
            string itemPath = AssetDatabase.GetAssetPath(selectedObjects[0]);
            ScriptableObjectCollection collection = SOCAddressablePostprocessor.FindCollectionForItemPath(itemPath);
            if (collection != null)
                Selection.activeObject = collection;
        }
    }
}
