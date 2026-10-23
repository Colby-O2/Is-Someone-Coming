using UnityEngine;
using UnityEngine.SceneManagement;

using System.Collections.Generic;

using PlazmaGames.Core;
using PlazmaGames.Core.Debugging;

using ColbyO.SJ26.Dialogue;
using ColbyO.SJ26.Dispatch;
using ColbyO.Sj26.Effects;
using ColbyO.SJ26.Task;
using ColbyO.SJ26.Entities;
using ColbyO.VNTG.CRT;
using InteractionSystem;
using Unity.Hierarchy;

namespace ColbyO.SJ26.GameLogic
{
    public class GameLogicMonoSystem : MonoBehaviour, IGameLogicMonoSystem
    {
        private HashSet<string> _inRange = new HashSet<string>();
        private Dictionary<string, int> _counters = new(); 
        private HashSet<string> _triggers = new HashSet<string>();
        private bool _started = false;

        private IDialogueMonoSystem _dialogueMs;
        private IVisualEffectMonoSystem _visualEffectMs;
        private ITaskMonoSystem _taskMonoSystem;
        private IActorMonoSystem _actorMonoSystem;
        private IDispatchMonoSystem _dispatchMs;
        private Scheduler _sch;

        private static class Refs
        {
            /*Act 1*/
            public static Elevator Elevator;
            public static SitAction PlayerSeat;
            public static UseDispatchViewAction DispatchMonitor;
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

        private void Start()
        {
            _sch = SJ26GameManager.GlobalScheduler;
            _dialogueMs = GameManager.GetMonoSystem<IDialogueMonoSystem>();
            _dispatchMs = GameManager.GetMonoSystem<IDispatchMonoSystem>();
            _visualEffectMs = GameManager.GetMonoSystem<IVisualEffectMonoSystem>();
            _taskMonoSystem = GameManager.GetMonoSystem<ITaskMonoSystem>();
            _actorMonoSystem = GameManager.GetMonoSystem<IActorMonoSystem>();
            Refs.Elevator = FindAnyObjectByType<Elevator>();
            Refs.PlayerSeat = GameObject.FindWithTag("PlayerSeat").GetComponent<Interactable>().GetAction<SitAction>();
            Refs.DispatchMonitor = GameObject.FindWithTag("DispatchMonitor").GetComponent<Interactable>().GetAction<UseDispatchViewAction>();
        }

        private void Update()
        {
            if (!_started)
            {
                _started = true;
                TriggerEvent("Act1");
            }
        }

        private void OnSceneLoad(Scene scene, LoadSceneMode mode)
        {

        }

        private void OnSceneUnload(Scene scene)
        {

        }

        public void TriggerEvent(string eventName)
        {
            PlazmaDebug.Log("Event Triggered", eventName, Color.green);

            switch (eventName)
            {
                case "Act1":
                    _actorMonoSystem.SendActorTo("Coworker", "Loc_BreakRoom", 5f);
                    _actorMonoSystem.TellActorToSitAt("Extra1", "SitPoint_Office1", 5f);
                    _actorMonoSystem.TellActorToSitAt("Extra2", "SitPoint_Office2", 5f);

                    /*
                    _visualEffectMs.FadeOut(0.01f)
                    .Then(_ => Promise.All(
                        _visualEffectMs.WriteFadeOutText("October 23rd 22:54\nMississauga, Ontario", 0.1f, 2f)
                            .Then(_ => _visualEffectMs.FadeIn(1f)),
                        Refs.Elevator.StartMoving())
                    )
                    .Then(_ => Refs.Elevator.StopMoving(2.5f))
                    .Then(_ => Refs.Elevator.Open(1.5f))
                    .Then(_ => _sch.When(() => IsInRange("Act1_LeftElevator")))
                    .Then(_ => Refs.Elevator.Close(1.5f))
                    .Then(_ => {
                        _taskMonoSystem.StartTask("Find Your Desk");
                        return Refs.PlayerSeat.WaitForSit();
                    })
                    .Then(_ =>
                    {
                        _taskMonoSystem.EndTask();
                    })
                    .Then(_ => _actorMonoSystem.SendActorTo("Coworker", "Loc_PlayersOffice", 2f))
                    .Then(_ => 
                    {
                        Debug.Log("Here 2345!");
                        Actor coworker = _actorMonoSystem.GetActor("Coworker");
                        SJ26GameManager.PlayerSit.LockHead = true;
                        SJ26GameManager.PlayerSit.RetargetHeadLookAt(coworker.GetFace().position);
                        return coworker.LookAt(SJ26GameManager.Player.transform, 0.5f);
                    })
                    .Then(_ => _dialogueMs.StartDialoguePromise("FirstMeetingWithCoworker"))
                    .Then(_ =>
                    {
                        SJ26GameManager.PlayerSit.RestoreHeadLook();
                        SJ26GameManager.PlayerSit.LockHead = false;
                        _actorMonoSystem.TellActorToSitAt("Coworker", "SitPoint_CoworkerOffice", 1f);
                    })*/
                    Debug.Log("------------------------------------------------------------------------");
                    CRTSettings crt = null;
                    _sch.Wait(0.1f)
                        .Then(_ => _sch.When(() => GetCounter("CallsCompleted") >= 1))
                        .Then(_ =>
                        {
                            //crt = _visualEffectMs.GetCRTSettings();
                            _dispatchMs.StopTheCalls();

                            _dialogueMs.SetFlag("HideUnits", false);
                            _dialogueMs.SetFlag("Spooky1", false);
                            _dialogueMs.SetFlag("Spooky2", false);
                            _dialogueMs.SetFlag("Spooky3", false);
                            _dispatchMs.RemoveAllEvents();
                            _dispatchMs.StartCallWith(SJ26GameManager.Preferences.DispatchEvents.GetEntry("SpookyCall"));

                            Refs.DispatchMonitor.WaitForEnter()
                                .Then(_ =>
                                {
                                    SJ26GameManager.PlayerInteractor.Controls.InspectionBackAction.Disable();
                                    SJ26GameManager.PlayerInteractor.Controls.InspectionLeaveAction.Disable();
                                    _actorMonoSystem.SendActorTo("Coworker", "SpookAtDesk", 8)
                                        .Then(_ =>
                                        {
                                            _actorMonoSystem.GetActor("Coworker").LookAt(SJ26GameManager.PlayerInteractor.gameObject.transform, 0.1f);
                                        });
                                })
                                .Then(_ => _sch.When(() => _dialogueMs.GetFlag("Spooky1")))
                                .Then(_ => _sch.When(() => !_dialogueMs.IsPlayingDialogue()))
                                .Then(_ => _sch.Wait(3))
                                .Then(_ =>
                                {
                                    return _dialogueMs.StartDialoguePromise("SpookyCallContinue", true);
                                });

                            _sch.When(() => _dialogueMs.GetFlag("HideUnits"))
                                .Then(_ =>
                                {
                                    _dispatchMs.HideAllUnits();
                                });

                            _sch.When(() => _dialogueMs.GetFlag("Spooky1"))
                                .Then(_ =>
                                {
                                    //crt.EnableNoise.value = true;
                                    //Utils.Animate(v => crt.NoiseFade.value = v,
                                    //   0, 0.2f, 3);
                                });
                            _sch.When(() => _dialogueMs.GetFlag("Spooky2"))
                                .Then(_ =>
                                {
                                    //Utils.Animate(v => crt.NoiseFade.value = v, 
                                    //   crt.NoiseFade.value, 0.4f, 3);
                                });


                            return _sch.When(() => _dialogueMs.GetFlag("Spooky3"))
                                /*.Then(_ => Utils.Animate(v => crt.NoiseFade.value = v,
                                        crt.NoiseFade.value, 0.8f, 5)
                                )*/
                                .Then(_ => _sch.Wait(3))
                                .Then(_ =>
                                {
                                    _dispatchMs.MakeFailActiveEvents();
                                })
                                .Then(_ =>
                                {
                                    return SJ26GameManager.Player.LookAt(GameObject.FindWithTag("Test").transform.position, 0.2f);
                                })
                                .Then(_ => _sch.Wait(0.3f))
                                .Then(_ =>
                                {
                                    _dialogueMs.StartDialoguePromise("SpookAtDesk", true);
                                })
                                .Then(_ => _sch.Wait(1.5f))
                                .Then(_ =>
                                {
                                    SJ26GameManager.PlayerInteractor.Controls.InspectionBackAction.Enable();
                                    SJ26GameManager.PlayerInteractor.Controls.InspectionLeaveAction.Enable();
                                    SJ26GameManager.PlayerInteractor.GetInspectorHandler().StopInspecting(SJ26GameManager.Player.transform);
                                    _dispatchMs.UnHideAllUnits();
                                })
                                .Then(_ => _dialogueMs.StartDialoguePromise("SpookAtDeskAfter"));
                        })
                        .Then(_ =>
                        {
                            _actorMonoSystem.SendActorTo("Coworker", "Loc_BreakRoom", 5f);
                        })
                        .Then(_ =>
                        {
                            _taskMonoSystem.StartTask("Go get a coffee");
                        })
                        .Then(_ => _sch.When(() => IsInRange("CoffeeBrewed")))
                        .Then(_ =>
                        {
                            _taskMonoSystem.EndTask();
                            _taskMonoSystem.StartTask("Bring coffee back to desk");
                        })
                        .Then(_ => _sch.When(() => IsInRange("Mug")))
                        .Then(_ =>
                        {
                            _taskMonoSystem.EndTask();
                            _dispatchMs.StartTheCalls();
                        })
                        .Then(_ => _sch.When(() => GetCounter("CallsCompleted") >= 2))
                        .Then(_ =>
                        {
                            _dispatchMs.StopTheCalls();
                            _dispatchMs.RemoveAllEvents();
                            _dialogueMs.SetFlag("LightsOut", false);
                            _dialogueMs.SetFlag("HideUnits", false);
                            _dispatchMs.StartCallWith(SJ26GameManager.Preferences.DispatchEvents.GetEntry("SpookyCall2"));
                            _sch.When(() => _dialogueMs.GetFlag("HideUnits"))
                                .Then(_ =>
                                {
                                    _dispatchMs.HideAllUnits();
                                });

                            return _sch.When(() => _dialogueMs.GetFlag("LightsOut"));
                        })
                        .Then(_ =>
                        {
                            Debug.Log("THE REST OF THE GAME");
                        });
                    break;

            }
        }

        private int GetCounter(string name)
        {
            return _counters.GetValueOrDefault(name, 0);
        }

        public void Trigger(string triggerName)
        {
            _triggers.Add(triggerName);
        }

        public void SetInRange(string rangeName, bool state)
        {
            if (state) _inRange.Add(rangeName);
            else _inRange.Remove(rangeName);

            switch (rangeName)
            {
                default: break;
            }
        }

        private bool IsTriggered(string triggerName) => _triggers.Remove(triggerName);

        public bool IsInRange(string rangeName) => _inRange.Contains(rangeName);
        public void IncrementCounter(string counterName, int count = 1)
        {
            _counters.TryAdd(counterName, 0);
            _counters[counterName] += 1;
        }

        public void DecrementCounter(string counterName, int count = 1)
        {
            _counters.TryAdd(counterName, 0);
            _counters[counterName] -= 1;
        }
    }
}