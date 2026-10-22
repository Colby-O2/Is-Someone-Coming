using System.Collections;
using UnityEngine;

namespace ColbyO.SJ26
{
    public static class Utils
    {
        public static IEnumerator DisableNextFrame(GameObject targetObj)
        {
            yield return null; 

            if (targetObj != null)
            {
                targetObj.SetActive(false);
            }
        }
    }
}