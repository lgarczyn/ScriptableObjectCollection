using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public interface ISOCItem
    {
        string name { get; }
    }

    public interface ISOCColorizedItem
    {
        Color LabelColor { get;}
    }
}
