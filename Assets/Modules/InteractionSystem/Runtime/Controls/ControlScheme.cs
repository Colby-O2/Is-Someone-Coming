using UnityEngine;
using UnityEngine.InputSystem;

namespace InteractionSystem.Controls
{
    [CreateAssetMenu(fileName = "DefaultInteractionControls", menuName = "Interaction System/ControlScheme")]
    public sealed class ControlScheme : ScriptableObject
    {
        [Header("Interact Actions")]
        public InputAction Slot1InteractAction;
        public InputAction Slot2InteractAction;
        public InputAction Slot3InteractAction;
        public InputAction Slot4InteractAction;

        [Header("Inspection Actions")]
        public InputAction InspectionClickAction;
        public InputAction InspectionCursorAction;
        public InputAction InspectionZoomAction;
        public InputAction InspectionRotateAction;
        public InputAction InspectionReadAction;
        public InputAction InspectionLeaveAction;
        public InputAction InspectionNextAction;
        public InputAction InspectionBackAction;

        [Header("Moving Actions")]
        public InputAction DropAction;

        [Header("Keybaord Sensitivity")]
        public float KeybaordMouseSensitivity = 10f;
        public float KeybaordRotateSensitivity = 1f;
        public float KeybaordZoomSensitivity = 1f;

        [Header("Controller Sensitivity")]
        public float ControllerMouseSensitivity = 10f;
        public float ControllerRotateSensitivity = 1f;
        [Range(0f, 1f)] public float ControllerZoomSensitivity = 0.1f;

        public void Enable()
        {
            Slot1InteractAction.Enable();
            Slot2InteractAction.Enable();
            Slot3InteractAction.Enable();
            Slot4InteractAction.Enable();

            InspectionClickAction.Enable();
            InspectionCursorAction.Enable();
            InspectionZoomAction.Enable();
            InspectionLeaveAction.Enable();
            InspectionReadAction.Enable();
            InspectionRotateAction.Enable();
            InspectionNextAction.Enable();
            InspectionBackAction.Enable();

            DropAction.Enable();
        }

        public void Disable()
        {
            Slot1InteractAction.Disable();
            Slot2InteractAction.Disable();
            Slot3InteractAction.Disable();
            Slot4InteractAction.Disable();

            InspectionClickAction.Disable();
            InspectionCursorAction.Disable();
            InspectionZoomAction.Disable();
            InspectionLeaveAction.Disable();
            InspectionReadAction.Disable();
            InspectionRotateAction.Disable();
            InspectionNextAction.Disable();
            InspectionBackAction.Disable();

            DropAction.Disable();
        }
    }
}
