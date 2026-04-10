using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace BrunoMikoski.ScriptableObjectCollections
{
    /// <summary>
    /// A global registry that holds references to all ScriptableObjects implementing IRegisteredSO.
    /// Items are auto-discovered in the editor via AssetPostprocessor and labeled for Addressables.
    /// At runtime, all registered items are loaded via their shared Addressable label.
    /// </summary>
    public static class SORegistry
    {
        public const string RegisteredLabel = "soc_registered";

        private static List<ScriptableObject> loadedItems;
        private static AsyncOperationHandle<IList<ScriptableObject>> itemsHandle;
        private static bool isLoaded;

        public static bool IsLoaded => isLoaded;

        /// <summary>
        /// All registered ScriptableObjects. Loads on first access.
        /// </summary>
        public static IReadOnlyList<ScriptableObject> Items
        {
            get
            {
                if (!isLoaded)
                    LoadSync();
                return loadedItems;
            }
        }

        public static void LoadSync()
        {
            if (isLoaded) return;

            try
            {
                itemsHandle = Addressables.LoadAssetsAsync<ScriptableObject>(RegisteredLabel, null);
                var result = itemsHandle.WaitForCompletion();
                loadedItems = result != null ? new List<ScriptableObject>(result) : new List<ScriptableObject>();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"SORegistry: Failed to load registered items: {e.Message}");
                loadedItems = new List<ScriptableObject>();
            }
            isLoaded = true;
        }

        public static void Unload()
        {
            if (!isLoaded) return;

            if (itemsHandle.IsValid())
                Addressables.Release(itemsHandle);

            loadedItems = null;
            isLoaded = false;
        }

        /// <summary>
        /// Get all registered items of a specific type.
        /// </summary>
        public static List<T> GetAll<T>() where T : ScriptableObject, IRegisteredSO
        {
            var result = new List<T>();
            var items = Items;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] is T typed)
                    result.Add(typed);
            }
            return result;
        }

        /// <summary>
        /// Try to find a registered item by name.
        /// </summary>
        public static bool TryGetByName<T>(string targetName, out T result) where T : ScriptableObject, IRegisteredSO
        {
            var items = Items;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] is T typed && string.Equals(typed.name, targetName, StringComparison.Ordinal))
                {
                    result = typed;
                    return true;
                }
            }
            result = null;
            return false;
        }

        /// <summary>
        /// Try to find a registered item by type (returns first match).
        /// </summary>
        public static bool TryGet<T>(out T result) where T : ScriptableObject, IRegisteredSO
        {
            var items = Items;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] is T typed)
                {
                    result = typed;
                    return true;
                }
            }
            result = null;
            return false;
        }
    }
}
