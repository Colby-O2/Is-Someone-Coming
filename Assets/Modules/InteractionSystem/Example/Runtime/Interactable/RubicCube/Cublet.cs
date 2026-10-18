using UnityEngine;

namespace InteractionSystem.Example.Interactables
{
    public class Cubelet : MonoBehaviour
    {
        public int ix, iy, iz; 
        [HideInInspector] public Transform cubelet; 

        private void Awake()
        {
            cubelet = transform;
        }
    }
}