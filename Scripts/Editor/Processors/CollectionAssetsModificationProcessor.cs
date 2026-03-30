using System;
using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections
{
    /// <summary>
    /// Handles asset deletion events for SOC items and collections.
    /// With folder-based collections, deleting an item just removes it from the folder.
    /// Deleting a collection triggers a registry metadata sync.
    /// </summary>
    public class CollectionAssetsModificationProcessor : UnityEditor.AssetModificationProcessor
    {
        public static AssetDeleteResult OnWillDeleteAsset(string targetAssetPath, RemoveAssetOptions removeAssetOptions)
        {
            var mainAssetAtPath = AssetDatabase.LoadMainAssetAtPath(targetAssetPath);
            if (mainAssetAtPath == null)
                return AssetDeleteResult.DidNotDelete;

            Type type = mainAssetAtPath.GetType();

            if (type.IsSubclassOf(typeof(ScriptableObject)))
            {
                ScriptableObject collectionItem =
                    AssetDatabase.LoadAssetAtPath<ScriptableObject>(targetAssetPath);

                if (collectionItem is ISOCItem socItem)
                {
                    // Just clear the collection reference - the asset is being deleted
                    socItem.ClearCollection();
                }

                return AssetDeleteResult.DidNotDelete;
            }

            if (type.IsSubclassOf(typeof(ScriptableObjectCollection)) || type == typeof(ScriptableObjectCollection))
            {
                // Collection is being deleted - registry will be updated on next sync
                ScriptableObjectCollection collection =
                    AssetDatabase.LoadAssetAtPath<ScriptableObjectCollection>(targetAssetPath);

                if (collection != null)
                {
                    CollectionsRegistry.Instance.RemoveEntry(collection.GUID);
                }

                return AssetDeleteResult.DidNotDelete;
            }

            return AssetDeleteResult.DidNotDelete;
        }
    }
}
