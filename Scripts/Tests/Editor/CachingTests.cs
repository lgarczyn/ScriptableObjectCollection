using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace BrunoMikoski.ScriptableObjectCollections.Tests
{
    /// <summary>
    /// Tests for static caching, WeakReference resolve, and cache lifecycle.
    /// </summary>
    [TestFixture]
    public class CachingTests
    {
        [TearDown]
        public void TearDown()
        {
            ScriptableObjectCollection.ClearCache();
        }

        // --- FindAll caching ---

        [Test]
        public void FindAll_ReturnsSameInstanceOnSecondCall()
        {
            LogAssert.ignoreFailingMessages = true;
            var first = ScriptableObjectCollection.FindAll();
            var second = ScriptableObjectCollection.FindAll();
            LogAssert.ignoreFailingMessages = false;

            Assert.AreSame(first, second);
        }

        [Test]
        public void ClearCache_InvalidatesFindAll()
        {
            LogAssert.ignoreFailingMessages = true;
            var first = ScriptableObjectCollection.FindAll();
            ScriptableObjectCollection.ClearCache();
            var second = ScriptableObjectCollection.FindAll();
            LogAssert.ignoreFailingMessages = false;

            Assert.AreNotSame(first, second);
        }

        // --- OfType caching ---

        [Test]
        public void OfType_ReturnsSameInstanceOnSecondCall()
        {
            LogAssert.ignoreFailingMessages = true;
            var first = ScriptableObjectCollection.OfType<TestItem>();
            var second = ScriptableObjectCollection.OfType<TestItem>();
            LogAssert.ignoreFailingMessages = false;

            Assert.AreSame(first, second);
        }

        [Test]
        public void ClearCache_InvalidatesOfType()
        {
            LogAssert.ignoreFailingMessages = true;
            var first = ScriptableObjectCollection.OfType<TestItem>();
            ScriptableObjectCollection.ClearCache();
            var second = ScriptableObjectCollection.OfType<TestItem>();
            LogAssert.ignoreFailingMessages = false;

            Assert.AreNotSame(first, second);
        }

        // --- Unload triggers ClearCache ---

        [Test]
        public void Unload_InvalidatesFindAllCache()
        {
            LogAssert.ignoreFailingMessages = true;
            var first = ScriptableObjectCollection.FindAll();

            var collection = ScriptableObject.CreateInstance<TestCollection>();
            collection.Load(); // will fail gracefully
            collection.Unload(); // should call ClearCache

            var second = ScriptableObjectCollection.FindAll();
            LogAssert.ignoreFailingMessages = false;

            Assert.AreNotSame(first, second);
            Object.DestroyImmediate(collection);
        }

        [Test]
        public void Unload_InvalidatesOfTypeCache()
        {
            LogAssert.ignoreFailingMessages = true;
            var first = ScriptableObjectCollection.OfType<TestItem>();

            var collection = ScriptableObject.CreateInstance<TestCollection>();
            collection.Load();
            collection.Unload();

            var second = ScriptableObjectCollection.OfType<TestItem>();
            LogAssert.ignoreFailingMessages = false;

            Assert.AreNotSame(first, second);
            Object.DestroyImmediate(collection);
        }

        // --- ScriptableObjectRegistry.Resolve ---

        [Test]
        public void Resolve_WithNullGuid_ReturnsNull()
        {
            WeakReference<TestItem> weakRef = null;
            LogAssert.ignoreFailingMessages = true;
            var result = ScriptableObjectRegistry.Resolve(ref weakRef, null);
            LogAssert.ignoreFailingMessages = false;

            Assert.IsNull(result);
        }

        [Test]
        public void Resolve_WithEmptyGuid_ReturnsNull()
        {
            WeakReference<TestItem> weakRef = null;
            LogAssert.ignoreFailingMessages = true;
            var result = ScriptableObjectRegistry.Resolve(ref weakRef, "");
            LogAssert.ignoreFailingMessages = false;

            Assert.IsNull(result);
        }

        [Test]
        public void Resolve_CachesInWeakReference()
        {
            // Create a real SO to use as the target
            var item = ScriptableObject.CreateInstance<TestItem>();
            item.name = "CacheTest";

            WeakReference<TestItem> weakRef = new WeakReference<TestItem>(item);

            // Resolve should return the cached item without hitting Addressables
            var result = ScriptableObjectRegistry.Resolve(ref weakRef, "doesnt-matter");
            Assert.AreEqual(item, result);

            Object.DestroyImmediate(item);
        }

        [Test]
        public void Resolve_ReloadsWhenTargetDestroyed()
        {
            var item = ScriptableObject.CreateInstance<TestItem>();
            WeakReference<TestItem> weakRef = new WeakReference<TestItem>(item);

            // Destroy the target — simulates Unity unloading
            Object.DestroyImmediate(item);

            // Resolve should detect the destroyed object and try to reload
            // (will fail with invalid guid, returning null)
            LogAssert.ignoreFailingMessages = true;
            var result = ScriptableObjectRegistry.Resolve(ref weakRef, "invalid-guid");
            LogAssert.ignoreFailingMessages = false;

            Assert.IsNull(result);
        }

        [Test]
        public void Resolve_SetsWeakRefAfterLoad()
        {
            WeakReference<TestItem> weakRef = null;

            // Will try to load, fail, return null — but weakRef should be set
            LogAssert.ignoreFailingMessages = true;
            ScriptableObjectRegistry.Resolve(ref weakRef, "invalid-guid");
            LogAssert.ignoreFailingMessages = false;

            Assert.IsNotNull(weakRef);
        }

        // --- LazyLoadReference does not prevent unloading ---

        [Test]
        public void UnloadUnusedAssets_ClearsUnreferencedAsset()
        {
            // Baseline: verify that UnloadUnusedAssetsImmediate actually works
            // for a persisted asset with no strong references.
            const string assetPath = "Assets/SOCUnloadBaselineTemp.asset";

            var collection = ScriptableObject.CreateInstance<TestCollection>();
            AssetDatabase.CreateAsset(collection, assetPath);
            AssetDatabase.SaveAssets();

            int instanceId = collection.GetInstanceID();
            collection = null;

            EditorUtility.UnloadUnusedAssetsImmediate();

            var reloaded = EditorUtility.InstanceIDToObject(instanceId);
            AssetDatabase.DeleteAsset(assetPath);

            Assert.IsTrue(reloaded == null,
                "UnloadUnusedAssetsImmediate should unload a persisted asset with no strong references");
        }

        [Test]
        public void LazyLoadReference_DoesNotPreventUnload()
        {
            // With the baseline working, verify LazyLoadReference doesn't keep it alive.
            const string assetPath = "Assets/SOCLazyLoadTestTemp.asset";

            var collection = ScriptableObject.CreateInstance<TestCollection>();
            AssetDatabase.CreateAsset(collection, assetPath);
            AssetDatabase.SaveAssets();

            var lazyRef = new LazyLoadReference<TestCollection> { asset = collection };
            int instanceId = collection.GetInstanceID();
            collection = null;

            EditorUtility.UnloadUnusedAssetsImmediate();

            var reloaded = EditorUtility.InstanceIDToObject(instanceId);
            AssetDatabase.DeleteAsset(assetPath);

            Assert.IsTrue(reloaded == null,
                "LazyLoadReference should not prevent UnloadUnusedAssets from collecting the asset");
        }

        // --- FindAll returns non-null even on failure ---

        [Test]
        public void FindAll_NeverReturnsNull()
        {
            LogAssert.ignoreFailingMessages = true;
            var result = ScriptableObjectCollection.FindAll();
            LogAssert.ignoreFailingMessages = false;

            Assert.IsNotNull(result);
        }

        // --- OfType returns non-null even on failure ---

        [Test]
        public void OfType_NeverReturnsNull()
        {
            LogAssert.ignoreFailingMessages = true;
            var result = ScriptableObjectCollection.OfType<TestItem>();
            LogAssert.ignoreFailingMessages = false;

            Assert.IsNotNull(result);
        }
    }
}
