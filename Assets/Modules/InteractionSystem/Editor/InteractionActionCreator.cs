#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace InteractionSystem.Editor
{
    public static class InteractionActionCreator
    {
        [MenuItem("Assets/Create/Scripting/Interaction Action", false, 80)]
        public static void CreateInteractionAction()
        {
            string path = GetSelectedPathOrFallback();

            string filePath = EditorUtility.SaveFilePanelInProject(
                "Create Interaction Action",
                "NewAction",
                "cs",
                "Enter a name for the Action",
                path
            );

            if (string.IsNullOrEmpty(filePath))
                return;

            string className = Path.GetFileNameWithoutExtension(filePath);
            string directory = Path.GetDirectoryName(filePath);
            string nameSpace = GetNamespace(directory);

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
            string asmNamespace = GetNamespaceFromAsmdef(folder);
            if (!string.IsNullOrEmpty(asmNamespace))
                return asmNamespace;

            return EditorSettings.projectGenerationRootNamespace;
        }

        private static void CreateClass(string path, string name, string ns)
        {
            string filePath = Path.Combine(path, name + ".cs");

            string body =
    $@"[System.Serializable]
public class {name} : InteractionAction
{{
    public override string ActionName => ""{name.Replace("Action", "")}"";

    public override void Execute(InteractorController interactor)
    {{
        // TODO: Implement
    }}
}}";

            string code =
    $@"using InteractionSystem;
using InteractionSystem.Actions;
using UnityEngine;

{WrapInNamespace(ns, Indent(body, 0))}";

            File.WriteAllText(filePath, code);
        }

        private static string WrapInNamespace(string ns, string code)
        {
            if (string.IsNullOrWhiteSpace(ns))
                return code;

            return $"namespace {ns}\n{{\n{Indent(code.TrimEnd(), 1)}\n}}";
        }

        private static string Indent(string text, int level)
        {
            string indent = new string(' ', level * 4);
            string[] lines = text.Split(new[] { '\n' }, System.StringSplitOptions.None);

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
#endif