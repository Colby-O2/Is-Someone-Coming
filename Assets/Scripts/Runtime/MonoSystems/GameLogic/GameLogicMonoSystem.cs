using UnityEngine;
using UnityEngine.SceneManagement;

using System.Collections.Generic;

using PlazmaGames.Core;
using PlazmaGames.Core.Debugging;

using InteractionSystem;

using ColbyO.SJ26.Dialogue;
using ColbyO.Sj26.Effects;
using ColbyO.SJ26.Task;
using ColbyO.SJ26.Entities;
using ColbyO.SJ26.Enviroment;

namespace ColbyO.SJ26.GameLogic
{
    public class GameLogicMonoSystem : MonoBehaviour, IGameLogicMonoSystem
    {
        private Scheduler _scheduler = new Scheduler();
        private HashSet<string> _inRange = new HashSet<string>();
        private Dictionary<string, int> _counters = new(); 
        private HashSet<string> _triggers = new HashSet<string>();
        private bool _started = false;

        private IDialogueMonoSystem _dialogueMs;
        private IVisualEffectMonoSystem _visualEffectMs;
        private ITaskMonoSystem _taskMonoSystem;
        private IActorMonoSystem _actorMonoSystem;
        private ILightMonoSystem _lightMs;

        private static class Refs
        {
            /*Act 1*/
            public static Elevator Elevator;
            public static SitAction PlayerSeat;

            /* Act 3 */
            public static Interactable FlashlightPickup;
            public static CryingGirlEncounter CryingGirlEncounter;
            public static Transform PlayerChaseStart; 
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
            _dialogueMs = GameManager.GetMonoSystem<IDialogueMonoSystem>();
            _visualEffectMs = GameManager.GetMonoSystem<IVisualEffectMonoSystem>();
            _taskMonoSystem = GameManager.GetMonoSystem<ITaskMonoSystem>();
            _actorMonoSystem = GameManager.GetMonoSystem<IActorMonoSystem>();
            _lightMs = GameManager.GetMonoSystem<ILightMonoSystem>();
            Refs.Elevator = FindAnyObjectByType<Elevator>();
            Refs.PlayerSeat = GameObject.FindWithTag("PlayerSeat").GetComponent<Interactable>().GetAction<SitAction>();

            Refs.FlashlightPickup = GameObject.FindWithTag("Flashlight").GetComponentInChildren<Interactable>();
            Refs.CryingGirlEncounter = FindAnyObjectByType<CryingGirlEncounter>();
            Refs.PlayerChaseStart = GameObject.FindWithTag("PlayerChaseStart").transform;

            Refs.CryingGirlEncounter.gameObject.SetActive(false);
            Refs.FlashlightPickup.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!_started)
            {
                _started = true;
                TriggerEvent("Act3");
            }

            _scheduler.Tick(Time.deltaTime);
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

                    _visualEffectMs.FadeOut(0.01f)
                    .Then(_ => Promise.All(
                        _visualEffectMs.WriteFadeOutText("October 23rd 22:54\nMississauga, Ontario", 0.1f, 2f)
                            .Then(_ => _visualEffectMs.FadeIn(1f)),
                        Refs.Elevator.StartMoving())
                    )
                    .Then(_ => Refs.Elevator.StopMoving(2.5f))
                    .Then(_ => Refs.Elevator.Open(1.5f))
                    .Then(_ => SJ26GameManager.GlobalScheduler.When(() => IsInRange("Act1_LeftElevator")))
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
                    });
                    break;

                case "Act3":
                    Refs.FlashlightPickup.gameObject.SetActive(true);

                    SJ26GameManager.GlobalScheduler.Wait(5f)
                    .Then(_ =>
                    {
                        _lightMs.TriggerOutageWithEmergencyBackup(waveOrigin: SJ26GameManager.Player.transform.position);
                    })
                    .Then(_ =>
                    {
                        _taskMonoSystem.StartTask("Find Flashlight in Office Area");
                    })
                    .Then(_ => Refs.FlashlightPickup.GetAction<PickupAction>().WaitForPickup())
                    .Then(_ =>
                    {
                        _taskMonoSystem.EndTask();
                        SJ26GameManager.Player.EnableFlashlight();
                        SJ26GameManager.PlayerAnimator.HoldingLight = true;
                        _taskMonoSystem.StartTask("Restore The Power");
                        Refs.CryingGirlEncounter.gameObject.SetActive(true);
                        Refs.CryingGirlEncounter.ResetEncounter();
                        Refs.CryingGirlEncounter.OnJumpscareFinished.AddListener(ResetChase);
                        return Refs.CryingGirlEncounter.WaitForEvent();
                    })
                    .Then(_ => Promise.All(_dialogueMs.StartDialoguePromise("CryingGirlEncounter"), SJ26GameManager.GlobalScheduler.When(() => Refs.CryingGirlEncounter.Phase == CryingGirlEncounter.EncounterPhase.WaitingToChase)))
                    .Then(_ => SJ26GameManager.GlobalScheduler.Wait(1f))
                    .Then(_ => Refs.CryingGirlEncounter.TriggerChase());

                    break;

            }
        }

        private void ResetChase()
        {
            Refs.CryingGirlEncounter.ResetEncounter();
            SJ26GameManager.Player.MoveTo(Refs.PlayerChaseStart.position);
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