using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.XInput;

using System.Linq;

namespace InteractionSystem.Controls
{
    public static class InputDeviceHandler
    {
        public static InputDevice Current { get; private set; }

        public static bool IsCurrentKeybaord => Current is Keyboard || Current is Mouse;
        public static bool IsCurrentGamepad => Current is Gamepad || Current is Joystick;
        public static bool IsCurrentXboxLike => Current is XInputController;

        private static void SetDevice(InputDevice device)
        {
            Current = device;
        }

        private static void OnUpdate(InputEventPtr eventPtr, InputDevice _)
        {
            if (!eventPtr.IsA<StateEvent>() && !eventPtr.IsA<DeltaStateEvent>())
                return;

            InputDevice device = InputSystem.GetDeviceById(eventPtr.deviceId);
            if (device == null) return;

            if (device is Gamepad gamepad)
            {
                bool buttonsPressed = false;
                foreach (InputControl control in gamepad.allControls)
                {
                    if (control is ButtonControl b && b.wasPressedThisFrame)
                    {
                        buttonsPressed = true;
                        break;
                    }
                }

                Vector2 leftStick = gamepad.leftStick.ReadValue();
                Vector2 rightStick = gamepad.rightStick.ReadValue();
                bool stickMoved = leftStick.sqrMagnitude > 0.01f || rightStick.sqrMagnitude > 0.01f;

                if (!buttonsPressed && !stickMoved) return;
                SetDevice(device);
            }
            else if (device is Joystick joystick)
            {
                bool anyButton = false;
                foreach (InputControl control in joystick.allControls)
                {
                    if (control is ButtonControl b && b.wasPressedThisFrame)
                    {
                        anyButton = true;
                        break;
                    }
                }

                bool moved = joystick.allControls.OfType<AxisControl>().Any(a => Mathf.Abs(a.ReadValue()) > 0.01f);

                if (!anyButton && !moved) return;
                SetDevice(device);
            }
            else if (device is Keyboard)
            {
                SetDevice(device);
            }
        }

        [RuntimeInitializeOnLoadMethod]
        private static void Init()
        {
            InputSystem.onEvent += OnUpdate;
        }
    }
}
