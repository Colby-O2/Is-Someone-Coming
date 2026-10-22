using PlazmaGames.Core;
using PlazmaGames.Core.MonoSystem;

namespace ColbyO.SJ26.Task
{
    public interface ITaskMonoSystem : IMonoSystem
    {
        public Promise StartTask(string msg, int maxCount = -1);
        public void UpdateTask(bool preventAutoEnding = false);
        public void EndTask();
    }
}