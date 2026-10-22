using UnityEngine;

namespace ColbyO.SJ26.Map
{
    [CreateAssetMenu(fileName = "MapObjectSO", menuName = "SJ26/Map/MapObjectSO")]
    public class MapObjectSO : ScriptableObject
    {
        [Header("Icon")]
        public Texture2D icon;
        public Color color;
        public int renderOrder = 0;
    }
}
