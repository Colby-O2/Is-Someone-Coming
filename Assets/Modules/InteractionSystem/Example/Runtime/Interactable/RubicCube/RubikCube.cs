using UnityEngine;
using UnityEngine.InputSystem;

using System.Collections;
using System.Collections.Generic;
using InteractionSystem.UI;

namespace InteractionSystem.Example.Interactables
{
    public class RubiksController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private InteractorController _controller;
        [SerializeField] private Camera _playerCamera;

        [Header("Cube Settings")]
        [SerializeField] private int _cubeSize = 3;
        [SerializeField] private float _spacing = 2f;
        [SerializeField] private Transform _cubeHolder;
        [SerializeField] private bool _scrambleOnStart = true;
        [SerializeField] LayerMask _interactable;

        [Header("Rotation Settings")]
        [SerializeField] private float _rotationDuration = 0.3f;

        private CubeletData[,,] _cubeState;
        private Vector3 _centerOffset;

        private Cubelet _dragStartCubelet;
        private Vector2 _dragStartMouse;
        private Vector3 _dragStartNormal;
        private bool _dragging = false;
        [SerializeField] private float _dragThreshold = 10f;

        private bool _isSolved = false;
        private bool _isRotating = false;

        private class CubeletData
        {
            public int x, y, z;
            public Quaternion rotation;
            public Transform cubelet;

            public int homeX, homeY, homeZ;
            public Quaternion homeRotation;
        }

        public bool IsSolved()
        {
            int n = _cubeSize;

            for (int x = 0; x < n; x++)
                for (int y = 0; y < n; y++)
                    for (int z = 0; z < n; z++)
                    {
                        CubeletData c = _cubeState[x, y, z];
                        if (c == null) return false;

                        if (c.x != c.homeX || c.y != c.homeY || c.z != c.homeZ)
                            return false;

                        if (Quaternion.Angle(c.rotation, c.homeRotation) > 1f)
                            return false;
                    }
            return true;
        }

        private void Awake()
        {
            if (!_controller) _controller = FindAnyObjectByType<InteractorController>();

            _cubeState = new CubeletData[_cubeSize, _cubeSize, _cubeSize];
            _centerOffset = Vector3.one * (_cubeSize - 1) * _spacing * 0.5f;

            foreach (Transform cubelet in _cubeHolder)
            {
                Cubelet cl = cubelet.GetComponent<Cubelet>() ?? cubelet.gameObject.AddComponent<Cubelet>();

                int ix = Mathf.RoundToInt(cubelet.localPosition.x / _spacing);
                int iy = Mathf.RoundToInt(cubelet.localPosition.y / _spacing);
                int iz = Mathf.RoundToInt(cubelet.localPosition.z / _spacing);

                _cubeState[ix, iy, iz] = new CubeletData
                {
                    x = ix,
                    y = iy,
                    z = iz,
                    rotation = cubelet.localRotation,
                    cubelet = cubelet,
                    homeX = ix,
                    homeY = iy,
                    homeZ = iz,
                    homeRotation = cubelet.localRotation
                };
            }

            UpdateAllCubeletTransforms();

            if (_scrambleOnStart) ScrambleCube();

            UpdateHolderColliders();
        }
        private void Update()
        {
            if (!_isSolved && IsSolved()) OnSolved();
        }

        public void ScrambleCube(int moves = 20)
        {
            int N = _cubeSize;
            System.Random rng = new System.Random();

            for (int i = 0; i < moves; i++)
            {
                Vector3[] axes = { Vector3.right, Vector3.up, Vector3.forward };
                Vector3 axis = axes[rng.Next(axes.Length)];

                int sliceIndex = rng.Next(0, N);

                bool clockwise = rng.Next(2) == 0;

                List<CubeletData> slice = new List<CubeletData>();
                foreach (var c in _cubeState)
                {
                    if (axis == Vector3.right && c.x == sliceIndex) slice.Add(c);
                    else if (axis == Vector3.up && c.y == sliceIndex) slice.Add(c);
                    else if (axis == Vector3.forward && c.z == sliceIndex) slice.Add(c);
                }

                Quaternion rot = Quaternion.AngleAxis(clockwise ? 90f : -90f, axis);
                foreach (var c in slice)
                {
                    c.cubelet.localRotation = rot * c.cubelet.localRotation;
                    c.cubelet.localPosition = rot * (c.cubelet.localPosition);
                }

                CommitSliceState(slice);
            }
        }

        private void OnClickPerformed(InputAction.CallbackContext ctx)
        {
            if (_isRotating) return;

            Vector2 mousePos = VirtualCaster.GetVirtualMousePosition();

            if (!VirtualCaster.Instance.Raycast(mousePos, out RaycastHit hit, Mathf.Infinity, ~_interactable)) return;

            _dragStartCubelet = hit.transform.GetComponent<Cubelet>();

            if (_dragStartCubelet == null) return;
            
            _dragStartMouse = mousePos;
            _dragStartNormal = hit.normal;
            _dragging = true;
        }

        private void OnDragPerformed()
        {
            if (!_dragging || _dragStartCubelet == null || _isRotating) return;

            Vector2 currentMouse = VirtualCaster.GetVirtualMousePosition();
            Vector2 delta = currentMouse - _dragStartMouse;
            if (delta.magnitude < _dragThreshold) return;

            Vector3 dragCamera = new Vector3(delta.x, delta.y, 0f);
            Vector3 worldDrag = _playerCamera.transform.TransformDirection(dragCamera);

            Vector3 faceNormal = _dragStartNormal;
            Vector3 dragOnFace = Vector3.ProjectOnPlane(worldDrag, faceNormal).normalized;
            if (dragOnFace == Vector3.zero) return;

            Vector3 rotationAxis = Vector3.Cross(faceNormal, dragOnFace).normalized;

            Vector3 localAxis = _cubeHolder.InverseTransformDirection(rotationAxis);
            localAxis = GetClosestAxis(localAxis);

            bool clockwise = true;
            CanonicalizeAxis(ref localAxis, ref clockwise);

            Vector3Int idx = GetCubeletIndices(_dragStartCubelet.transform);
            int sliceIndex = localAxis == Vector3.right ? idx.x :
                             localAxis == Vector3.up ? idx.y :
                                                           idx.z;

            StartCoroutine(RotateSliceAnimated(sliceIndex, localAxis, clockwise));

            _dragging = false;
            _dragStartCubelet = null;
        }

        private void OnDragCanceled(InputAction.CallbackContext ctx)
        {
            _dragging = false;
            _dragStartCubelet = null;
        }

        private Vector3Int GetCubeletIndices(Transform cubelet)
        {
            for (int x = 0; x < _cubeSize; x++)
                for (int y = 0; y < _cubeSize; y++)
                    for (int z = 0; z < _cubeSize; z++)
                        if (_cubeState[x, y, z].cubelet == cubelet)
                            return new Vector3Int(x, y, z);
            return Vector3Int.zero;
        }

        private Vector3 GetClosestAxis(Vector3 dir)
        {
            Vector3[] axes = {
                Vector3.right, Vector3.up, Vector3.forward,
                -Vector3.right, -Vector3.up, -Vector3.forward
            };

            float maxDot = -1f;
            Vector3 closest = Vector3.zero;

            foreach (var axis in axes)
            {
                float dot = Vector3.Dot(dir, axis);
                if (dot > maxDot)
                {
                    maxDot = dot;
                    closest = axis;
                }
            }
            return closest;
        }

        private void CanonicalizeAxis(ref Vector3 axis, ref bool clockwise)
        {
            if (axis == -Vector3.right || axis == -Vector3.up || axis == -Vector3.forward)
            {
                axis = -axis;
                clockwise = !clockwise;
            }
        }

        private IEnumerator RotateSliceAnimated(int sliceIndex, Vector3 axis, bool clockwise)
        {
            _isRotating = true;

            List<CubeletData> slice = new List<CubeletData>();
            int N = _cubeSize;
            for (int x = 0; x < N; x++)
                for (int y = 0; y < N; y++)
                    for (int z = 0; z < N; z++)
                    {
                        CubeletData c = _cubeState[x, y, z];
                        if (axis == Vector3.up && c.y == sliceIndex) slice.Add(c);
                        else if (axis == Vector3.right && c.x == sliceIndex) slice.Add(c);
                        else if (axis == Vector3.forward && c.z == sliceIndex) slice.Add(c);
                    }

            GameObject pivotGO = new GameObject("SlicePivot");
            pivotGO.transform.SetParent(_cubeHolder);
            pivotGO.transform.localPosition = Vector3.zero;
            pivotGO.transform.localRotation = Quaternion.identity;

            foreach (var c in slice) c.cubelet.SetParent(pivotGO.transform);

            float angle = clockwise ? 90f : -90f;
            Quaternion startRot = pivotGO.transform.localRotation;
            Quaternion endRot = Quaternion.AngleAxis(angle, axis) * startRot;

            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / _rotationDuration;
                pivotGO.transform.localRotation = Quaternion.Slerp(startRot, endRot, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }
            pivotGO.transform.localRotation = endRot;

            foreach (var c in slice) c.cubelet.SetParent(_cubeHolder);
            Destroy(pivotGO);

            CommitSliceState(slice);
            _isRotating = false;
        }

        private void CommitSliceState(List<CubeletData> slice)
        {
            int N = _cubeSize;

            foreach (var c in slice)
            {
                Vector3 localPos = c.cubelet.localPosition + _centerOffset;
                int ix = Mathf.RoundToInt(localPos.x / _spacing);
                int iy = Mathf.RoundToInt(localPos.y / _spacing);
                int iz = Mathf.RoundToInt(localPos.z / _spacing);

                ix = Mathf.Clamp(ix, 0, N - 1);
                iy = Mathf.Clamp(iy, 0, N - 1);
                iz = Mathf.Clamp(iz, 0, N - 1);

                c.x = ix;
                c.y = iy;
                c.z = iz;
                c.rotation = c.cubelet.localRotation;
            }

            CubeletData[,,] newState = new CubeletData[N, N, N];
            foreach (var c in _cubeState)
            {
                if (c != null)
                    newState[c.x, c.y, c.z] = c;
            }
            _cubeState = newState;
        }

        private void UpdateAllCubeletTransforms()
        {
            int N = _cubeSize;
            for (int x = 0; x < N; x++)
                for (int y = 0; y < N; y++)
                    for (int z = 0; z < N; z++)
                    {
                        CubeletData c = _cubeState[x, y, z];
                        if (c.cubelet == null) continue;

                        Vector3 localPos = new Vector3(c.x, c.y, c.z) * _spacing - _centerOffset;
                        c.cubelet.localPosition = localPos;
                        c.cubelet.localRotation = c.rotation;
                    }
        }

        private void OnEnable()
        {
            _controller.Controls.InspectionClickAction.performed += OnClickPerformed;
            _controller.Controls.InspectionClickAction.canceled += OnDragCanceled;

            VirtualCaster.OnMouseDrag.AddListener(OnDragPerformed);
        }

        private void OnDisable()
        {
            _controller.Controls.InspectionClickAction.performed -= OnClickPerformed;
            _controller.Controls.InspectionClickAction.canceled -= OnDragCanceled;

            VirtualCaster.OnMouseDrag.RemoveListener(OnDragPerformed);
        }

        void UpdateHolderColliders()
        {
            float size = (_cubeSize - 1) * _spacing;

            foreach (Collider col in gameObject.GetComponents<Collider>())
            {
                if (col is BoxCollider box)
                {
                    box.center = Vector3.zero;
                    box.size = new Vector3(size, size, size);
                }
                else if (col is SphereCollider sphere)
                {
                    sphere.center = Vector3.zero;
                }
            }

            Physics.SyncTransforms();
        }

        private void OnSolved()
        {
            Debug.Log("You Solved Rubik Cube!");
            _isSolved = true;
        }
    }
}