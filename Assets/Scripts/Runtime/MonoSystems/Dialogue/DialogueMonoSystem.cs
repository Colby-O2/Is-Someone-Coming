using UnityEngine;
using UnityEngine.SceneManagement;

using PlazmaGames.Core;
using PlazmaGames.UI;

using DialogueGraph;
using DialogueGraph.Enumeration;

using ColbyO.SJ26.Cinematic;
using ColbyO.SJ26.UI;

namespace ColbyO.SJ26.Dialogue
{ 
    public class DialogueMonoSystem : DialogueController, IDialogueMonoSystem
    {
        private GameObject _cinematicView;

        private IUIMonoSystem _uiSystem;

        private bool _isPassive;

        private void Start()
        {
            _uiSystem = GameManager.GetMonoSystem<IUIMonoSystem>();
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
            _cinematicView = GameObject.FindWithTag("CinematicView");
            DisabeCinematicCamera();
        }

        private void OnSceneUnload(Scene scene)
        {
            FinishDialogue();
        }


        private void StartDialogue(string dialogueName, System.Action finsihCallback, bool passive)
        {
            _isPassive = passive;

            if (!passive)
            {
                SJ26GameManager.LockMovement = true;
            }

            base.StartDialogue(dialogueName, finsihCallback);
        }
        public override void StartDialogue(string dialogueName, System.Action finsihCallback = null)
        {
            StartDialogue(dialogueName, finsihCallback, false);
        }

        public Promise StartDialoguePromise(string dialogueName, bool passive = false)
        {
            Promise p = new Promise();
            _isPassive = passive;
            StartDialogue(dialogueName, () => Promise.ResolveExisting(ref p), passive);
            return p;
        }

        protected override void EnableCinematicCamera()
        {
            GameManager.GetMonoSystem<ICinematicMonoSystem>().Enable();
        }

        protected override void DisabeCinematicCamera()
        {
            GameManager.GetMonoSystem<ICinematicMonoSystem>().Disabe(false);
        }

        protected override void MoveCinematicCamera(Transform loc, Transform lookAt, Transform parnet)
        {
            GameManager.GetMonoSystem<ICinematicMonoSystem>().MoveTo(loc, lookAt: lookAt, parnet: parnet);
        }

        protected override void TransitionCinematicCamera(Transform from, Transform to, Transform lookAt, float duration, Transform parent = null)
        {
            GameManager.GetMonoSystem<ICinematicMonoSystem>().HandleCameraTransition(from, to, lookAt, duration, parent)
            .Then(_ =>
            {
                NextNode(0);
            });
        }

        protected override void CinematicCameraLookAt(Transform lookAtLoc, float duration)
        {
            GameManager.GetMonoSystem<ICinematicMonoSystem>().HandleCameraLookAt(lookAtLoc, duration)
            .Then(_ =>
            {
                NextNode(0);
            });
        }

        public override void OnDialogueFinished()
        {
            if (!_isPassive) SJ26GameManager.LockMovement = false;
            //GameManager.GetMonoSystem<IUIMonoSystem>().ShowLast();
        }

        public void SelectedChoice(int choice)
        {
            if (_currentDialogueNode == null) return;
            if (_currentDialogueNode.Choices.Count == 0) FinishDialogue();
            else NextNode(choice);
        }

        protected override void PlayDialogueNode(DialogueType type)
        {
            //if (!_uiSystem.GetCurrentViewIs<DialogueView>()) _uiSystem.Show<DialogueView>(hideLastView: false);

            if (type == DialogueType.SingleChoice)
            {
                _uiSystem.GetView<DialogueView>().DisplayMessage(_currentDialogueNode.ActorName, _currentDialogueNode.Text, _isPassive);
            }
            else if (type == DialogueType.MultipleChoice)
            {
                _uiSystem.GetView<DialogueView>().DisplayChoices(_currentDialogueNode.Choices);
            }
        }
    }
}