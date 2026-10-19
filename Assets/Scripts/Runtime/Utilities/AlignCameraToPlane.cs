using PlazmaGames.Core.Debugging;
using UnityEngine;

namespace ColbyO.SJ26.Utilities
{
    [ExecuteAlways]
    public class AlignCameraToPlane : MonoBehaviour
    {
        [SerializeField] private Camera _targetCamera;
        [SerializeField] private Transform _targetPlane;
        [SerializeField] private float _cameraDistance = 10f;
        [SerializeField] private bool _fitEntirePlane = true;

        private void Reset()
        {
            _targetCamera = GetComponent<Camera>();
        }

        [ContextMenu("Align To Plane")]
        public void Align()
        {
            if (_targetCamera == null)
                _targetCamera = Camera.main;

            if (_targetCamera == null || _targetPlane == null)
            {
                PlazmaDebug.LogWarning("Missing Camera or Target Plane reference.", "AlignCameraToPlane");
                return;
            }

            _targetCamera.orthographic = true;

            float planeWorldWidth = 10f * _targetPlane.lossyScale.x;
            float planeWorldHeight = 10f * _targetPlane.lossyScale.z;

            Vector3 planeNormal = _targetPlane.up;
            Vector3 planeUp = _targetPlane.forward;

            _targetCamera.transform.rotation = Quaternion.LookRotation(-planeNormal, planeUp);

            _targetCamera.transform.position = _targetPlane.position + (planeNormal * _cameraDistance);

            float screenAspect = _targetCamera.aspect;
            float planeAspect = planeWorldWidth / planeWorldHeight;

            float orthoSizeForHeight = planeWorldHeight * 0.5f;
            float orthoSizeForWidth = (planeWorldWidth * 0.5f) / screenAspect;

            if (_fitEntirePlane)
            {
                _targetCamera.orthographicSize = Mathf.Max(orthoSizeForHeight, orthoSizeForWidth);
            }
            else
            {
                _targetCamera.orthographicSize = Mathf.Min(orthoSizeForHeight, orthoSizeForWidth);
            }
        }
    }
}
