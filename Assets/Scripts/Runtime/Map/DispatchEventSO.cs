using DialogueGraph.SO;
using UnityEngine;

namespace ColbyO.SJ26
{

    [CreateAssetMenu(fileName = "DispatchEventDatabase", menuName = "SJ26/DispatchEventDatabase")]
    public class DispatchEventDatabase : SODatabase<DispatchEventSO>
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        public static void Initialize()
        {
            SODatabase<DispatchEventSO> [] databases = Resources.LoadAll<SODatabase<DispatchEventSO>>("");
            foreach (SODatabase<DispatchEventSO> database in databases) database.InitDatabase();
        }
    }
    
    [CreateAssetMenu(fileName = "DispatchEventSO", menuName = "SJ26/DispatchEventSO")]
    public class DispatchEventSO : BaseSO
    {
        public string CallDialogue;
        public string SuccessDialogue;
        public string FailedDialogue;
        public int RequiredPolice;
        public int RequiredEMS;
        public int RequiredFire;
        public int RequiredRescue;
        public float TimeToRespond;
        public float TimeToResolve;
    }
}
