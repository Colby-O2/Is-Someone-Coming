using UnityEngine;


namespace ColbyO.SJ26
{
    [CreateAssetMenu(fileName = "DefaultPreferences", menuName = "SJ26/Preferences")]
    public class Preferences : ScriptableObject
    {
        public Texture2D Cursor;
        public float DetachedHeadTransitionTime = 1f;
        public Texture2D SuccessMapIcon;
        public Texture2D FailedMapIcon;
        public DispatchEventDatabase DispatchEvents;
        public float MinCallTime = 10;
        public float MaxCallTime = 20;
        public Color CoffeeColor = Color.brown;
        public float CoffeeBrewTime = 8.0f;
        public MovementSettings Movement;

        public float RandomCallTime() => Random.Range(MinCallTime, MaxCallTime);
    }
}