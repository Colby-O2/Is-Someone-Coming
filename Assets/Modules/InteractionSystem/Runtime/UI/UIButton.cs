using InteractionSystem.Controls;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XInput;
using UnityEngine.UI;

namespace InteractionSystem.UI
{
    [System.Serializable]
    public class UIIcon
    {
        public GameObject Key;
        public Image Icon;

        public Sprite KeybaordIcon;
        public Sprite PlaystationIcon;
        public Sprite GenericGamepadIcon;

        public bool IsActive() => Key.activeSelf;

        public void SetActive(bool state)
        {
            if (Key.activeSelf != state) Key.SetActive(state);
        }

        public void UpdateIconMaterial()
        {
            if (Key == null || InputDeviceHandler.Current == null) return;

            if (InputDeviceHandler.IsCurrentKeybaord)
            {
                Icon.sprite = KeybaordIcon;
            }
            else if (InputDeviceHandler.IsCurrentXboxLike)
            {
                Icon.sprite = GenericGamepadIcon;
            }
            else
            {
                Icon.sprite = PlaystationIcon;
            }
        }
    }
}
