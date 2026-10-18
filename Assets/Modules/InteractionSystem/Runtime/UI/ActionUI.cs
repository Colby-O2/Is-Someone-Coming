using InteractionSystem.Controls;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XInput;

namespace InteractionSystem
{
    [System.Serializable]
    internal class ActionUI
    {
        [SerializeField] private TMPro.TMP_Text _action;
        public MeshRenderer Key;

        public Material KeyboardMaterial;
        public Material PlayStationMaterial;
        public Material GenericGamepadMaterial;

        public string Action { get => _action.text; set => _action.text = value; }

        public void SetColor(Color color)
        {
            Key.material.color = color;
        }

        public bool IsActive() => _action.gameObject.activeSelf;

        public void SetActive(bool state) => _action.gameObject.SetActive(state);

        public void UpdateIconMaterial()
        {
            if (Key == null || InputDeviceHandler.Current == null) return;

            if (InputDeviceHandler.IsCurrentKeybaord)
            {
                Key.material = KeyboardMaterial;
            }
            else if (InputDeviceHandler.IsCurrentXboxLike)
            {
                Key.material = GenericGamepadMaterial;
            }
            else
            {
                Key.material = PlayStationMaterial;
            }
        }
    }
}
