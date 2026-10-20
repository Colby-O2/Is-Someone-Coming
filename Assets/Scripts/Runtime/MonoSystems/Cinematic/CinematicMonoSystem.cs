using UnityEngine;
using UnityEngine.SceneManagement;

using System.Collections.Generic;

using PlazmaGames.Animation;
using PlazmaGames.Core;

using ColbyO.SJ26.Utilities;

namespace ColbyO.SJ26.Cinematic
{
    public class CinematicMonoSystem : MonoBehaviour, ICinematicMonoSystem
    {
        [Header("Camera Settings")]
        [SerializeField] private string _cinematicCameraTag = "MainCamera";

        private Dictionary<string, Transform> _cameraLocations = new Dictionary<string, Transform>();

        private CameraShake _cinematicCamera;
        private VelocityTracker _vel;

        private GameObject _cinematicView;
        private GameObject _playerCamera;

        private void Start()
        {
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoad;
            SceneManager.sceneUnloaded += OnSceneUnload;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoad;
            SceneManager.sceneUnloaded -= OnSceneUnload;
        }

        private void OnSceneLoad(Scene scene, LoadSceneMode mode)
        {
            _playerCamera = GameObject.FindWithTag("PlayerCamera");

            GameObject cinematicCameraGO = GameObject.FindGameObjectWithTag(_cinematicCameraTag);
            if (cinematicCameraGO != null)
            {
                _cinematicCamera = cinematicCameraGO.GetComponent<CameraShake>();
                _vel = cinematicCameraGO.GetComponent<VelocityTracker>();
            }

            _cinematicView = GameObject.FindWithTag("CinematicView");

            Disabe(false);
        }

        private void OnSceneUnload(Scene scene)
        {

        }

        public VelocityTracker GetCameraVelocity() => _vel;

        public void Enable(bool lockMovement = true)
        {
            if (_cinematicCamera && !_cinematicCamera.gameObject.activeSelf) _cinematicCamera.gameObject.SetActive(true);
            if (_cinematicView != null && !_cinematicView.activeSelf) _cinematicView.SetActive(true);

            if (_playerCamera.activeSelf) _playerCamera.SetActive(false);

            SJ26GameManager.LockMovement = lockMovement;
        }

        public void Disabe(bool setGlobalLock = true)
        {
            if (_cinematicCamera && _cinematicCamera.gameObject.activeSelf)
            {
                _cinematicCamera.transform.SetParent(null);
                _cinematicCamera.gameObject.SetActive(false);
            }
            if (_cinematicView != null && _cinematicView.activeSelf) _cinematicView.SetActive(false);

            if (setGlobalLock)
            {
                if (!_playerCamera.activeSelf) _playerCamera.SetActive(true);
                SJ26GameManager.LockMovement = false;
            }
        }

        private Transform GetCameraLocation(string tag)
        {
            if (!_cameraLocations.TryGetValue(tag, out Transform cameraLoc))
            {
                if (!string.IsNullOrEmpty(tag))
                {
                    GameObject cameraLocGO = GameObject.FindGameObjectWithTag(tag);

                    if (cameraLocGO != null)
                    {
                        cameraLoc = cameraLocGO.transform;
                        _cameraLocations[tag] = cameraLoc;
                    }
                }
            }

            return cameraLoc;
        }

        private void TransitionStep(float t, Vector3 startPos, Quaternion startRot, Vector3 endPos, Quaternion endRot)
        {
            _cinematicCamera.transform.position = Vector3.Lerp(startPos, endPos, t);
            _cinematicCamera.transform.rotation = Quaternion.Slerp(startRot, endRot, t);
        }

        private void LookAtStep(float t, Quaternion startRot, Quaternion endRot)
        {
            _cinematicCamera.transform.rotation = Quaternion.Slerp(startRot, endRot, t);
        }

        public void MoveTo(string tag, string lookAtTag = "")
        {
            if (_cinematicCamera == null)
            {
                GameObject cinematicCameraGO = GameObject.FindGameObjectWithTag(_cinematicCameraTag);
                if (cinematicCameraGO != null)
                {
                    _cinematicCamera = cinematicCameraGO.GetComponent<CameraShake>();
                }

                if (!_cinematicCamera)
                {
                    Debug.LogWarning($"Cannot Move Dialogue Camera To '{tag}' As No Camera With tag '{_cinematicCameraTag}' Exist.");
                    return;
                }
            }

            if (!_cinematicCamera.gameObject.activeSelf)
            {
                return;
            }

            Transform cameraLoc = GetCameraLocation(tag);
            if (cameraLoc == null)
            {
                Debug.LogWarning($"Cannot Move Dialogue Camera To '{tag}' As No GameObject With tag '{tag}' Exist.");
                return;
            }

            Transform lookAtLoc = GetCameraLocation(lookAtTag);

            MoveTo(cameraLoc, lookAt: lookAtLoc, parnet: cameraLoc);
        }

        public void MoveTo(Transform loc, Transform lookAt = null, Transform parnet = null)
        {
            _cinematicCamera.transform.SetParent(loc);
            _cinematicCamera.transform.SetPositionAndRotation(loc.position, loc.rotation);

            if (lookAt != null) _cinematicCamera.transform.LookAt(lookAt.position);

            _cinematicCamera.ResetDefaultState();
        }

        public Promise HandleCameraTransition(string fromTag, string toTag, string lookAtTag, float duration)
        {
            if (_cinematicCamera == null)
            {
                GameObject cinematicCameraGO = GameObject.FindGameObjectWithTag(_cinematicCameraTag);
                if (cinematicCameraGO != null)
                {
                    _cinematicCamera = cinematicCameraGO.GetComponent<CameraShake>();
                }

                if (!_cinematicCamera)
                {
                    Debug.LogWarning($"Cannot Move Dialogue Camera To '{tag}' As No Camera With tag '{_cinematicCameraTag}' Exist.");
                    return null;
                }
            }

            if (!_cinematicCamera.gameObject.activeSelf)
            {
                return null;
            }

            Transform fromLoc = GetCameraLocation(fromTag);
            if (fromLoc == null)
            {
                Debug.LogWarning($"Cannot Transition Dialogue Camera From '{fromTag}' to '{toTag}' As No GameObject With tag '{fromTag}' Exist.");
                return null;
            }

            Transform toLoc = GetCameraLocation(toTag);
            if (toLoc == null)
            {
                Debug.LogWarning($"Cannot Transition Dialogue Camera From '{fromTag}' to '{toTag}' As No GameObject With tag '{toTag}' Exist.");
                return null;
            }

            Transform lookAtLoc = GetCameraLocation(lookAtTag);

            return HandleCameraTransition(fromLoc, toLoc, lookAtLoc, duration, toLoc);
        }

        public Promise HandleCameraTransition(
            Transform from,
            Transform to,
            Transform lookAt,
            float duration,
            Transform parent = null
        )
        {
            if (from == null || to == null) return null;

            _cinematicCamera.transform.SetParent(parent);

            from.GetPositionAndRotation(out Vector3 startPos, out Quaternion startRot);
            to.GetPositionAndRotation(out Vector3 endPos, out Quaternion endRot);

            if (lookAt != null)
            {
                Vector3 direction = (lookAt.position - to.position).normalized;
                endRot = Quaternion.LookRotation(direction);
            }

            _cinematicCamera.IsPaused = true;

            return GameManager.GetMonoSystem<IAnimationMonoSystem>().RequestAnimation(
                this,
                duration,
                (float t) => TransitionStep(t, startPos, startRot, endPos, endRot)
            ).Then(_ =>
            {
                _cinematicCamera.ResetDefaultState();
                _cinematicCamera.IsPaused = false;
            });
        }

        public Promise HandleCameraLookAt(Transform lookAtLoc, float duration)
        {
            if (lookAtLoc == null) return null;

            Quaternion startRot = _cinematicCamera.transform.rotation;
            Vector3 direction = (lookAtLoc.position - _cinematicCamera.transform.position).normalized;
            Quaternion endRot = Quaternion.LookRotation(direction);

            _cinematicCamera.IsPaused = true;

            return GameManager.GetMonoSystem<IAnimationMonoSystem>().RequestAnimation(
                this,
                duration,
                (float t) => LookAtStep(t, startRot, endRot)
            ).Then(_ =>
            {
                _cinematicCamera.ResetDefaultState();
                _cinematicCamera.IsPaused = false;
            });
        }

        public Promise HandleCameraLookAt(string lookAtTag, float duration)
        {
            if (_cinematicCamera == null)
            {
                GameObject cinematicCameraGO = GameObject.FindGameObjectWithTag(_cinematicCameraTag);
                if (cinematicCameraGO != null)
                {
                    _cinematicCamera = cinematicCameraGO.GetComponent<CameraShake>();
                }

                if (!_cinematicCamera)
                {
                    Debug.LogWarning($"Dialogue Camera Cannot Look At '{lookAtTag}' As No Camera With tag '{_cinematicCameraTag}' Exist.");
                    return null;
                }
            }

            if (!_cinematicCamera.gameObject.activeSelf)
            {
                return null;
            }

            Transform lookAtLoc = GetCameraLocation(lookAtTag);
            if (lookAtTag == null)
            {
                Debug.LogWarning($"Dialogue Camera Cannot Look At '{lookAtTag}' As No GameObject With tag '{lookAtTag}' Exist.");
                return null;
            }

            return HandleCameraLookAt(lookAtLoc, duration);
        }
    }
}