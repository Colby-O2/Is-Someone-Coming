using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using PlazmaGames.Core;
using PlazmaGames.Attribute;
using PlazmaGames.Math;
using ColbyO.SJ26.GameLogic;

namespace ColbyO.SJ26.Entities
{
    public class CryingGirlEncounter : MonoBehaviour
    {
        public enum EncounterPhase
        {
            CryingInFetus,
            GettingUp,
            Staring,
            WaitingToChase,
            Chasing,
            CaughtPlayer,
            Stopped
        }

        [Header("Actor Settings")]
        [SerializeField] private string _girlActorName = "Girl";
        [SerializeField] private float _getUpDuration = 1.5f;
        [SerializeField] private float _stareDuration = 2.5f;
        [SerializeField] private float _chaseSpeed = 5.5f;
        [SerializeField] private float _turnSpeed = 8f;

        [Header("Jumpscare Alignment")]
        [SerializeField] private float _catchDistance = 1.35f;
        [SerializeField] private Transform _girlAlignPoint;
        [SerializeField] private Vector3 _cameraOffset = new Vector3(0f, 0f, 0.45f);
        [SerializeField] private Vector3 _rotationTuningOffset = new Vector3(0f, 180f, 0f);
        [SerializeField] private bool _infiniteJumpscareForTesting = true;
        [SerializeField] private float _jumpscareDuration = 2.0f;
        [SerializeField] private float _cameraShakeIntensity = 0.015f;

        [Header("Head Twitch Settings")]
        [SerializeField] private float _girlHeadShakeAngle = 18f;
        [SerializeField] private float _girlHeadShakeSpeed = 55f;
        [SerializeField] private UnityEvent _onJumpscareFinished;

        [Header("Audio")]
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _cryingLoopClip;
        [SerializeField] private AudioClip _screamLoopClip;
        [SerializeField] private AudioClip _jumpscareSoundClip;

        [Header("Debug")]
        [SerializeField, ReadOnly] private EncounterPhase _phase = EncounterPhase.CryingInFetus;
        [SerializeField, ReadOnly] private Actor _girlActor;
        private Transform _playerTransform;
        private ViewController _playerView;

        private Vector3 _girlStartPos;
        private Quaternion _girlStartRot;
        private Quaternion _alignPointStartLocalRot;
        private Vector3 _camStartLocalPos;
        private Collider[] _girlColliders;
        private bool _hasCachedDefaults;
        private Promise _promise;

        public EncounterPhase Phase => _phase;
        public UnityEvent OnJumpscareFinished => _onJumpscareFinished;

        public bool Force { get; set; }

        private void Awake()
        {
            _phase = EncounterPhase.CryingInFetus;
        }

        private IEnumerator Start()
        {
            yield return null;

            _girlActor = GameManager.GetMonoSystem<IActorMonoSystem>().GetActor(_girlActorName);

            if (SJ26GameManager.Player != null)
            {
                _playerTransform = SJ26GameManager.Player.transform;
                _playerView = SJ26GameManager.Player.GetComponentInChildren<ViewController>();
                if (_playerView != null)
                {
                    _camStartLocalPos = _playerView.transform.localPosition;
                }
            }

            if (_girlActor != null)
            {
                if (_girlAlignPoint == null)
                {
                    _girlAlignPoint = _girlActor.GetFace();
                }

                _girlStartPos = _girlActor.transform.position;
                _girlStartRot = _girlActor.transform.rotation;
                _girlColliders = _girlActor.GetComponentsInChildren<Collider>();

                if (_girlAlignPoint != null)
                {
                    _alignPointStartLocalRot = _girlAlignPoint.localRotation;
                }

                _hasCachedDefaults = true;
            }

            ResetEncounter();
        }

        [ContextMenu("Reset Encounter")]
        public void ResetEncounter()
        {
            StopAllCoroutines();

            _phase = EncounterPhase.CryingInFetus;

            if (SJ26GameManager.Player != null)
            {
                SJ26GameManager.Player.Unfreeze();
            }

            if (_playerView != null && _hasCachedDefaults)
            {
                _playerView.transform.localPosition = _camStartLocalPos;
            }

            if (_girlActor != null && _hasCachedDefaults)
            {
                if (_girlColliders != null)
                {
                    foreach (Collider col in _girlColliders)
                    {
                        if (col != null) col.enabled = true;
                    }
                }

                if (_girlAlignPoint != null && _girlAlignPoint != _girlActor.transform)
                {
                    _girlAlignPoint.localRotation = _alignPointStartLocalRot;
                }

                _girlActor.ResetToPosition(_girlStartPos, _girlStartRot, MoveState.Fetus);
            }

            if (_audioSource != null)
            {
                _audioSource.Stop();
                if (_cryingLoopClip != null)
                {
                    _audioSource.clip = _cryingLoopClip;
                    _audioSource.loop = true;
                    _audioSource.Play();
                }
            }
        }

        public Promise WaitForEvent()
        {
            SJ26GameManager.GlobalScheduler.When(() => GameManager.GetMonoSystem<IGameLogicMonoSystem>().IsInRange("InCryingGirlRoom"))
            .Then(_ =>
            {
                TriggerEncounter();
            });

            return Promise.CreateExisting(ref _promise);
        }

        public void ForceEvent()
        {
            TriggerEncounter();
        }

        public void TriggerEncounter()
        {
            if (_girlActor == null || _phase != EncounterPhase.CryingInFetus) return;

            _phase = EncounterPhase.GettingUp;

            if (_audioSource != null) _audioSource.Stop();

            _girlActor.SetState(MoveState.Idle, _getUpDuration)
            .Then(_ =>
            {
                if (_phase != EncounterPhase.GettingUp) return;
                _phase = EncounterPhase.Staring;
                StartCoroutine(StareThenChaseRoutine());
                Promise.ResolveExisting(ref _promise);
            });
        }

        public void TriggerChase()
        {
            if (!_girlActor) return;

            if (_audioSource != null && _screamLoopClip != null)
            {
                _audioSource.clip = _screamLoopClip;
                _audioSource.loop = true;
                _audioSource.Play();
            }

            _girlActor.SetMoveSpeed(_chaseSpeed);
            _girlActor.SetChasingMode(true);
            SJ26GameManager.GlobalScheduler.Wait(0.5f)
            .Then(_ => _girlActor.SetState(MoveState.Moving, 0.2f))
            .Then(_ =>
            {
                if (_phase != EncounterPhase.WaitingToChase) return;
                _phase = EncounterPhase.Chasing;
            });
        }

        public void Stop()
        {
            _phase = EncounterPhase.Stopped;
        }

        private IEnumerator StareThenChaseRoutine()
        {
            if (!Force)
            {
                float timer = 0f;
                while (timer < _stareDuration)
                {
                    timer += Time.deltaTime;
                    RotateTowardsPlayer();
                    yield return null;
                }

                float windupTimer = 0f;
                while (windupTimer < 1f)
                {
                    windupTimer += Time.deltaTime;
                    RotateTowardsPlayer();
                    yield return null;
                }
            }

            _phase = EncounterPhase.WaitingToChase;

            if (Force) TriggerChase();
        }

        private void Update()
        {
            if (_girlActor == null || _playerTransform == null) return;

            if (_phase == EncounterPhase.Stopped)
            {
                if (_audioSource != null && _audioSource.isPlaying) _audioSource.Stop();
                _girlActor.SetChasingMode(false);
                _girlActor.StopMoving(disableAgent: true);
                if (_girlActor.State != MoveState.Idle) _girlActor.SetState(MoveState.Idle, 0.05f);
            }
            else if (_phase == EncounterPhase.Chasing)
            {
                _girlActor.ChaseTarget(_playerTransform.position);

                Vector3 girlPosFlat = _girlActor.transform.position.SetY(0f);
                Vector3 playerPosFlat = _playerTransform.position.SetY(0f);
                float distanceToPlayer = Vector3.Distance(girlPosFlat, playerPosFlat);

                if (distanceToPlayer <= _catchDistance)
                {
                    ExecuteJumpscare();
                }
            }
        }

        private void RotateTowardsPlayer()
        {
            if (_playerTransform == null || _girlActor == null) return;

            Vector3 dir = (_playerTransform.position.SetY(_girlActor.transform.position.y) - _girlActor.transform.position).normalized;
            if (dir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(dir);
                _girlActor.transform.rotation = Quaternion.Slerp(_girlActor.transform.rotation, targetRot, _turnSpeed * Time.deltaTime);
            }
        }

        [ContextMenu("Test Jumpscare Now")]
        public void ExecuteJumpscare(bool force = false)
        {
            if (_phase == EncounterPhase.CaughtPlayer || force) return;
            _phase = EncounterPhase.CaughtPlayer;

            SJ26GameManager.Player.Freeze();

            _girlActor.SetChasingMode(false);
            _girlActor.StopMoving(disableAgent: true);
            _girlActor.SetState(MoveState.Reach, 0.05f);

            if (_audioSource != null)
            {
                _audioSource.Stop();
                if (_jumpscareSoundClip != null)
                    _audioSource.PlayOneShot(_jumpscareSoundClip);
            }

            StartCoroutine(JumpscareSequenceRoutine());
        }

        private IEnumerator JumpscareSequenceRoutine()
        {
            Transform camTransform = _playerView != null ? _playerView.transform : _playerTransform;

            Transform alignPoint = _girlAlignPoint != null ? _girlAlignPoint : _girlActor.GetFace();
            if (alignPoint == null) alignPoint = _girlActor.transform;

            if (_girlColliders != null)
            {
                foreach (Collider col in _girlColliders)
                {
                    if (col != null) col.enabled = false;
                }
            }

            if (_playerView != null)
            {
                _playerView.transform.localEulerAngles = new Vector3(0f, _playerView.transform.localEulerAngles.y, 0f);
            }

            Vector3 anchorCamPos = camTransform.position;
            Vector3 flatCamForward = camTransform.forward.SetY(0f).normalized;
            if (flatCamForward.sqrMagnitude < 0.001f) flatCamForward = _playerTransform.forward;
            Quaternion flatCamRot = Quaternion.LookRotation(flatCamForward);

            Vector3 localRootToFaceOffset = _girlActor.transform.InverseTransformPoint(alignPoint.position);

            float timer = 0f;

            while (_infiniteJumpscareForTesting || timer < _jumpscareDuration)
            {
                timer += Time.deltaTime;

                Quaternion targetGirlRot = flatCamRot * Quaternion.Euler(_rotationTuningOffset);
                _girlActor.transform.rotation = targetGirlRot;

                Vector3 desiredFaceWorldPos = anchorCamPos + (flatCamRot * _cameraOffset);

                Vector3 worldRootToFaceOffset = targetGirlRot * Vector3.Scale(localRootToFaceOffset, _girlActor.transform.localScale);
                _girlActor.transform.position = desiredFaceWorldPos - worldRootToFaceOffset;

                if (alignPoint != _girlActor.transform)
                {
                    float shakePitch = Mathf.Sin(timer * _girlHeadShakeSpeed) * _girlHeadShakeAngle
                                       + Random.Range(-_girlHeadShakeAngle * 0.3f, _girlHeadShakeAngle * 0.3f);
                    float shakeYaw = Mathf.Cos(timer * (_girlHeadShakeSpeed * 0.85f)) * _girlHeadShakeAngle
                                     + Random.Range(-_girlHeadShakeAngle * 0.3f, _girlHeadShakeAngle * 0.3f);
                    float shakeRoll = Mathf.Sin(timer * (_girlHeadShakeSpeed * 1.3f)) * (_girlHeadShakeAngle * 1.1f);

                    alignPoint.localRotation = _alignPointStartLocalRot * Quaternion.Euler(shakePitch, shakeYaw, shakeRoll);
                }

                if (_playerView != null)
                {
                    _playerView.transform.localPosition = _camStartLocalPos + Random.insideUnitSphere * _cameraShakeIntensity;
                }

                yield return null;
            }

            if (alignPoint != _girlActor.transform)
            {
                alignPoint.localRotation = _alignPointStartLocalRot;
            }

            if (_playerView != null)
            {
                _playerView.transform.localPosition = _camStartLocalPos;
            }

            if (_girlColliders != null)
            {
                foreach (Collider col in _girlColliders)
                {
                    if (col != null) col.enabled = true;
                }
            }

            _onJumpscareFinished?.Invoke();
        }
    }
}