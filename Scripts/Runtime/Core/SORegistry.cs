using UnityEngine;
using UnityEngine.AddressableAssets;

namespace BrunoMikoski.ScriptableObjectCollections
{
    /// <summary>
    /// Thin wrapper to load any Addressable ScriptableObject by its asset GUID.
    /// </summary>
    public static class SORegistry
    {
        public const string RegisteredLabel = "soc_registered";

        /// <summary>
        /// Load a ScriptableObject by its asset GUID (Addressable address).
        /// </summary>
        public static T Load<T>(string guid) where T : ScriptableObject
        {
            if (string.IsNullOrEmpty(guid))
                return null;

            return Addressables.LoadAssetAsync<T>(guid).WaitForCompletion();
        }
    }
}
