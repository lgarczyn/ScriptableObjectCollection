using BrunoMikoski.ScriptableObjectCollections;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections.Tests
{
    /// <summary>
    /// Concrete item type for testing.
    /// </summary>
    public class TestItem : ScriptableObjectCollectionItem
    {
        [SerializeField]
        private int testValue;
        public int TestValue
        {
            get => testValue;
            set => testValue = value;
        }
    }

    /// <summary>
    /// Concrete collection type for testing.
    /// </summary>
    public class TestCollection : ScriptableObjectCollection<TestItem>
    {
    }

    /// <summary>
    /// A ScriptableObject implementing IRegisteredSO for testing the SORegistry.
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
