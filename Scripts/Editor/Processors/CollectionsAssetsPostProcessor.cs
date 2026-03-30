using System;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections
{
    /// <summary>
    /// Watches for new/changed SOC-related assets and triggers addressable sync.
    /// Most of the heavy lifting is now done by SOCAddressablePostprocessor;
    /// this processor handles domain-reload-related registry refreshes.
    /// </summary>
    public sealed class CollectionsAssetsPostProcessor : AssetPostprocessor
    {
        private const string REFRESH_REGISTRY_AFTER_RECOMPILATION_KEY = "RefreshRegistryAfterRecompilationKey";

        private static bool RefreshRegistryAfterRecompilation
        {
            get => EditorPrefs.GetBool(REFRESH_REGISTRY_AFTER_RECOMPILATION_KEY, false);
            set => EditorPrefs.SetBool(REFRESH_REGISTRY_AFTER_RECOMPILATION_KEY, value);
        }

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            bool needsSync = false;

            for (int i = 0; i < importedAssets.Length; i++)
            {
                string importedAssetPath = importedAssets[i];
                Type type = AssetDatabase.GetMainAssetTypeAtPath(importedAssetPath);

                if (typeof(ScriptableObjectCollection).IsAssignableFrom(type))
                {
                    needsSync = true;
                }
            }

            if (needsSync)
            {
                RefreshRegistry();
            }
        }

        private static void RefreshRegistry()
        {
            if (EditorApplication.isCompiling)
            {
                RefreshRegistryAfterRecompilation = true;
                return;
            }

            EditorApplication.delayCall += () => { SOCAddressableUtility.SyncAllAddressables(); };
        }

        [DidReloadScripts]
        static void OnAfterScriptsReloading()
        {
            if (RefreshRegistryAfterRecompilation)
                RefreshRegistry();

            RefreshRegistryAfterRecompilation = false;
        }
    }
}
