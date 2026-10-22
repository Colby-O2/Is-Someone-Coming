using PlazmaGames.Core.MonoSystem;

namespace ColbyO.SJ26.GameLogic
{
    public interface IGameLogicMonoSystem : IMonoSystem
    {
        public void TriggerEvent(string eventName);
        public void Trigger(string triggerName);
        public void SetInRange(string rangeName, bool state);
        public bool IsInRange(string rangeName);
    }
}