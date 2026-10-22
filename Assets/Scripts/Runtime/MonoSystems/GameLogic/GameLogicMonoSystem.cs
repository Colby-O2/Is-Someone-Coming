using UnityEngine;
using UnityEngine.SceneManagement;

using System.Collections.Generic;

using PlazmaGames.Core;
using PlazmaGames.Core.Debugging;

using ColbyO.SJ26.Dialogue;
using ColbyO.Sj26.Effects;
using ColbyO.SJ26.Task;
using ColbyO.SJ26.Entities;
using InteractionSystem;

namespace ColbyO.SJ26.GameLogic
{
    public class GameLogicMonoSystem : MonoBehaviour, IGameLogicMonoSystem
    {
        private Scheduler _scheduler = new Scheduler();
        private HashSet<string> _inRange = new HashSet<string>();
        private HashSet<string> _triggers = new HashSet<string>();
        private bool _started = false;

        private IDialogueMonoSystem _dialogueMs;
        private IVisualEffectMonoSystem _visualEffectMs;
        private ITaskMonoSystem _taskMonoSystem;
        private IActorMonoSystem _actorMonoSystem;

        private static class Refs
        {
            /*Act 1*/
            public static Elevator Elevator;
            public static SitAction PlayerSeat;
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

            Refs.Elevator = FindAnyObjectByType<Elevator>();
            Refs.PlayerSeat = GameObject.FindWithTag("PlayerSeat").GetComponent<Interactable>().GetAction<SitAction>();
        }

        private void Update()
        {
            if (!_started)
            {
                _started = true;
                TriggerEvent("Act1");
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

            }
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
    }
}