using UnityEngine;


namespace ColbyO.SJ26
{
    [CreateAssetMenu(fileName = "DefaultPreferences", menuName = "SJ26/Preferences")]
    public class Preferences : ScriptableObject
    {
        public Texture2D Cursor;
        public float DetachedHeadTransitionTime = 1f;
    }
}