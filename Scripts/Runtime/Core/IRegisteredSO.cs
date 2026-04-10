namespace BrunoMikoski.ScriptableObjectCollections
{
    /// <summary>
    /// Marker interface for ScriptableObjects that should be automatically discovered
    /// and registered in the global ScriptableObjectRegistry via Addressable labels.
    ///
    /// Any ScriptableObject implementing this interface will be:
    /// - Auto-discovered by the SOCAddressablePostprocessor
    /// - Labeled with the ScriptableObjectRegistry's Addressable label
    /// - Loadable at runtime via Addressables
    /// </summary>
    public interface IRegisteredSO
    {
        string Guid { get; }
    }
}
