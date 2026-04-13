using UnityEngine.AddressableAssets;

namespace BrunoMikoski.ScriptableObjectCollections
{
    /// <summary>
    /// Thin wrapper to load any Addressable ScriptableObject by its asset GUID.
    /// Also provides lookup for IRegisteredSO items.
    /// </summary>
    public static class ScriptableObjectRegistry
    {
        public const string RegisteredLabel = "soc_registered";

        /// <summary>
        /// Load a ScriptableObject by its asset GUID (Addressable address).
        /// </summary>
        public static T Load<T>(string guid) where T : class, IRegisteredSO
        {
            if (string.IsNullOrEmpty(guid))
                return null;

            return Addressables.LoadAssetAsync<T>(guid).WaitForCompletion();
        }

        /// <summary>
        /// Resolve a WeakReference, reloading via Addressables if the target was collected or destroyed.
        /// Used by generated static accessors.
        /// </summary>
        public static T Resolve<T>(ref System.WeakReference<T> weakRef, string guid) where T : class
        {
#if !UNITY_EDITOR
            if (weakRef != null && weakRef.TryGetTarget(out var cached) && cached is UnityEngine.Object obj && obj != null)
                return cached;
#endif

            if (string.IsNullOrEmpty(guid))
                return null;

            var loaded = Addressables.LoadAssetAsync<T>(guid).WaitForCompletion();
            weakRef = new System.WeakReference<T>(loaded);
            return loaded;
        }

        /// <summary>
        /// Try to find a registered item by its baked asset GUID.
        /// Loads the item via Addressables.
        /// </summary>
        public static bool TryGetItemByGUID<T>(string guid, out T result) where T : class, IRegisteredSO
        {
            if (string.IsNullOrEmpty(guid))
            {
                result = null;
                return false;
            }

            result = Load<T>(guid);
            return result != null;
        }
    }
}
