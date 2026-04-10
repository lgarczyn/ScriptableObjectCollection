using UnityEditor;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public static class ScriptableObjectCollectionContextMenuItems
    {
        [MenuItem("CONTEXT/ScriptableObjectCollection/Reset Settings", false, 1000)]
        private static void ResetSettings(MenuCommand command)
        {
            ScriptableObjectCollection collection = (ScriptableObjectCollection)command.context;
            SOCSettings.Instance.ResetSettings(collection);
        }
    }
}
