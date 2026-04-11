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
}
