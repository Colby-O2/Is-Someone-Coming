using UnityEngine.Events;

using PlazmaGames.Core;
using PlazmaGames.Core.MonoSystem;

namespace ColbyO.SJ26.Dialogue
{
    public interface IDialogueMonoSystem : IMonoSystem
    {
        public bool IsPlayingDialogue();
        public bool GetFlag(string name);
        public void SetFlag(string name, bool value);
        public int? GetInt(string name);
        public void SetInt(string name, int value);
        public void AddListener(string eventTag, UnityAction<int> callback);
        public void StartDialogue(string dialogueName, System.Action finishCallback = null);
        public Promise StartDialoguePromise(string dialogueName, bool passive = false);
        public void OnDialogueNodeFinish(int choice);
        public void OnDialogueFinished();
        public void FinishDialogue();
        public void SelectedChoice(int choice);
    }
}