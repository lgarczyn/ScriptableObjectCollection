using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public class ScriptableObjectCollectionItem : ScriptableObject, ISOCItem
    {
        [SerializeField, HideInInspector]
        private string m_Guid;
        public string Guid => m_Guid;
    }
}
