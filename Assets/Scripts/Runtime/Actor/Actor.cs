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
using PlazmaGames.Math;

namespace ColbyO.SJ26.Entities
{
    public enum MoveState
    {
        Idle,
        Waiting,
        Moving,
        SittingIntermediate,
        Sitting,
        Reach,
        ChangingState,
        HoldingLightMoving,
        HoldingLightIdle,
        Fetus
    }

    [RequireComponent(typeof(NavMeshAgent))]
    public class Actor : MonoBehaviour
    {
        [SerializeField] private ActorAnimator _animator;

        [Header("Information")]
        [SerializeField] private string _actorName;

        [Header("References")]
        [SerializeField] private NavMeshAgent _agent;
        [SerializeField] private Transform _face;

        [Header("State")]
        [SerializeField, ReadOnly] private MoveState _state = MoveState.Idle;

        [Header("Sitting Settings")]
        [SerializeField] private Transform _mesh;
        [SerializeField] private float _sitDistance = 0.35f;
        [SerializeField] private float _sitHeightOffset = 0.2f;
        [SerializeField] private float _sitForwardOffset = 0.25f;

        [Header("Follow Settings")]
        [SerializeField] private float _followDistance = 1f;
        [SerializeField] private float _timeBeforeMad;

        [Header("Debug")]
        [SerializeField, ReadOnly] private Vector3 _targetPos;
        [SerializeField, ReadOnly] private Transform _withTarget;
        [SerializeField, ReadOnly] private bool _isWaitingForDoor;
        [SerializeField, ReadOnly] private bool _isWaitingForPlayer;
        [SerializeField, ReadOnly] private float _timeAwayFromPlayer;

        private Promise _gotoPromise;
        private Promise _sitPromise;
        [SerializeField, ReadOnly] private string _farDialogue;

        private Vector3 _meshSavedLocalPos;
        private Quaternion _meshSavedLocalRot;

        public string ActorName => _actorName;
        public MoveState State => _state;

        private void Awake()
        {
            if (!_agent) _agent = GetComponent<NavMeshAgent>();
            GameManager.GetMonoSystem<IPoseMonoSystem>().LoadPose(ActorName, Enum.GetName(typeof(MoveState), MoveState.Idle));
            _state = MoveState.Idle;

            if (_animator != null) _animator.Init();
        }

        private void Update()
        {
            if (_animator != null)
                _animator.UpdateAnimator(_state, Time.deltaTime);

            if (_withTarget != null && _state != MoveState.ChangingState && !_isWaitingForDoor)
            {
                float dstToWith = Vector3.Distance(transform.position, _withTarget.position);
                bool isPlayerFarAway = dstToWith > _followDistance;

                Vector3 forward = transform.forward;
                Vector3 playerDir = (SJ26GameManager.Player.transform.position.SetY(transform.position.y) - transform.position).normalized;
                float dot = Vector3.Dot(forward, playerDir);

                if (_isWaitingForPlayer)
                {
                    _timeAwayFromPlayer += Time.deltaTime;

                    Quaternion targetRot = Quaternion.LookRotation((_withTarget.position.SetY(transform.position.y) - transform.position).normalized);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, _agent.angularSpeed * Time.deltaTime);

                    if (_timeAwayFromPlayer > _timeBeforeMad)
                    {
                        if (!string.IsNullOrEmpty(_farDialogue) && GameManager.GetMonoSystem<IUIMonoSystem>().GetCurrentViewIs<GameView>())
                        {
                            GameManager.GetMonoSystem<IDialogueMonoSystem>().StartDialoguePromise(_farDialogue, passive: true)
                               .Then(_ =>
                               {
                                   _timeAwayFromPlayer = 0f;
                               });
                        }
                    }
                }

                if (dot < 0 && isPlayerFarAway && !_isWaitingForPlayer)
                {
                    _isWaitingForPlayer = true;
                    _timeAwayFromPlayer = 0f;
                    _agent.isStopped = true;

                    if (_state != MoveState.Idle) UpdateState(MoveState.Idle, 0.25f);
                }
                else if (!isPlayerFarAway && _isWaitingForPlayer)
                {
                    _isWaitingForPlayer = false;
                    _agent.isStopped = false;
                    UpdateState(MoveState.Moving, 0f);
                }
            }

            if (_state == MoveState.Moving && !_agent.isStopped)
                UpdateMovement();
        }

        private void UpdateMovement()
        {
            if (_agent.pathPending) return;

            HandleDoor();

            if (_agent.remainingDistance <= _agent.stoppingDistance)
            {
                if (!_agent.hasPath || _agent.velocity.sqrMagnitude < 0.1f)
                {
                    _agent.isStopped = true;
                    _agent.ResetPath();
                    _withTarget = null;

                    MoveState nextState = _sitPromise != null ? MoveState.Idle : MoveState.Waiting;

                    UpdateState(nextState)
                    .Then(_ =>
                    {
                        Promise.ResolveExisting(ref _gotoPromise);
                    });
                }
            }
        }

        private void HandleDoor()
        {
            if (!InFrontOfDoor(out Interactable doorObj)) return;
            DoorAction door = doorObj.GetAction<DoorAction>();
            if (door.IsOpen) return;

            _agent.isStopped = true;
            _isWaitingForDoor = true;

            Quaternion startRot = transform.rotation;
            Quaternion targetRot = Quaternion.LookRotation((doorObj.transform.position.SetY(transform.position.y) - transform.position).normalized);

            UpdateState(MoveState.Idle, 0.25f)
            .Then(_ =>
            {
                GameManager.GetMonoSystem<IAnimationMonoSystem>().RequestAnimation(
                    this,
                    1f,
                    (float t) =>
                    {
                        transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
                    })
                    .Then(_ =>
                    {
                        UpdateState(MoveState.Reach)
                        .Then(_ =>
                        {

                            Promise promise = door.RequestOpen();

                            if (promise == null)
                            {
                                UpdateState(MoveState.Moving)
                                .Then(_ =>
                                {
                                    _isWaitingForDoor = false;
                                    _agent.isStopped = false;
                                });

                                return null;
                            }

                            return promise;
                        })
                        .Then(_ => UpdateState(MoveState.Moving))
                        .Then(_ =>
                        {
                            _isWaitingForDoor = false;
                            _agent.isStopped = false;
                        });
                    }
                );
            });
        }

        private bool InFrontOfDoor(out Interactable door)
        {
            door = null;
            Vector3 origin = transform.position + Vector3.up * 0.5f;
            Vector3 direction = _agent.desiredVelocity.normalized;

            if (direction.sqrMagnitude < 0.1f)
                return false;

            if (Physics.SphereCast(origin, 0.1f, direction, out RaycastHit hit, 0.3f))
            {
                if (hit.collider.TryGetComponent<Interactable>(out door))
                    return door.HasAction<DoorAction>();
            }

            return false;
        }

        public Transform GetFace() => _face;

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
                    if (_animator != null && (state == MoveState.Idle || state == MoveState.Moving || state == MoveState.HoldingLightIdle || state == MoveState.HoldingLightMoving))
                    {
                        _animator.CacheRestPositions();
                    }
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
            Debug.Log("Making A goto promise");
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

        private void SitStep(float t, Vector3 startPos, Vector3 targetPos, Quaternion startRot, Quaternion targetRot)
        {
            if (_mesh)
            {
                _mesh.position = Vector3.Lerp(startPos, targetPos, t);
                _mesh.rotation = Quaternion.Slerp(startRot, targetRot, t);
            }
            else
            {
                transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            }
        }

        public Promise SitAt(Transform seat)
        {
            if (_state == MoveState.Sitting || _sitPromise != null) return null;

            Promise.CreateExisting(ref _sitPromise);

            Vector3 forward = seat.forward;
            Vector3 walkTargetPos = seat.position + forward * _sitDistance;
            Quaternion targetRot = Quaternion.LookRotation(forward);

            Goto(walkTargetPos).Then(_ =>
            {
                _state = MoveState.ChangingState;
                _agent.isStopped = true;

                Transform movingPart = _mesh ? _mesh : transform;
                if (_mesh)
                {
                    _meshSavedLocalPos = _mesh.localPosition;
                    _meshSavedLocalRot = _mesh.localRotation;
                }

                Vector3 startPos = movingPart.position;
                Quaternion startRot = movingPart.rotation;

                Vector3 sitTargetPos = new Vector3(
                    seat.position.x,
                    startPos.y - _sitHeightOffset,
                    seat.position.z
                ) + forward * _sitForwardOffset;

                UpdateState(MoveState.SittingIntermediate)
                .Then(_ =>
                {
                    UpdateState(MoveState.Sitting);
                    return GameManager.GetMonoSystem<IAnimationMonoSystem>().RequestAnimation(this, 1f, (float t) =>
                    {
                        SitStep(t, startPos, sitTargetPos, startRot, targetRot);
                    });
                })
                .Then(_ =>
                {
                    Promise.ResolveExisting(ref _sitPromise);
                });
            });

            return _sitPromise;
        }

        public Promise Unsit()
        {
            if (_state != MoveState.Sitting) return null;

            Promise.CreateExisting(ref _sitPromise);
            _state = MoveState.ChangingState;

            Transform movingPart = _mesh ? _mesh : transform;
            Vector3 startPos = movingPart.position;
            Quaternion startRot = movingPart.rotation;

            Vector3 targetWorldPos = _mesh && _mesh.parent
                ? _mesh.parent.TransformPoint(_meshSavedLocalPos)
                : transform.position;
            Quaternion targetWorldRot = _mesh && _mesh.parent
                ? _mesh.parent.rotation * _meshSavedLocalRot
                : transform.rotation;

            GameManager.GetMonoSystem<IAnimationMonoSystem>().RequestAnimation(this, 1f, (float t) =>
            {
                SitStep(t, startPos, targetWorldPos, startRot, targetWorldRot);
            });

            UpdateState(MoveState.SittingIntermediate)
            .Then(_ => UpdateState(MoveState.Idle))
            .Then(_ =>
            {
                if (_mesh)
                {
                    _mesh.localPosition = _meshSavedLocalPos;
                    _mesh.localRotation = _meshSavedLocalRot;
                }
                _state = MoveState.Idle;
                _agent.isStopped = false;
                Promise.ResolveExisting(ref _sitPromise);
            });

            return _sitPromise;
        }

        public Promise LookAt(Transform target, float duration)
        {
            if (target == null) return null;

            Vector3 targetDirection = (target.position.SetY(transform.position.y) - transform.position).normalized;

            if (targetDirection.sqrMagnitude < 0.001f) return null;

            Quaternion startRot = transform.rotation;
            Quaternion targetRot = Quaternion.LookRotation(targetDirection);

            return GameManager.GetMonoSystem<IAnimationMonoSystem>().RequestAnimation(
                this,
                duration,
                (float t) =>
                {
                    transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
                }
            );
        }

        public void StopMoving(bool disableAgent = false)
        {
            if (_agent)
            {
                if (_agent.isOnNavMesh)
                {
                    _agent.isStopped = true;
                    _agent.ResetPath();
                }
                if (disableAgent)
                {
                    _agent.enabled = false;
                }
            }
        }

        public void SetChasingMode(bool isChasing)
        {
            if (_animator != null) _animator.IsChasing = isChasing;
        }

        public void SnapToState(MoveState state)
        {
            _state = state;
            if (_agent) _agent.isStopped = true;
            GameManager.GetMonoSystem<IPoseMonoSystem>().LoadPose(ActorName, Enum.GetName(typeof(MoveState), state));
        }

        public Promise SetState(MoveState state, float? duration = null)
        {
            return UpdateState(state, duration);
        }

        public void ChaseTarget(Vector3 targetPosition)
        {
            if (_state != MoveState.Moving) return;
            _targetPos = targetPosition;
            _agent.isStopped = false;
            _agent.SetDestination(_targetPos);
        }

        public void StopMoving()
        {
            if (_agent)
            {
                _agent.isStopped = true;
                _agent.ResetPath();
            }
        }

        public void ResetToPosition(Vector3 position, Quaternion rotation, MoveState initialState)
        {
            if (_agent)
            {
                _agent.enabled = false;
            }

            transform.position = position;
            transform.rotation = rotation;

            if (_agent)
            {
                _agent.enabled = true;
                if (_agent.isOnNavMesh)
                {
                    _agent.Warp(position);
                    _agent.isStopped = true;
                    _agent.ResetPath();
                }
            }

            SetChasingMode(false);
            SnapToState(initialState);
        }
    }
}