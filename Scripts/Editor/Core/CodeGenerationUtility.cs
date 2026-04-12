using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public static class CodeGenerationUtility
    {
        private const string PrivateValuesName = "cachedValues";
        private const string PublicValuesName = "Values";
        private const string ExtensionNew = ".g.cs";


        public static bool CreateNewScript(
            string fileName, string parentFolder, string nameSpace, string[] directives, params string[] lines)
        {
            parentFolder = parentFolder.ToPathWithConsistentSeparators();

            // Make sure the folder exists.
            AssetDatabaseUtils.CreatePathIfDoesntExist(parentFolder);

            // Check that the file doesn't exist yet.
            string finalFilePath = Path.Combine(parentFolder, $"{fileName}.cs");
            if (File.Exists(Path.GetFullPath(finalFilePath)))
                return false;

            using StreamWriter writer = new StreamWriter(finalFilePath);
            int indentation = 0;

            if (directives != null && directives.Length > 0)
            {
                foreach (string directive in directives)
                {
                    if (string.IsNullOrWhiteSpace(directive))
                        continue;
                    writer.WriteLine($"using {directive};");
                }
                writer.WriteLine();
            }

            bool hasNameSpace = !string.IsNullOrEmpty(nameSpace);
            if (hasNameSpace)
            {
                writer.WriteLine($"namespace {nameSpace}");
                writer.WriteLine("{");
                indentation++;
            }

            if (lines != null)
            {
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];
                    line = line.TrimStart();

                    if (line == "}")
                        indentation--;

                    writer.WriteLine(GetIndentation(indentation) + line);

                    if (line == "{")
                        indentation++;
                }
            }

            if (hasNameSpace)
                writer.WriteLine("}");

            writer.Close();

            return true;
        }

        public static bool CreateNewScript(string fileName, string parentFolder, string nameSpace,
            string classAttributes, string classDeclarationString, string[] innerContent, params string[] directives)
        {
            List<string> lines = new List<string>();
            int indentation = 0;

            if (!string.IsNullOrEmpty(classAttributes))
                lines.Add($"{GetIndentation(indentation)}{classAttributes}");
            lines.Add($"{GetIndentation(indentation)}{classDeclarationString}");

            lines.Add(GetIndentation(indentation) + "{");
            indentation++;

            if (innerContent != null)
            {
                foreach (string content in innerContent)
                {
                    if (content == "}")
                        indentation--;
                    lines.Add(GetIndentation(indentation) + content);
                    if (content == "{")
                        indentation++;
                }
            }

            indentation--;
            lines.Add(GetIndentation(indentation) + "}");

            return CreateNewScript(fileName, parentFolder, nameSpace, directives, lines.ToArray());
        }

        private static string GetIndentation(int indentation)
        {
            StringBuilder stringBuilder = new StringBuilder();
            for (int i = 0; i < indentation; i++)
            {
                stringBuilder.Append("    ");
            }
            return stringBuilder.ToString();
        }

        private static void AppendHeader(StreamWriter writer, ref int indentation, string nameSpace, string classAttributes, string className, bool isStatic, params string[] directives)
        {
            writer.WriteLine("//  Automatically generated");
            writer.WriteLine("//");
            writer.WriteLine();
            for (int i = 0; i < directives.Length; i++)
            {
                string directive = directives[i];
                if (string.IsNullOrEmpty(directive))
                    continue;
                writer.WriteLine($"using {directive};");
            }

            writer.WriteLine();

            bool hasNameSpace = !string.IsNullOrEmpty(nameSpace);
            if (hasNameSpace)
            {
                writer.WriteLine($"namespace {nameSpace}");
                writer.WriteLine("{");
                indentation++;
            }

            if (!string.IsNullOrEmpty(classAttributes))
                writer.WriteLine($"{GetIndentation(indentation)}{classAttributes}");

            string finalClassDeclaration = "";
            finalClassDeclaration += GetIndentation(indentation);
            finalClassDeclaration += "public ";
            if (isStatic)
                finalClassDeclaration += "static ";
            finalClassDeclaration += "partial ";
            finalClassDeclaration += "class ";
            finalClassDeclaration += className;

            writer.WriteLine(finalClassDeclaration);
            writer.WriteLine(GetIndentation(indentation) + "{");
            indentation++;
        }

        public static void AppendLine(StreamWriter writer, int indentation, string input = "")
        {
            writer.WriteLine($"{GetIndentation(indentation)}{input}");
        }

        public static void AppendFooter(StreamWriter writer, ref int indentation, string nameSpace)
        {
            bool hasNameSpace = !string.IsNullOrEmpty(nameSpace);
            if (hasNameSpace)
            {
                writer.WriteLine($"{GetIndentation(indentation)}" + "}");
                indentation--;
                writer.WriteLine($"{GetIndentation(indentation)}" + "}");
            }
            else
            {
                indentation--;
                writer.WriteLine($"{GetIndentation(indentation)}" + "}");
            }
        }

        /// <summary>
        /// Get the folder where the collection's script lives.
        /// Generated code goes next to it.
        /// </summary>
        public static string GetCollectionScriptFolder(ScriptableObjectCollection collection)
        {
            MonoScript script = MonoScript.FromScriptableObject(collection);
            if (script != null)
            {
                string scriptPath = AssetDatabase.GetAssetPath(script);
                if (!string.IsNullOrEmpty(scriptPath))
                    return Path.GetDirectoryName(scriptPath);
            }
            // Fallback: next to the collection asset itself
            return Path.GetDirectoryName(AssetDatabase.GetAssetPath(collection));
        }

        public static void GenerateStaticCollectionScript(ScriptableObjectCollection collection)
        {
            if (!CanGenerateStaticFile(collection, out string errorMessage))
            {
                Debug.LogError(errorMessage);
                return;
            }

            string fileName = SOCSettings.Instance.GetStaticFilenameForCollection(collection);
            string nameSpace = SOCSettings.Instance.GetNamespaceForCollection(collection);
            string finalFolder = GetCollectionScriptFolder(collection);

            // Always partial — generated code is next to the collection script, same assembly
            bool useBaseClass = SOCSettings.Instance.GetUseBaseClassForItem(collection);

            AssetDatabaseUtils.CreatePathIfDoesntExist(finalFolder);

            string finalFileName = Path.Combine(finalFolder, fileName);
            finalFileName += ExtensionNew;
            using (StreamWriter writer = new StreamWriter(finalFileName))
            {
                int indentation = 0;

                List<string> directives = new List<string>();
                directives.Add(collection.GetType().Namespace);
                directives.Add(typeof(List<>).Namespace);
                directives.Add("System");
                directives.Add("System.Threading.Tasks");
                directives.Add("UnityEngine.AddressableAssets");
                directives.Add("UnityEngine.ResourceManagement.AsyncOperations");
                directives.AddRange(GetCollectionDirectives(collection));

                string className = collection.GetItemType().Name;

                if (className.Equals(nameof(ScriptableObject)))
                {
                    Debug.LogWarning($"Cannot create static class using the collection type name ({nameof(ScriptableObject)})" +
                        $"The \"Static File Name\" ({fileName}) will be used as its class name instead.");
                    className = fileName;
                }

                AppendHeader(writer, ref indentation, nameSpace, "", className,
                    false, directives.Distinct().ToArray()
                );

                WriteCollectionAccessors(collection, writer, ref indentation, useBaseClass);

                indentation--;
                AppendFooter(writer, ref indentation, nameSpace);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static bool CanGenerateStaticFile(ScriptableObjectCollection collection, out string errorMessage)
        {
            List<ScriptableObjectCollection> collectionsOfSameType = ScriptableObjectCollection.FindByItemType(collection.GetItemType());
            if (collectionsOfSameType.Count > 1)
            {
                for (int i = 0; i < collectionsOfSameType.Count; i++)
                {
                    ScriptableObjectCollection collectionA = collectionsOfSameType[i];
                    string targetNamespaceA = SOCSettings.Instance.GetNamespaceForCollection(collectionA);
                    string targetFileNameA = SOCSettings.Instance.GetStaticFilenameForCollection(collectionA);

                    for (int j = 0; j < collectionsOfSameType.Count; j++)
                    {
                        if (i == j)
                            continue;

                        ScriptableObjectCollection collectionB = collectionsOfSameType[j];
                        string targetNamespaceB = SOCSettings.Instance.GetNamespaceForCollection(collectionB);
                        string targetFileNameB = SOCSettings.Instance.GetStaticFilenameForCollection(collectionB);

                        if (targetFileNameA.Equals(targetFileNameB, StringComparison.Ordinal)
                            && targetNamespaceA.Equals(targetNamespaceB, StringComparison.Ordinal))
                        {
                            errorMessage =
                                $"Two collections ({collectionA.name} and {collectionB.name}) with the same name and namespace already exist, please use custom ones";
                            return false;
                        }
                    }
                }
            }

            errorMessage = String.Empty;
            return true;
        }

        private static string[] GetCollectionDirectives(ScriptableObjectCollection collection)
        {
            HashSet<string> directives = new HashSet<string>();
            var items = collection.ItemsGeneric;
            for (int i = 0; i < items.Count; i++)
                directives.Add(items[i].GetType().Namespace);
            return directives.ToArray();
        }

        /// <summary>
        /// Generate collection Values property, individual item accessors, and UnloadCollection method.
        /// </summary>
        private static void WriteCollectionAccessors(ScriptableObjectCollection collection, StreamWriter writer,
            ref int indentation, bool useBaseClass)
        {
            string privateValuesName = $"{PrivateValuesName}_{collection.name}";
            string publicValuesName = $"{PublicValuesName}_{collection.name}";

            // Cached values field
            AppendLine(writer, indentation, $"private static {collection.GetType().FullName} {privateValuesName};");
            AppendLine(writer, indentation);

            // Cached item fields — use AssetDatabase to find items, not Addressables
            var items = collection.ItemsGeneric;
            for (int i = 0; i < items.Count; i++)
            {
                ScriptableObject collectionItem = items[i];
                Type type = useBaseClass ? collection.GetItemType() : collectionItem.GetType();
                AppendLine(writer, indentation,
                    $"private static {type.FullName} cached{collectionItem.name.Sanitize().FirstToUpper()};");
            }

            AppendLine(writer, indentation);

            // Values property - loads collection via Addressables on first access
            string collectionAddress = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(collection));
            AppendLine(writer, indentation,
                $"public static {collection.GetType().FullName} {publicValuesName}");
            AppendLine(writer, indentation, "{");
            indentation++;
            AppendLine(writer, indentation, "get");
            AppendLine(writer, indentation, "{");
            indentation++;
            AppendLine(writer, indentation, $"if ({privateValuesName} == null)");
            indentation++;
            AppendLine(writer, indentation,
                $"{privateValuesName} = Addressables.LoadAssetAsync<{collection.GetType().FullName}>(\"{collectionAddress}\").WaitForCompletion();");
            indentation--;
            AppendLine(writer, indentation, $"return {privateValuesName};");
            indentation--;
            AppendLine(writer, indentation, "}");
            indentation--;
            AppendLine(writer, indentation, "}");
            AppendLine(writer, indentation);

            // Individual item properties — load directly via Addressables using the asset GUID
            for (int i = 0; i < items.Count; i++)
            {
                ScriptableObject collectionItem = items[i];
                string collectionNameFirstUpper = collectionItem.name.Sanitize().FirstToUpper();
                string privateStaticCachedName = $"cached{collectionNameFirstUpper}";
                Type type = useBaseClass ? collection.GetItemType() : collectionItem.GetType();

                if (collectionItem is not ISOCItem)
                    continue;

                string assetPath = AssetDatabase.GetAssetPath(collectionItem);
                string assetGuid = AssetDatabase.AssetPathToGUID(assetPath);

                AppendLine(writer, indentation, $"public static {type.FullName} {collectionNameFirstUpper}");
                AppendLine(writer, indentation, "{");
                indentation++;
                AppendLine(writer, indentation, "get");
                AppendLine(writer, indentation, "{");
                indentation++;
                AppendLine(writer, indentation, $"if ({privateStaticCachedName} == null)");
                indentation++;
                AppendLine(writer, indentation,
                    $"{privateStaticCachedName} = Addressables.LoadAssetAsync<{type.FullName}>(\"{assetGuid}\").WaitForCompletion();");
                indentation--;
                AppendLine(writer, indentation, $"return {privateStaticCachedName};");
                indentation--;
                AppendLine(writer, indentation, "}");
                indentation--;
                AppendLine(writer, indentation, "}");
                AppendLine(writer, indentation);
            }

            // IsCollectionLoaded property
            AppendLine(writer, indentation, $"public static bool IsCollectionLoaded => {privateValuesName} != null && {privateValuesName}.IsLoaded;");
            AppendLine(writer, indentation);

            // UnloadCollection method
            AppendLine(writer, indentation, "public static void UnloadCollection()");
            AppendLine(writer, indentation, "{");
            indentation++;
            AppendLine(writer, indentation, $"if ({privateValuesName} != null)");
            indentation++;
            AppendLine(writer, indentation, $"{privateValuesName}.Unload();");
            indentation--;
            AppendLine(writer, indentation, $"{privateValuesName} = null;");

            for (int i = 0; i < items.Count; i++)
            {
                ScriptableObject collectionItem = items[i];
                string collectionNameFirstUpper = collectionItem.name.Sanitize().FirstToUpper();
                AppendLine(writer, indentation, $"cached{collectionNameFirstUpper} = null;");
            }

            indentation--;
            AppendLine(writer, indentation, "}");
            AppendLine(writer, indentation);
        }

    }
}
