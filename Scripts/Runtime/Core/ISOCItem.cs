using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public interface ISOCItem
    {
        string name { get; }
        string Guid { get; }
    }

    public interface ISOCColorizedItem
    {
        Color LabelColor { get;}
    }
}
