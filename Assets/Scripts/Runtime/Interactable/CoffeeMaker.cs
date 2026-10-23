using System;
using ColbyO.SJ26.GameLogic;
using PlazmaGames.Core;
using UnityEngine;

namespace ColbyO.SJ26
{
    public class CoffeeMaker : MonoBehaviour
    {
        [SerializeField] private Transform _mugSpot;
        [SerializeField] private GameObject _drip;
        [SerializeField] private AudioSource _dripSound;

        private void Awake()
        {
        }

        public void TakeMug(Mug mug)
        {
            if (mug.IsBrewed || mug.IsBrewing) return;
            
            SJ26GameManager.PlayerInteractor.GetMoveableHandler().EndMove();
            
            Rigidbody rb = mug.GetComponent<Rigidbody>();
            rb.isKinematic = true;
            mug.transform.position = _mugSpot.position;
            mug.IsBrewing = true;
            _drip.SetActive(true);
            _dripSound.Play();
            SJ26GameManager.GlobalScheduler.Every(0, () =>
            {
                mug.transform.position = _mugSpot.position;
                return mug.IsBrewing;
            });
            SJ26GameManager.GlobalScheduler.Wait(SJ26GameManager.Preferences.CoffeeBrewTime).Then(_ =>
            {
                var collider = GetComponent<SphereCollider>();
                if (collider) collider.enabled = false;
                _drip.SetActive(false);
                mug.IsBrewing = false;
                mug.IsBrewed = true;
                mug.SetFilled();
                rb.isKinematic = false;
                GameManager.GetMonoSystem<IGameLogicMonoSystem>().SetInRange("CoffeeBrewed", true);
            });
        }
    }
}
