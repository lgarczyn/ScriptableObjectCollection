using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections.Tests
{
    /// <summary>
    /// A ScriptableObject implementing IRegisteredSO for testing the ScriptableObjectRegistry.
    /// </summary>
    public class TestRegisteredItem : ScriptableObject, IRegisteredSO
    {
        [SerializeField, HideInInspector]
        private string m_Guid;
        public string Guid => m_Guid;

        [SerializeField]
        private string label;
        public string Label
        {
            get => label;
            set => label = value;
        }
    }
}
