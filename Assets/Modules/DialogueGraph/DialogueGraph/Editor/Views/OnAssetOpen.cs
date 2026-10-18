using UnityEditor.Callbacks;
using UnityEditor;
using UnityEngine;
using DialogueGraph.Data;
using DialogueGraph.Editor.Views;

namespace DialogueGraph.Editor
{
    public class OnAssetOpen : MonoBehaviour
    {
        [OnOpenAsset]
        public static bool OpenGraphWindow(EntityId instanceID, int line)
        {
            if (!instanceID.IsValid()) return false;

            UnityEngine.Object obj = EditorUtility.EntityIdToObject(instanceID);

            if (obj is DialogueGraphSO so)
            {
                var window = EditorWindow.GetWindow<DialogueGraphEditorWindow>();
                window.LoadData(so);
                window.Show();
                return true; 
            }

            return false;
        }
    }
}
