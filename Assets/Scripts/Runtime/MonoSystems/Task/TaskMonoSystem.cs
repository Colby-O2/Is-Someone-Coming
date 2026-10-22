using UnityEngine;

using PlazmaGames.Attribute;
using PlazmaGames.Core;
using PlazmaGames.UI;

namespace ColbyO.SJ26.Task
{
    public class TaskMonoSystem : MonoBehaviour, ITaskMonoSystem
    {
        [SerializeField, ReadOnly] private bool _hasTask;
        [SerializeField, ReadOnly] private string _taskMsg;
        [SerializeField, ReadOnly] private int _maxCount;
        [SerializeField, ReadOnly] private int _count;

        private GameView _gameView;

        private Promise _taskPromise;

        private string GetTaskString()
        {
            return (_maxCount > 0) ? $"{_taskMsg} {_count}/{_maxCount}" : _taskMsg;
        }

        public Promise StartTask(string msg, int maxCount = -1)
        {
            _hasTask = true;
            _maxCount = maxCount;
            _taskMsg = msg;
            _count = 0;
            _gameView.ShowTask(GetTaskString());

            Promise.CreateExisting(ref _taskPromise);
            return _taskPromise;
        }

        public void UpdateTask(bool preventAutoEnding = false)
        {
            _count++;
            _gameView.UpdateTask(GetTaskString());
            if (!preventAutoEnding && _maxCount <= _count) EndTask();
        }

        public void EndTask()
        {
            _gameView.HideTask();
            Promise.ResolveExisting(ref _taskPromise);
            _hasTask = false;
            _maxCount = -1;
            _count = 0;
            _taskMsg = string.Empty;
        }

        private void Start()
        {
            _gameView = GameManager.GetMonoSystem<IUIMonoSystem>().GetView<GameView>();
        }
    }
}