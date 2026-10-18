
namespace PlazmaGames.Core.FSM
{
    public interface ITransitionCallback
    {
        public abstract void BeforeTransition();
        public abstract void AfterTransition();
    }
}
