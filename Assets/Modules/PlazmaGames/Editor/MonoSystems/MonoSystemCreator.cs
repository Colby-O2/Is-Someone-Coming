using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace PlazmaGames.Editor.MonoSystems
{
    public class MonoSystemCreator
    {
        [MenuItem("Assets/Create/Scripting/MonoSystem", false, 80)]
        public static void CreateMonoSystem()
        {
            string path = GetSelectedPathOrFallback();

            string filePath = EditorUtility.SaveFilePanelInProject(
                "Create MonoSystem",
                "NewMonoSystem",
                "cs",
                "Enter a name for the MonoSystem",
                path
            );

            if (string.IsNullOrEmpty(filePath))
                return;

            string className = Path.GetFileNameWithoutExtension(filePath);
            string directory = Path.GetDirectoryName(filePath);

            string nameSpace = GetNamespace(directory);

            CreateInterface(directory, className, nameSpace);
            CreateClass(directory, className, nameSpace);

            AssetDatabase.Refresh();
        }

        private static string GetNamespaceFromAsmdef(string folder)
        {
            var asmdefGuids = AssetDatabase.FindAssets("t:AssemblyDefinitionAsset", new[] { folder });
            if (asmdefGuids.Length == 0)
                return null;

            string asmdefPath = AssetDatabase.GUIDToAssetPath(asmdefGuids[0]);
            string json = File.ReadAllText(asmdefPath);

            const string key = "\"rootNamespace\":";
            int index = json.IndexOf(key);

            if (index == -1)
                return null;

            index += key.Length;

            int startQuote = json.IndexOf('"', index) + 1;
            int endQuote = json.IndexOf('"', startQuote);

            if (startQuote <= 0 || endQuote <= 0)
                return null;

            return json.Substring(startQuote, endQuote - startQuote);
        }

        private static string GetNamespace(string folder)
        {
            string asmdefNamespace = GetNamespaceFromAsmdef(folder);
            if (!string.IsNullOrEmpty(asmdefNamespace))
                return asmdefNamespace;

            return EditorSettings.projectGenerationRootNamespace;
        }

        private static void CreateInterface(string path, string name, string ns)
        {
            string interfaceName = $"I{name}";
            string filePath = Path.Combine(path, interfaceName + ".cs");

            string content = "using PlazmaGames.Core.MonoSystem;\n\n" +
                             WrapInNamespace(ns, $"public interface {interfaceName} : IMonoSystem\n{{\n}}");

            File.WriteAllText(filePath, content);
        }

        private static void CreateClass(string path, string name, string ns)
        {
            string filePath = Path.Combine(path, name + ".cs");

            string content = "using UnityEngine;\n\n" +
                             WrapInNamespace(ns, $"public class {name} : MonoBehaviour, I{name}\n{{\n}}");

            File.WriteAllText(filePath, content);
        }

        private static string WrapInNamespace(string ns, string code)
        {
            if (string.IsNullOrEmpty(ns))
                return code;

            string indentedCode = Indent(code.Trim(), 1);

            return $"namespace {ns}\n{{\n{indentedCode}\n}}";
        }

        private static string Indent(string text, int level)
        {
            string indent = new string(' ', level * 4);
            string[] lines = text.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(lines[i]))
                    lines[i] = indent + lines[i];
            }

            return string.Join("\n", lines);
        }

        private static string GetSelectedPathOrFallback()
        {
            string path = "Assets";

            if (Selection.activeObject != null)
            {
                path = AssetDatabase.GetAssetPath(Selection.activeObject);
            }

            if (!Directory.Exists(path))
            {
                path = Path.GetDirectoryName(path);
            }

            return path;
        }
    }
}