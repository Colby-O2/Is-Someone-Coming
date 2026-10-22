using UnityEngine;

using System.Collections.Generic;

using PlazmaGames.SO;

namespace ColbyO.SJ26
{
    
    [CreateAssetMenu(fileName = "DispatchEventSO", menuName = "SJ26/DispatchEvents/DispatchEventSO")]
    public class DispatchEventSO : BaseSO
    {
        public string CallDialogue;
        public string SuccessDialogue;
        public string FailedDialogue;
        public List<string> LocDescriptions = new List<string>();
        public int RequiredPolice;
        public int RequiredEMS;
        public int RequiredFire;
        public int RequiredRescue;
        public float TimeToRespond;
        public float TimeToResolve;
    }
}
