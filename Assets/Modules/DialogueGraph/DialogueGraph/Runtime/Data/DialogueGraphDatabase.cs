using DialogueGraph.SO;
using UnityEngine;

namespace DialogueGraph.Data
{
    [CreateAssetMenu(fileName = "NewDialogueGraphDatabase", menuName = "Dialogue/Graph Database")]
    public class DialogueGraphDatabase : SODatabase<DialogueGraphSO>
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        public static void Initialize()
        {
            SODatabase<DialogueGraphSO> [] databases = Resources.LoadAll<SODatabase<DialogueGraphSO>>("");
            foreach (SODatabase<DialogueGraphSO> database in databases) database.InitDatabase();
        }
    }
}
