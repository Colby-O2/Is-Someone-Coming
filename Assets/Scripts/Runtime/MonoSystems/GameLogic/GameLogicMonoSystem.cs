using UnityEngine;
using UnityEngine.SceneManagement;

using System.Collections.Generic;

using PlazmaGames.Core;
using PlazmaGames.Core.Debugging;

using InteractionSystem;

using ColbyO.SJ26.Dialogue;
using ColbyO.SJ26.Dispatch;
using ColbyO.Sj26.Effects;
using ColbyO.SJ26.Task;
using ColbyO.SJ26.Entities;
using ColbyO.SJ26.Enviroment;
using ColbyO.VNTG.CRT;
using InteractionSystem;
using Unity.Hierarchy;
using PlazmaGames.Audio;
using PlazmaGames.UI;
using ColbyO.SJ26.UI;
using ColbyO.SJ26.Utilities;

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
        private ILightMonoSystem _lightMs;
        private IDispatchMonoSystem _dispatchMs;
        private Scheduler _sch;

        private static class Refs
        {
            /*Act 1*/
            public static Elevator Elevator;
            public static SitAction PlayerSeat;
            public static Interactable Radio;

            /* Act 3 */
            public static Interactable FlashlightPickup;
            public static CryingGirlEncounter CryingGirlEncounter;
            public static Transform PlayerChaseStart; 
            public static UseDispatchViewAction DispatchMonitor;
            public static GameObject FireGirl;
            public static GameObject FireHallway;
            public static OpenElevatorAction CallElevator;
            public static GameObject Mug;

            public static DoorAction Door1;
            public static DoorAction Door2;
            public static DoorAction Door3;
            public static DoorAction Door4;
            public static GameObject EndFire;
            public static Renderer ScareObject;
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
            _lightMs = GameManager.GetMonoSystem<ILightMonoSystem>();

            Refs.Door1 = GameObject.FindWithTag("Door1").GetComponent<Interactable>().GetAction<DoorAction>();
            Refs.Door2 = GameObject.FindWithTag("Door2").GetComponent<Interactable>().GetAction<DoorAction>();
            Refs.Door3 = GameObject.FindWithTag("Door3").GetComponent<Interactable>().GetAction<DoorAction>();
            Refs.Door4 = GameObject.FindWithTag("Door4").GetComponent<Interactable>().GetAction<DoorAction>();
            Refs.EndFire = GameObject.FindWithTag("EndFire");
            Refs.ScareObject = GameObject.FindWithTag("ScareObject").GetComponent<Renderer>();
            Refs.EndFire.SetActive(false);
            Refs.Radio = GameObject.FindWithTag("Radio").GetComponent<Interactable>();

            Refs.Radio.GetAction<RadioAction>().IsEnabled = false;

            Refs.Elevator = FindAnyObjectByType<Elevator>();
            Refs.PlayerSeat = GameObject.FindWithTag("PlayerSeat").GetComponent<Interactable>().GetAction<SitAction>();

            Refs.FlashlightPickup = GameObject.FindWithTag("Flashlight").GetComponentInChildren<Interactable>();
            Refs.CryingGirlEncounter = FindAnyObjectByType<CryingGirlEncounter>();
            Refs.PlayerChaseStart = GameObject.FindWithTag("PlayerChaseStart").transform;
            Refs.FireGirl = GameObject.FindWithTag("FireGirl");
            Refs.FireHallway = GameObject.FindWithTag("FireHallway");
            Refs.CallElevator = GameObject.FindWithTag("CallElevator").GetComponent<Interactable>().GetAction<OpenElevatorAction>();
            Refs.Mug = GameObject.FindWithTag("Mug");

            Refs.CryingGirlEncounter.gameObject.SetActive(false);
            Refs.FlashlightPickup.gameObject.SetActive(false);
            Refs.FireGirl.SetActive(false);
            Refs.FireHallway.SetActive(false);

            Refs.Mug.SetActive(false);

            Refs.DispatchMonitor = GameObject.FindWithTag("DispatchMonitor").GetComponent<Interactable>().GetAction<UseDispatchViewAction>();
            Refs.DispatchMonitor.ReallyCanInteract = false;
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
                    _actorMonoSystem.SendActorTo("Coworker", "Loc_BreakRoom", 5f)
                    .Then(_ =>
                    {
                        Refs.Door4.RequestClose(true);
                        Refs.Door4.IsLocked = false;
                    });
                    _actorMonoSystem.TellActorToSitAt("Extra1", "SitPoint_Office1", 5f);
                    _actorMonoSystem.TellActorToSitAt("Extra2", "SitPoint_Office2", 5f);

                    CRTSettings crt;

                    _sch.Wait(0.5f)
                    .Then(_ => Promise.All(
                        _visualEffectMs.WriteFadeOutText("October 23rd 22:54\nMississauga, Ontario", 0.1f, 2f)
                            .Then(_ => _visualEffectMs.FadeIn(1f))
                            .Then(_ =>
                            {
                                SJ26GameManager.LockMovement = false;
                            }),
                        Refs.Elevator.StartMoving())
                    )
                    .Then(_ => Refs.Elevator.StopMoving(2.5f))
                    .Then(_ => Refs.Elevator.Open(1.5f))
                    .Then(_ => _sch.When(() => IsInRange("Act1_LeftElevator")))
                    .Then(_ => Refs.Elevator.Close(1.5f))
                    .Then(_ =>
                    {
                        _taskMonoSystem.StartTask("Find Your Desk");
                        return Refs.PlayerSeat.WaitForSit();
                    })
                    .Then(_ =>
                    {
                        SJ26GameManager.PlayerSit.CanUnSit = false;
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
                    })
                    .Then(_ => _dialogueMs.StartDialoguePromise("TurnOnRadio", passive: true))
                    .Then(_ =>
                    {
                        _taskMonoSystem.StartTask("Turn On Radio");
                        return Refs.Radio.GetAction<RadioAction>().WaitForPlay();
                    })
                    .Then(_ =>
                    {
                        _dispatchMs.StartTheCalls();
                        _taskMonoSystem.EndTask();
                        Refs.DispatchMonitor.ReallyCanInteract = true;
                        Refs.Radio.gameObject.GetComponent<AudioSource>().Play();
                    })
                    .Then(_ => _sch.When(() => GetCounter("CallsCompleted") >= 4)) //
                    .Then(_ =>
                    {
                        crt = _visualEffectMs.GetCRTSettings();
                        _dispatchMs.StopTheCalls();

                        _dialogueMs.SetFlag("HideUnits", false);
                        _dialogueMs.SetFlag("Spooky1", false);
                        _dialogueMs.SetFlag("Spooky2", false);
                        _dialogueMs.SetFlag("Spooky3", false);
                        _dispatchMs.RemoveAllEvents();
                        DispatchEventSO ev = SJ26GameManager.Preferences.DispatchEvents.GetEntry("SpookyCall");
                        _dispatchMs.StartCallWith(ev);

                        _sch.When(() => _dispatchMs.InCall() != null && _dispatchMs.InCall().Event == ev)
                            .Then(_ => Refs.DispatchMonitor.WaitForEnter())
                            .Then(_ =>
                            {
                                SJ26GameManager.PlayerInteractor.Controls.InspectionBackAction.Disable();
                                SJ26GameManager.PlayerInteractor.Controls.InspectionLeaveAction.Disable();
                                _actorMonoSystem.GetActor("Coworker").gameObject.GetComponent<WalkingSound>().enabled = false;
                                _actorMonoSystem.TellActorToUnsit("Coworker")
                                .Then(_ => _actorMonoSystem.SendActorTo("Coworker", "SpookAtDesk", 8))
                                .Then(_ => _actorMonoSystem.GetActor("Coworker").LookAt(SJ26GameManager.PlayerInteractor.gameObject.transform, 0.1f))
                                .Then(_ =>
                                {
                                    _actorMonoSystem.GetActor("Coworker").gameObject.GetComponent<WalkingSound>().enabled = true;
                                });
                            })
                            .Then(_ => _sch.When(() => _dialogueMs.GetFlag("Spooky1")))
                            .Then(_ =>
                            {
                                Refs.Radio.gameObject.GetComponent<AudioSource>().Pause();
                                _dispatchMs.HideAllUnits();
                            })
                            .Then(_ => _sch.When(() => !_dialogueMs.IsPlayingDialogue()))
                            .Then(_ => _sch.Wait(3))
                            .Then(_ =>
                            {
                                return _dialogueMs.StartDialoguePromise("SpookyCallContinue", true);
                            });

                        _sch.When(() => _dialogueMs.GetFlag("Spooky1"))
                            .Then(_ =>
                            {
                                crt.EnableNoise.value = true;
                                Utils.Animate(v => crt.NoiseFade.value = v,
                                    0, 0.2f, 3);

                                crt.EnableColorAdjustment.value = true;
                                Utils.Animate(v => crt.RedShift.value = v,
                                    0f, 1f, 3f);
                            });
                        _sch.When(() => _dialogueMs.GetFlag("Spooky2"))
                            .Then(_ =>
                            {
                                Utils.Animate(v => crt.NoiseFade.value = v,
                                    crt.NoiseFade.value, 0.4f, 3);
                            });


                        return _sch.When(() => _dialogueMs.GetFlag("Spooky3"))
                            .Then(_ => Utils.Animate(v => crt.NoiseFade.value = v,
                                    crt.NoiseFade.value, 0.8f, 5)
                            )
                            .Then(_ => _sch.Wait(3))
                            .Then(_ =>
                            {
                                _dispatchMs.MakeFailActiveEvents();
                            })
                            .Then(_ => _visualEffectMs.FadeOut(1f))
                            .Then(_ =>
                            {
                                GameManager.GetMonoSystem<IAudioMonoSystem>().PlayAudio("DeskJumpscare", PlazmaGames.Audio.AudioType.Sfx, loop: false);
                                return _visualEffectMs.FadeIn(0.1f);
                            })
                            .Then(_ =>
                            {
                                _visualEffectMs.ResetCRTSettings();
                                return SJ26GameManager.Player.LookAt(GameObject.FindWithTag("SpookLookAtLoc").transform.position, 0.2f);
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
                        Refs.Door4.RequestClose(true);
                        Refs.Door4.IsLocked = false;
                        Refs.Radio.gameObject.GetComponent<AudioSource>().UnPause();
                        SJ26GameManager.PlayerSit.CanUnSit = true;
                        _actorMonoSystem.TellActorToSitAt("Coworker", "SitPoint_CoworkerOffice", 1f);
                    })
                    .Then(_ =>
                    {
                        Refs.DispatchMonitor.ReallyCanInteract = false;
                        Refs.Mug.SetActive(true);
                        _taskMonoSystem.StartTask("Go Get a Coffee");
                    })
                    .Then(_ => _sch.When(() => IsInRange("CoffeeBrewed")))
                    .Then(_ =>
                    {
                        Refs.PlayerSeat.IsEnabled = false;
                        _taskMonoSystem.EndTask();
                        _taskMonoSystem.StartTask("Bring Coffee Back to Desk");
                    })
                    .Then(_ => _sch.When(() => IsInRange("Mug")))
                    .Then(_ =>
                    {
                        Refs.PlayerSeat.IsEnabled = true;
                        _taskMonoSystem.EndTask();
                    })
                    .Then(_ => Refs.PlayerSeat.WaitForSit())
                    .Then(_ =>
                    {
                        SJ26GameManager.PlayerSit.CanUnSit = false;
                        Refs.DispatchMonitor.ReallyCanInteract = true;
                        _dispatchMs.StartTheCalls();
                    })
                    .Then(_ => _sch.When(() => GetCounter("CallsCompleted") >= 8))
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
                    Refs.Radio.gameObject.GetComponent<AudioSource>().Pause();
                    return SJ26GameManager.GlobalScheduler.Wait(1f);
                })
                .Then(_ =>
                    {
                        Refs.FlashlightPickup.gameObject.SetActive(true);
                    })
                    .Then(_ =>
                    {
                        SJ26GameManager.PlayerSit.CanUnSit = true;
                        _actorMonoSystem.GetActor("Coworker").gameObject.SetActive(false);              
                        _actorMonoSystem.GetActor("Extra1").gameObject.SetActive(false);
                        _actorMonoSystem.GetActor("Extra2").gameObject.SetActive(false);
                        _lightMs.TriggerOutageWithEmergencyBackup(waveOrigin: SJ26GameManager.Player.transform.position);
                    })
                    .Then(_ =>
                    {
                        _taskMonoSystem.StartTask("Find Flashlight in Office Area");

                        Refs.Door2.RequestClose(true);
                        Refs.Door2.OnLockedDialogueName = "NeedToFindKey";
                        Refs.Door2.IsLocked = true;

                        Refs.Door3.RequestClose(true);
                        Refs.Door3.OnLockedDialogueName = "NeedToFindKey";
                        Refs.Door3.IsLocked = true;
                    })
                    .Then(_ => Refs.FlashlightPickup.GetAction<PickupAction>().WaitForPickup())
                    .Then(_ =>
                    {
                        Refs.Door2.IsLocked = false;
                        Refs.Door3.IsLocked = false;
                    })
                    .Then(_ =>
                    {
                        Refs.Door1.IsLocked = false;
                        _taskMonoSystem.EndTask();
                        SJ26GameManager.Player.EnableFlashlight();
                        SJ26GameManager.PlayerAnimator.HoldingLight = true;
                        _taskMonoSystem.StartTask("Restore The Power");
                        Refs.CryingGirlEncounter.gameObject.SetActive(true);
                        Refs.CryingGirlEncounter.ResetEncounter();
                        Refs.CryingGirlEncounter.OnJumpscareFinished.AddListener(ResetChase);
                        Refs.FireGirl.SetActive(true);
                        Debug.Log("Waiting for Range");
                        return SJ26GameManager.GlobalScheduler.When(() => IsInRange("InRangeCryingGirl"));
                    })
                    .Then(_ =>
                    {
                        Refs.Door1.RequestClose(true);
                        Refs.Door1.IsLocked = true;

                        SJ26GameManager.GlobalScheduler.When(() => IsInRange("InCryingGirlRoom"))
                        .Then(_ =>
                        {
                            SJ26GameManager.LockMovement = true;
                        });
                    })
                    .Then(_ => Refs.CryingGirlEncounter.WaitForEvent())
                    .Then(_ =>
                    {
                        SJ26GameManager.Player.LookAt(Refs.CryingGirlEncounter.transform.position);

                        Refs.Door1.IsLocked = false;

                        Refs.Door1.RequestOpen();
                        Refs.Door1.IsLocked = true;

                        Refs.Door2.RequestOpen();
                        Refs.Door2.OnLockedDialogueName = string.Empty;
                        Refs.Door2.IsLocked = true;

                        Refs.Door3.RequestOpen();
                        Refs.Door3.OnLockedDialogueName = string.Empty;
                        Refs.Door3.IsLocked = true;

                        Refs.Door4.RequestClose(true);
                        Refs.Door4.IsLocked = true;

                        Refs.FireHallway.SetActive(true);

                        Refs.CallElevator.IsEnabled = true;
                    })
                    .Then(_ => Promise.All(_dialogueMs.StartDialoguePromise("CryingGirlEncounter"), SJ26GameManager.GlobalScheduler.When(() => Refs.CryingGirlEncounter.Phase == CryingGirlEncounter.EncounterPhase.WaitingToChase)))
                    .Then(_ =>
                    {
                        SJ26GameManager.LockMovement = false;
                        Refs.CryingGirlEncounter.Force = true;
                        SJ26GameManager.Player.IsRunning = true;

                        _taskMonoSystem.EndTask();
                        _taskMonoSystem.StartTask("RUN");
                    })
                    .Then(_ => Refs.CryingGirlEncounter.TriggerChase())
                    .Then(_ => SJ26GameManager.GlobalScheduler.When(() => IsInRange("InsideElevatorNotify")))
                    .Then(_ =>
                    {
                        _taskMonoSystem.EndTask();
                        SJ26GameManager.Player.IsRunning = false;
                        Refs.CryingGirlEncounter.Stop();
                        return Refs.Elevator.Close(1.5f);
                    })
                    .Then(_ =>
                    {
                        Refs.FireHallway.SetActive(false);
                    })
                    .Then(_ => Refs.Elevator.StartMoving())
                    .Then(_ => Refs.Elevator.StopMoving(2.5f))
                    .Then(_ =>
                    {
                        Refs.EndFire.SetActive(true);
                        return Refs.Elevator.Open(1.5f);
                    })
                    .Then(_ => _sch.Wait(0.5f))
                    .Then(_ => _sch.When(() => !Utils.CanCameraSee(SJ26GameManager.Player.Camera, Refs.ScareObject)))
                    .Then(_ => _sch.Wait(0.4f))
                    .Then(_ => _sch.When(() => Utils.CanCameraSee(SJ26GameManager.Player.Camera, Refs.ScareObject)))
                    .Then(_ =>
                    {
                        return Refs.CryingGirlEncounter.ForceJumpscare();
                    })
                    .Then(_ => _visualEffectMs.FadeOut(0.5f))
                    .Then(_ =>
                    {
                        GameManager.GetMonoSystem<IUIMonoSystem>().Show<EndView>();
                    })
                    .Then(_ => _visualEffectMs.FadeIn(2f));

                break;

            }
        }

        private void ResetChase()
        {
            Refs.CryingGirlEncounter.ResetEncounter();
            Refs.CryingGirlEncounter.ForceEvent();
            SJ26GameManager.Player.MoveTo(Refs.PlayerChaseStart.position);
            Refs.Elevator.Close();
            Refs.CallElevator.IsEnabled = true;
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
