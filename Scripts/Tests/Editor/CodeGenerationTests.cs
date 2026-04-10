using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace BrunoMikoski.ScriptableObjectCollections.Tests
{
    /// <summary>
    /// Tests for static access code generation.
    /// Creates real assets, generates the .g.cs file, and verifies its content.
    /// </summary>
    [TestFixture]
    public class CodeGenerationTests
    {
        private const string TestFolder = "Assets/SOCCodeGenTestTemp";
        private const string ItemsFolder = "Assets/SOCCodeGenTestTemp/Items";
        private const string ScriptsFolder = "Assets/SOCCodeGenTestTemp/Scripts";
        private TestCollection collection;
        private TestItem itemAlpha;
        private TestItem itemBeta;
        private string generatedFilePath;

        [SetUp]
        public void SetUp()
        {
            AssetDatabaseUtils.CreatePathIfDoesntExist(TestFolder);
            AssetDatabaseUtils.CreatePathIfDoesntExist(ItemsFolder);
            AssetDatabaseUtils.CreatePathIfDoesntExist(ScriptsFolder);

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

            // Configure settings
            SOCSettings.Instance.SetNamespaceForCollection(collection, "TestNamespace");
            SOCSettings.Instance.SetStaticFilenameForCollection(collection, "TestCodeGenCollectionStatic");
            SOCSettings.Instance.SetGeneratedScriptsParentFolder(collection,
                AssetDatabase.LoadAssetAtPath<DefaultAsset>(ScriptsFolder));

            generatedFilePath = Path.Combine(ScriptsFolder, "TestCodeGenCollectionStatic.g.cs");

            // Generate the static file (code gen uses AssetDatabase, not Addressables)
            LogAssert.ignoreFailingMessages = true;
            CodeGenerationUtility.GenerateStaticCollectionScript(collection);
            LogAssert.ignoreFailingMessages = false;

            // Re-resolve after code gen's Refresh
            itemAlpha = AssetDatabase.LoadAssetAtPath<TestItem>($"{ItemsFolder}/Alpha.asset");
            itemBeta = AssetDatabase.LoadAssetAtPath<TestItem>($"{ItemsFolder}/Beta.asset");
        }

        [TearDown]
        public void TearDown()
        {
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
        public void GenerateStaticFile_UsesAddressablesForCollection()
        {
            string content = File.ReadAllText(generatedFilePath);
            Assert.IsTrue(content.Contains("Addressables.LoadAssetAsync"), "Missing Addressables.LoadAssetAsync call");
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
        public void GenerateStaticFile_ContainsIsCollectionLoaded()
        {
            string content = File.ReadAllText(generatedFilePath);
            Assert.IsTrue(content.Contains("IsCollectionLoaded"), "Missing IsCollectionLoaded property");
        }

        [Test]
        public void GenerateStaticFile_ContainsUnloadCollection()
        {
            string content = File.ReadAllText(generatedFilePath);
            Assert.IsTrue(content.Contains("UnloadCollection"), "Missing UnloadCollection method");
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
            Assert.IsTrue(content.Contains("using UnityEngine.AddressableAssets;"), "Missing Addressables using");
            Assert.IsTrue(content.Contains("using System;"), "Missing System using");
        }
    }
}
