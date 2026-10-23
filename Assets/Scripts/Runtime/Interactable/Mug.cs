using System;
using InteractionSystem;
using UnityEngine;

namespace ColbyO.SJ26
{
    public class Mug : MonoBehaviour
    {
        public bool IsBrewing = false;
        public bool IsBrewed = false;
        
        private MeshRenderer _meshRenderer;

        void Awake()
        {
            _meshRenderer = GetComponent<MeshRenderer>();
        }

        public void SetFilled()
        {
            _meshRenderer.materials[1].SetColor("_BaseColor", SJ26GameManager.Preferences.CoffeeColor);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.TryGetComponent<CoffeeMaker>(out var cm))
            {
                cm.TakeMug(this);
            }
            else if (other.CompareTag("MyDesk") && IsBrewed)
            {
                GetComponent<Interactable>().CanInteract = false;
                SJ26GameManager.PlayerInteractor.GetMoveableHandler().EndMove();
            }
        }
    }
}
