//  Automatically generated
//

using BrunoMikoski.ScriptableObjectCollections.Tests;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace TestNamespace
{
    public partial class TestItem
    {
        private static BrunoMikoski.ScriptableObjectCollections.Tests.TestCollection cachedValues_TestCodeGenCollection;
        
        private static BrunoMikoski.ScriptableObjectCollections.Tests.TestItem cachedAlpha;
        private static BrunoMikoski.ScriptableObjectCollections.Tests.TestItem cachedBeta;
        
        public static BrunoMikoski.ScriptableObjectCollections.Tests.TestCollection Values_TestCodeGenCollection
        {
            get
            {
                if (cachedValues_TestCodeGenCollection == null)
                    cachedValues_TestCodeGenCollection = Addressables.LoadAssetAsync<BrunoMikoski.ScriptableObjectCollections.Tests.TestCollection>("f8ca69cdc241cf347a0fb14fba230646").WaitForCompletion();
                return cachedValues_TestCodeGenCollection;
            }
        }
        
        public static BrunoMikoski.ScriptableObjectCollections.Tests.TestItem Alpha
        {
            get
            {
                if (cachedAlpha == null)
                    cachedAlpha = Addressables.LoadAssetAsync<BrunoMikoski.ScriptableObjectCollections.Tests.TestItem>("d19c868d72ba7fe4c82a02f6eef52165").WaitForCompletion();
                return cachedAlpha;
            }
        }
        
        public static BrunoMikoski.ScriptableObjectCollections.Tests.TestItem Beta
        {
            get
            {
                if (cachedBeta == null)
                    cachedBeta = Addressables.LoadAssetAsync<BrunoMikoski.ScriptableObjectCollections.Tests.TestItem>("867decf4069045c4c9e741ee0cc7c4f6").WaitForCompletion();
                return cachedBeta;
            }
        }
        
        public static bool IsCollectionLoaded => cachedValues_TestCodeGenCollection != null && cachedValues_TestCodeGenCollection.IsLoaded;
        
        public static void UnloadCollection()
        {
            if (cachedValues_TestCodeGenCollection != null)
                cachedValues_TestCodeGenCollection.Unload();
            cachedValues_TestCodeGenCollection = null;
            cachedAlpha = null;
            cachedBeta = null;
        }
        
    }
}
