using UnityEngine;

using PlazmaGames.Core;

using ColbyO.SJ26.GameLogic;

namespace ColbyO.SJ26
{
    public class NotifyInside : MonoBehaviour
    {
        [SerializeField] private string _id;
        [SerializeField] private string _targetTag;

        private void OnTriggerEnter(Collider other)
        {
            Debug.Log("Enter");
            if (other.CompareTag(_targetTag))
            {
                GameManager.GetMonoSystem<IGameLogicMonoSystem>().SetInRange(_id, true);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            Debug.Log("Exit");
            if (other.CompareTag(_targetTag))
            {
                GameManager.GetMonoSystem<IGameLogicMonoSystem>().SetInRange(_id, false);
            }
        }
    }
}