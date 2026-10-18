using PlazmaGames.Utilities;
using UnityEngine;

namespace PlazmaGames.UI
{
    public abstract class View : MonoBehaviour
    {
        [Header("General View Settings")]
        [SerializeField] private bool _canChangeLayer = false;
        [SerializeField] private string _selectedLayer = "UI";
        [SerializeField] private string _unselectedLayer = "UI";

        /// <summary>
        /// Initialzes a view. 
        /// </summary>
        public abstract void Init();

        public virtual void SetLayer(bool isSelected)
        {
            if (!_canChangeLayer) return;

            if (isSelected) gameObject.SetGameLayerRecursive(LayerMask.NameToLayer(_selectedLayer));
            else gameObject.SetGameLayerRecursive(LayerMask.NameToLayer(_unselectedLayer));
        }

        /// <summary>
        /// Hides a view. 
        /// </summary>
        public virtual void Hide()
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Displays a view. 
        /// </summary>
        public virtual void Show()
        {
            gameObject.SetActive(true);
        }
    }
}
