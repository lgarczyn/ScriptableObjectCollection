using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace BrunoMikoski.ScriptableObjectCollections
{
    /// <summary>
    /// Ensure all addressable entries are up to date before build.
    /// </summary>
    public class CollectionPreprocessBuild : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        void IPreprocessBuildWithReport.OnPreprocessBuild(BuildReport report)
        {
            SOCAddressableUtility.SyncAllAddressables();
        }
    }
}
