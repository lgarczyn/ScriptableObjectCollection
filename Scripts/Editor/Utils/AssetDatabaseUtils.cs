using System.IO;
using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public static class AssetDatabaseUtils
    {
        public static void CreatePathIfDoesntExist(string targetPath)
        {
            string absolutePath = Path.GetFullPath(targetPath);
            if (Directory.Exists(absolutePath))
                return;

            Directory.CreateDirectory(absolutePath);
            AssetDatabase.Refresh();
        }

        public static void RenameAsset(Object targetObject, string newName)
        {
            string assetPath = AssetDatabase.GetAssetPath(targetObject);
            string guid = AssetDatabase.AssetPathToGUID(assetPath);

            Undo.SetCurrentGroupName($"Rename Asset from {targetObject.name} to {newName}");
            Undo.RecordObject(targetObject, "Rename Asset");
            AssetDatabase.RenameAsset(assetPath, newName);
            Undo.CollapseUndoOperations(Undo.GetCurrentGroup());

            EditorUtility.SetDirty(targetObject);
            AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(guid));
        }
    }
}
