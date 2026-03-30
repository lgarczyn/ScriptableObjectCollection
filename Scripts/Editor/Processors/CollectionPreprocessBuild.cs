using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace BrunoMikoski.ScriptableObjectCollections
{
    /// <summary>
    /// Pre-build: ensure all addressable entries and registry metadata are up to date.
    /// Post-build: no-op (no more Resources cleanup needed).
    /// </summary>
    public class CollectionPreprocessBuild : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        public int callbackOrder => 0;

        void IPostprocessBuildWithReport.OnPostprocessBuild(BuildReport report)
        {
            // No-op: with Addressables, no post-build cleanup is needed
        }

        void IPreprocessBuildWithReport.OnPreprocessBuild(BuildReport report)
        {
            // Ensure all collections and items are properly addressable before build
            SOCAddressableUtility.SyncAllAddressables();
        }
    }
}
