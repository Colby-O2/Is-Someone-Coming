using UnityEngine;

using PlazmaGames.SO;

namespace ColbyO.SJ26
{

    [CreateAssetMenu(fileName = "DispatchEventDatabase", menuName = "SJ26/DispatchEvent/DispatchEventDatabase")]
    public class DispatchEventDatabase : SODatabase<DispatchEventSO>
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        public static void Initialize()
        {
            SODatabase<DispatchEventSO>[] databases = Resources.LoadAll<SODatabase<DispatchEventSO>>("");
            foreach (SODatabase<DispatchEventSO> database in databases) database.InitDatabase();
        }
    }
}
