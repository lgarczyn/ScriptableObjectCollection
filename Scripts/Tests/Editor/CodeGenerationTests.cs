using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections.Tests
{
    /// <summary>
    /// Tests for static access code generation.
    /// Creates real assets once, bakes GUIDs, generates the .g.cs file, and verifies its content.
    /// </summary>
    [TestFixture]
    public class CodeGenerationTests
    {
        private const string TestFolder = "Assets/SOCCodeGenTestTemp";
        private const string ItemsFolder = "Assets/SOCCodeGenTestTemp/Items";
        private static TestCollection collection;
        private static TestItem itemAlpha;
        private static TestItem itemBeta;
        private static string generatedFilePath;

        private static void BakeGuid(Object asset)
        {
            string path = AssetDatabase.GetAssetPath(asset);
            string guid = AssetDatabase.AssetPathToGUID(path);
            var so = new SerializedObject(asset);
            var prop = so.FindProperty("m_Guid");
            if (prop != null)
            {
                prop.stringValue = guid;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            AssetDatabaseUtils.CreatePathIfDoesntExist(TestFolder);
            AssetDatabaseUtils.CreatePathIfDoesntExist(ItemsFolder);
            collection = ScriptableObject.CreateInstance<TestCollection>();
            collection.name = "TestCodeGenCollection";
            AssetDatabase.CreateAsset(collection, $"{TestFolder}/TestCodeGenCollection.asset");

            itemAlpha = ScriptableObject.CreateInstance<TestItem>();
            itemAlpha.name = "Alpha";
            AssetDatabase.CreateAsset(itemAlpha, $"{ItemsFolder}/Alpha.asset");

            itemBeta = ScriptableObject.CreateInstance<TestItem>();
            itemBeta.name = "Beta";
            AssetDatabase.CreateAsset(itemBeta, $"{ItemsFolder}/Beta.asset");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Re-resolve after Refresh
            collection = AssetDatabase.LoadAssetAtPath<TestCollection>($"{TestFolder}/TestCodeGenCollection.asset");
            itemAlpha = AssetDatabase.LoadAssetAtPath<TestItem>($"{ItemsFolder}/Alpha.asset");
            itemBeta = AssetDatabase.LoadAssetAtPath<TestItem>($"{ItemsFolder}/Beta.asset");

            Assert.IsNotNull(collection, "Collection failed to load after Refresh");
            Assert.IsNotNull(itemAlpha, "Alpha item failed to load after Refresh");
            Assert.IsNotNull(itemBeta, "Beta item failed to load after Refresh");

            // Bake GUIDs
            BakeGuid(collection);
            BakeGuid(itemAlpha);
            BakeGuid(itemBeta);

            // Set up Addressables
            if (UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings != null)
            {
                string collectionGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(collection));
                SOCAddressableUtility.EnsureCollectionAddressable(collection, AssetDatabase.GetAssetPath(collection));
                SOCAddressableUtility.EnsureItemAddressable(AssetDatabase.GetAssetPath(itemAlpha), collectionGuid);
                SOCAddressableUtility.EnsureItemAddressable(AssetDatabase.GetAssetPath(itemBeta), collectionGuid);
            }

            // Configure settings
            SOCSettings.Instance.SetNamespaceForCollection(collection, "TestNamespace");
            SOCSettings.Instance.SetStaticFilenameForCollection(collection, "TestCodeGenCollectionStatic");

            // Generated file goes next to collection script (falls back to collection asset folder)
            string genFolder = CodeGenerationUtility.GetCollectionScriptFolder(collection);
            generatedFilePath = Path.Combine(genFolder, "TestCodeGenCollectionStatic.g.cs");

            // Generate the static file once
            CodeGenerationUtility.GenerateStaticCollectionScript(collection);

            // Re-resolve after code gen's Refresh
            itemAlpha = AssetDatabase.LoadAssetAtPath<TestItem>($"{ItemsFolder}/Alpha.asset");
            itemBeta = AssetDatabase.LoadAssetAtPath<TestItem>($"{ItemsFolder}/Beta.asset");
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            // Delete the generated .g.cs (may be outside TestFolder if script couldn't be found)
            if (!string.IsNullOrEmpty(generatedFilePath) && File.Exists(generatedFilePath))
            {
                File.Delete(generatedFilePath);
                string meta = generatedFilePath + ".meta";
                if (File.Exists(meta))
                    File.Delete(meta);
            }

            if (AssetDatabase.IsValidFolder(TestFolder))
                AssetDatabase.DeleteAsset(TestFolder);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [Test]
        public void GenerateStaticFile_CreatesFile()
        {
            Assert.IsTrue(File.Exists(generatedFilePath), $"Generated file not found at {generatedFilePath}");
        }

        [Test]
        public void GenerateStaticFile_ContainsNamespace()
        {
            string content = File.ReadAllText(generatedFilePath);
            Assert.IsTrue(content.Contains("namespace TestNamespace"), "Missing namespace declaration");
        }

        [Test]
        public void GenerateStaticFile_ContainsValuesProperty()
        {
            string content = File.ReadAllText(generatedFilePath);
            Assert.IsTrue(content.Contains("Values_TestCodeGenCollection"), "Missing Values property");
        }

        [Test]
        public void GenerateStaticFile_ContainsItemProperties()
        {
            string content = File.ReadAllText(generatedFilePath);
            Assert.IsTrue(content.Contains("Alpha"), "Missing Alpha item accessor");
            Assert.IsTrue(content.Contains("Beta"), "Missing Beta item accessor");
        }

        [Test]
        public void GenerateStaticFile_UsesWeakRefResolve()
        {
            string content = File.ReadAllText(generatedFilePath);
            Assert.IsTrue(content.Contains("ScriptableObjectRegistry.Resolve"), "Missing ScriptableObjectRegistry.Resolve call");
        }

        [Test]
        public void GenerateStaticFile_UsesAssetGUIDForItems()
        {
            string content = File.ReadAllText(generatedFilePath);

            string alphaGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(itemAlpha));
            string betaGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(itemBeta));

            Assert.IsTrue(content.Contains(alphaGuid), $"Missing Alpha asset GUID ({alphaGuid})");
            Assert.IsTrue(content.Contains(betaGuid), $"Missing Beta asset GUID ({betaGuid})");
        }

        [Test]
        public void GenerateStaticFile_ContainsCollectionAssetGUID()
        {
            string content = File.ReadAllText(generatedFilePath);
            string collectionGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(collection));
            Assert.IsTrue(content.Contains(collectionGuid), $"Missing collection asset GUID ({collectionGuid})");
        }


        [Test]
        public void GenerateStaticFile_ContainsAutoGeneratedHeader()
        {
            string content = File.ReadAllText(generatedFilePath);
            Assert.IsTrue(content.Contains("Automatically generated"), "Missing auto-generated header");
        }

        [Test]
        public void GenerateStaticFile_ContainsUsingDirectives()
        {
            string content = File.ReadAllText(generatedFilePath);
            Assert.IsTrue(content.Contains("using System;"), "Missing System using");
            Assert.IsTrue(content.Contains("using BrunoMikoski.ScriptableObjectCollections;"), "Missing SOC namespace using");
        }
    }
}
