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
                    cachedValues_TestCodeGenCollection = Addressables.LoadAssetAsync<BrunoMikoski.ScriptableObjectCollections.Tests.TestCollection>("bed8a6bae37cb74488d9c514a60dbf2e").WaitForCompletion();
                return cachedValues_TestCodeGenCollection;
            }
        }
        
        public static BrunoMikoski.ScriptableObjectCollections.Tests.TestItem Alpha
        {
            get
            {
                if (cachedAlpha == null)
                    cachedAlpha = Addressables.LoadAssetAsync<BrunoMikoski.ScriptableObjectCollections.Tests.TestItem>("5925188d8dbb8df4a8c024e5f085a33a").WaitForCompletion();
                return cachedAlpha;
            }
        }
        
        public static BrunoMikoski.ScriptableObjectCollections.Tests.TestItem Beta
        {
            get
            {
                if (cachedBeta == null)
                    cachedBeta = Addressables.LoadAssetAsync<BrunoMikoski.ScriptableObjectCollections.Tests.TestItem>("d8b777ca261a58f46836d9464c3c2010").WaitForCompletion();
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
