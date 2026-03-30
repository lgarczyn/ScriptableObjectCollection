using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public interface ISOCItem
    {
        LongGuid GUID { get; }
        ScriptableObjectCollection Collection { get; }
        string name { get; set; }
        void SetCollection(ScriptableObjectCollection collection);
        /// <summary>
        /// Runtime-only: sets the cached collection reference without dirtying the asset.
        /// </summary>
        void SetCollectionRuntime(ScriptableObjectCollection collection);
        void GenerateNewGUID();
        void ClearCollection();
    }

    public interface ISOCColorizedItem
    {
        Color LabelColor { get;}
    }
}