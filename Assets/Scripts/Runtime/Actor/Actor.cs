using UnityEngine;
using UnityEngine.AI;

using System;

using InteractionSystem;
using PlazmaGames.Animation;
using PlazmaGames.Attribute;
using PlazmaGames.Core;
using PlazmaGames.UI;

using ColbyO.SJ26.Dialogue;
using ColbyO.SJ26.Rig;

namespace ColbyO.SJ26
{
    public enum MoveState
    {
        Idle,
        Waiting,
        Moving,
        SittingIntermediate,
        Sitting,
        Reach,
        ChangingState
    }

    [RequireComponent(typeof(NavMeshAgent))]
    public class Actor : MonoBehaviour
    {
        [SerializeField] private ActorAnimator _animator;

        [Header("Information")]
        [SerializeField] private string _actorName;

        [Header("References")]
        [SerializeField] private NavMeshAgent _agent;

        [Header("State")]
        [SerializeField, ReadOnly] private MoveState _state = MoveState.Idle;

        [Header("Settings")]
        [SerializeField] private float _sitDistance = 0.3f;
        [SerializeField] private float _followDistance = 1f;
        [SerializeField] private float _timeBeforeMad;

        [Header("Debug")]
        [SerializeField, ReadOnly] private Vector3 _targetPos;
        [SerializeField, ReadOnly] private Transform _withTarget;
        [SerializeField, ReadOnly] private bool _isWaitingForDoor;
        [SerializeField, ReadOnly] private bool _isWaitingForPlayer;
        [SerializeField, ReadOnly] private float _timeAwayFromPlayer;

        [SerializeField, ReadOnly] private Promise _gotoPromise;
        [SerializeField, ReadOnly] private Promise _sitPromise;
        [SerializeField, ReadOnly] private string _farDialogue;

        public string ActorName => _actorName;

        private void Awake()
        {
            if (!_agent) _agent = GetComponent<NavMeshAgent>();
            GameManager.GetMonoSystem<IPoseMonoSystem>().LoadPose(ActorName, Enum.GetName(typeof(MoveState), MoveState.Idle));
            _state = MoveState.Idle;
        }

        private void Update()
        {
            if (_animator != null)
                _animator.UpdateAnimator(_state, Time.deltaTime);

            //if (_withTarget != null && _state != MoveState.ChangingState && !_isWaitingForDoor)
            //{
            //    float dstToWith = Vector3.Distance(transform.position, _withTarget.position);
            //    bool isPlayerFarAway = dstToWith > _followDistance;

            //    Vector3 forward = transform.forward;
            //    Vector3 playerDir = (SJ26GameManager.Player.transform.position.SetY(transform.position.y) - transform.position).normalized;
            //    float dot = Vector3.Dot(forward, playerDir);

            //    if (_isWaitingForPlayer)
            //    {
            //        _timeAwayFromPlayer += Time.deltaTime;

            //        Quaternion targetRot = Quaternion.LookRotation((_withTarget.position.SetY(transform.position.y) - transform.position).normalized);
            //        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, _agent.angularSpeed * Time.deltaTime);

            //        if (_timeAwayFromPlayer > _timeBeforeMad)
            //        {
            //            if (!string.IsNullOrEmpty(_farDialogue) && GameManager.GetMonoSystem<IUIMonoSystem>().GetCurrentViewIs<GameView>())
            //            {
            //                GameManager.GetMonoSystem<IDialogueMonoSystem>().StartDialoguePromise(_farDialogue, passive: true)
            //                   .Then(_ =>
            //                   {
            //                       _timeAwayFromPlayer = 0f;
            //                   });
            //            }
            //        }
            //    }

            //    if (dot < 0 && isPlayerFarAway && !_isWaitingForPlayer)
            //    {
            //        _isWaitingForPlayer = true;
            //        _timeAwayFromPlayer = 0f;
            //        _agent.isStopped = true;

            //        if (_state != MoveState.Idle) UpdateState(MoveState.Idle, 0.25f);
            //    }
            //    else if (!isPlayerFarAway && _isWaitingForPlayer)
            //    {
            //        _isWaitingForPlayer = false;
            //        _agent.isStopped = false;
            //        UpdateState(MoveState.Moving, 0f);
            //    }
            //}

            if (_state == MoveState.Moving && !_agent.isStopped)
                UpdateMovement();
        }

        private void UpdateMovement()
        {
            if (_agent.pathPending) return;

            HandleDoor();

            if (_agent.remainingDistance <= _agent.stoppingDistance)
            {
                if (!_agent.hasPath || _agent.velocity.sqrMagnitude < 0.01f)
                {
                    _agent.isStopped = true;
                    _withTarget = null;
                    UpdateState(_sitPromise != null ? MoveState.Idle : MoveState.Waiting)
                    .Then(_ =>
                    {
                        Promise.ResolveExisting(ref _gotoPromise);
                    });
                }
            }
        }

        private void HandleDoor()
        {
            return;
            //if (!InFrontOfDoor(out Interactable doorObj)) return;
            //DoorAction door = doorObj.GetAction<DoorAction>();
            //if (door.IsOpen) return;

            //_agent.isStopped = true;
            //_isWaitingForDoor = true;

            //Quaternion startRot = transform.rotation;
            //Quaternion targetRot = Quaternion.LookRotation((doorObj.transform.position.SetY(transform.position.y) - transform.position).normalized);

            //UpdateState(MoveState.Idle, 0.25f)
            //.Then(_ =>
            //{
            //    GameManager.GetMonoSystem<IAnimationMonoSystem>().RequestAnimation(
            //        this,
            //        1f,
            //        (float t) =>
            //        {
            //            transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            //        },
            //        () => {
            //            UpdateState(MoveState.Reach)
            //            .Then(_ =>
            //            {

            //                Promise promise = door.RequestOpen();

            //                if (promise == null)
            //                {
            //                    UpdateState(MoveState.Moving)
            //                    .Then(_ =>
            //                    {
            //                        _isWaitingForDoor = false;
            //                        _agent.isStopped = false;
            //                    });

            //                    return null;
            //                }

            //                return promise;
            //            })
            //            .Then(_ => UpdateState(MoveState.Moving))
            //            .Then(_ =>
            //            {
            //                _isWaitingForDoor = false;
            //                _agent.isStopped = false;
            //            });
            //        }
            //    );
            //});
        }

        private bool InFrontOfDoor(out Interactable door)
        {
            door = null;
            Vector3 origin = transform.position + Vector3.up * 0.5f;
            Vector3 direction = _agent.desiredVelocity.normalized;

            if (direction.sqrMagnitude < 0.1f)
                return false;

            //if (Physics.SphereCast(origin, 0.1f, direction, out RaycastHit hit, 0.3f))
            //{
            //    if (hit.collider.TryGetComponent<Interactable>(out door))
            //        return door.HasAction<DoorAction>();
            //}

            return false;
        }

        public void SetMoveSpeed(float speed) => _agent.speed = speed;

        public Promise LerpPose(string poseName, float? duration = null)
        {
            return GameManager.GetMonoSystem<IPoseMonoSystem>()
                .LerpPose(_actorName, poseName, speedOverride: duration);
        }

        private Promise UpdateState(MoveState state, float? duration = null)
        {
            _state = MoveState.ChangingState;

            return LerpPose(Enum.GetName(typeof(MoveState), state), duration)
                .Then(_ =>
                {
                    _state = state;
                });
        }

        public Promise GotoWith(Transform target, Transform with, string farDialgoue = null)
        {
            return Goto(target.position, with, farDialgoue);
        }

        public Promise Goto(Transform target)
        {
            return Goto(target.position);
        }

        public Promise Goto(Vector3 position, Transform with = null, string farDialgoue = null)
        {
            Promise.CreateExisting(ref _gotoPromise);

            _withTarget = with;
            _farDialogue = farDialgoue;

            UpdateState(MoveState.Moving)
            .Then(_ =>
            {
                _targetPos = position;
                _agent.isStopped = false;
                _agent.SetDestination(_targetPos);
            });

            return _gotoPromise;
        }

        private void SitStep(float t, Quaternion startRot, Quaternion targetRot)
        {
            transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
        }

        public Promise SitAt(Transform seat)
        {
            if (_state == MoveState.Sitting || _sitPromise != null) return null;

            Vector3 forward = seat.forward;
            Vector3 targetPos = seat.position - forward * _sitDistance;
            Quaternion targetRot = Quaternion.LookRotation(-forward);

            Goto(targetPos).Then(_ =>
            {
                _state = MoveState.ChangingState;
                _agent.isStopped = true;

                Quaternion startRot = transform.rotation;

                GameManager.GetMonoSystem<IAnimationMonoSystem>().RequestAnimation(this, 1f, (float t) =>
                {
                    SitStep(t, startRot, targetRot);
                }).Then(_ =>
                {
                    UpdateState(MoveState.SittingIntermediate)
                    .Then(_ => UpdateState(MoveState.Sitting))
                    .Then(_ => { Promise.ResolveExisting(ref _sitPromise); });
                });
            });

            return Promise.CreateExisting(ref _sitPromise);
        }

        public Promise Unsit()
        {
            if (_state != MoveState.Sitting) return null;

            _state = MoveState.ChangingState;
            UpdateState(MoveState.SittingIntermediate)
            .Then(_ => { Promise.ResolveExisting(ref _sitPromise); })
            .Then(_ =>
            {
                _state = MoveState.Idle;
                _agent.isStopped = false;
                Promise.ResolveExisting(ref _sitPromise);
            });

            return Promise.CreateExisting(ref _sitPromise);
        }
    }
}