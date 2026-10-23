using System.Collections;
using PlazmaGames.Attribute;
using UnityEngine;

namespace ColbyO.SJ26.Enviroment
{
    public class FlashlightController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform _cameraTransform;
        [SerializeField] private Light _spotLight;
        [SerializeField] private AudioSource _audioSource;

        [Header("Audio Clips")]
        [SerializeField] private AudioClip _clickOnClip;
        [SerializeField] private AudioClip _clickOffClip;

        [Header("Hand Motion & Sway")]
        [Tooltip("If true, the light aims parallel to camera forward (no inward cross-eye). If false, it uses the hand bone's exact rotation.")]
        [SerializeField] private bool _aimParallelToCamera = true;
        [SerializeField] private float _rotationSpeed = 14f;
        [SerializeField] private float _idleSwayAmount = 0.6f;
        [SerializeField] private float _idleSwaySpeed = 1.2f;
        [SerializeField] private float _walkBobAmount = 2.5f;
        [SerializeField] private float _walkBobSpeed = 9f;

        [Header("PSX Flicker & Walls")]
        [SerializeField, Range(0f, 1f)] private float _ambientSputterChance = 0.15f;
        [SerializeField] private int _lightSteps = 5;
        [SerializeField] private LayerMask _wallMask = ~0;
        [SerializeField] private float _wallCheckDistance = 1.2f;

        [SerializeField, ReadOnly] private bool _isOn;
        [SerializeField, ReadOnly] private float _defaultIntensity;
        [SerializeField, ReadOnly] private bool _isInterfering;

        private Coroutine _interferenceRoutine;
        private float _noiseSeed;
        private Vector3 _lastTrackPos;
        private float _walkCycle;
        private Quaternion _initialLocalRotation;
        private Quaternion _smoothedWorldRotation;

        public bool IsOn => _isOn;

        private void Awake()
        {
            _noiseSeed = Random.Range(0f, 100f);
            _initialLocalRotation = transform.localRotation;
            _smoothedWorldRotation = transform.rotation;

            if (_spotLight != null)
            {
                _defaultIntensity = _spotLight.intensity;
            }

            Transform trackTarget = _cameraTransform != null ? _cameraTransform : transform;
            _lastTrackPos = trackTarget.position;

            SetLightState(true, playAudio: false);
        }

        private void LateUpdate()
        {
            HandleRotationAndBob();

            if (!_isOn) return;

            HandleAmbientFlickerAndWalls();
        }

        private void HandleRotationAndBob()
        {
            Transform trackTarget = _cameraTransform != null ? _cameraTransform : transform;
            float speed = Vector3.Distance(trackTarget.position, _lastTrackPos) / Mathf.Max(Time.deltaTime, 0.0001f);
            _lastTrackPos = trackTarget.position;

            float moveFactor = Mathf.Clamp01(speed / 4f);
            _walkCycle += Time.deltaTime * _walkBobSpeed * moveFactor;

            float swayX = (Mathf.PerlinNoise(Time.time * _idleSwaySpeed, _noiseSeed) - 0.5f) * 2f * _idleSwayAmount;
            float swayY = (Mathf.PerlinNoise(_noiseSeed, Time.time * _idleSwaySpeed) - 0.5f) * 2f * _idleSwayAmount;

            float bobX = Mathf.Sin(_walkCycle * 2f) * _walkBobAmount * moveFactor;
            float bobY = Mathf.Cos(_walkCycle) * (_walkBobAmount * 0.6f) * moveFactor;

            Quaternion swayAndBobOffset = Quaternion.Euler(swayX + bobX, swayY + bobY, 0f);

            if (_aimParallelToCamera && _cameraTransform != null)
            {
                Quaternion targetWorldRot = _cameraTransform.rotation * swayAndBobOffset;
                _smoothedWorldRotation = Quaternion.Slerp(_smoothedWorldRotation, targetWorldRot, Time.deltaTime * _rotationSpeed);
                transform.rotation = _smoothedWorldRotation;
            }
            else
            {
                Quaternion targetLocalRot = _initialLocalRotation * swayAndBobOffset;
                transform.localRotation = Quaternion.Slerp(transform.localRotation, targetLocalRot, Time.deltaTime * _rotationSpeed);
            }
        }

        private void HandleAmbientFlickerAndWalls()
        {
            if (_isInterfering) return;

            float multiplier = 1f;

            float steppedTime = Mathf.Floor(Time.time * 15f) / 15f;
            float noise = Mathf.PerlinNoise(steppedTime * 12f, _noiseSeed);

            if (noise < _ambientSputterChance)
            {
                multiplier = Random.Range(0.45f, 0.85f);
            }

            if (Physics.SphereCast(transform.position, 0.1f, transform.forward, out RaycastHit hit, _wallCheckDistance, _wallMask, QueryTriggerInteraction.Ignore))
            {
                float wallFactor = Mathf.Lerp(0.35f, 1f, hit.distance / _wallCheckDistance);
                multiplier *= wallFactor;
            }

            ApplyIntensityMultiplier(multiplier);
        }

        public void ToggleFlashlight()
        {
            SetLightState(!_isOn, playAudio: true);
        }

        public void SetLightState(bool state, bool playAudio = true)
        {
            _isOn = state;

            if (_spotLight != null) _spotLight.enabled = _isOn;

            if (_isOn)
            {
                if (_interferenceRoutine != null) StopCoroutine(_interferenceRoutine);
                _interferenceRoutine = StartCoroutine(TurnOnWarmupRoutine());
            }

            if (playAudio)
            {
                PlaySound(_isOn ? _clickOnClip : _clickOffClip, Random.Range(0.95f, 1.05f));
            }
        }

        private IEnumerator TurnOnWarmupRoutine()
        {
            ApplyIntensityMultiplier(0.3f);
            yield return new WaitForSeconds(0.05f);
            ApplyIntensityMultiplier(1.15f);
            yield return new WaitForSeconds(0.04f);
            ApplyIntensityMultiplier(1f);
            _interferenceRoutine = null;
        }

        public void TriggerInterference(float duration = 2.0f, bool shutOffAtEnd = false)
        {
            if (!_isOn) return;

            if (_interferenceRoutine != null) StopCoroutine(_interferenceRoutine);
            _interferenceRoutine = StartCoroutine(InterferenceRoutine(duration, shutOffAtEnd));
        }

        private IEnumerator InterferenceRoutine(float duration, bool shutOffAtEnd)
        {
            _isInterfering = true;
            float elapsed = 0f;

            while (elapsed < duration && _isOn)
            {
                elapsed += Time.deltaTime;

                float steppedTime = Mathf.Floor(Time.time * 15f) / 15f;
                float noise = Mathf.PerlinNoise(steppedTime * 30f, _noiseSeed + 50f);
                float multiplier = noise > 0.45f ? noise : 0.05f;

                ApplyIntensityMultiplier(multiplier);
                yield return null;
            }

            _isInterfering = false;

            if (shutOffAtEnd && _isOn)
            {
                SetLightState(false, playAudio: true);
            }
            else if (_isOn)
            {
                ApplyIntensityMultiplier(1f);
            }
        }

        private void ApplyIntensityMultiplier(float multiplier)
        {
            if (_spotLight == null) return;

            if (_lightSteps > 0)
            {
                multiplier = Mathf.Round(multiplier * _lightSteps) / _lightSteps;
            }

            _spotLight.intensity = _defaultIntensity * multiplier;
        }

        private void PlaySound(AudioClip clip, float pitch = 1f)
        {
            if (_audioSource != null && clip != null)
            {
                _audioSource.pitch = pitch;
                _audioSource.PlayOneShot(clip);
            }
        }
    }
}