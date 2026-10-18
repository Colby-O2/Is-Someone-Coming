using PlazmaGames.Core.MonoSystem;
using System;

namespace PlazmaGames.UI
{
    public interface IUIMonoSystem : IMonoSystem
    {
        /// <summary>
        /// Checks if current View is of type T
        /// </summary>
        public bool GetCurrentViewIs<T>() where T : View;

        /// <summary>
        /// Gets the current view if the view is of type T
        /// </summary>
        public T GetCurrentView<T>() where T : View;

        /// <summary>
        /// Gets a View of type T from the master view list
        /// </summary>
        public T GetView<T>() where T : View;

        /// <summary>
        /// Displays a view of type T. Remeber parameter indicates if to add the view
        /// to the history or not. 
        /// </summary>
        public void Show<T>(bool remeber = true, bool hideLastView = true, bool toggleLayer = true) where T : View;

        public void Show(System.Type type, bool remeber = true, bool hideLastView = true, bool toggleLayer = true);

        /// <summary>
        /// Displays a view of type TNext After a tranition to view of type TIntermediate. 
        /// THe transition to TNext will not take place untill the canTransitionCallback return true.
        /// Remeber parameter indicates if to add the view
        /// to the history or not. 
        /// </summary>
        public void ShowWithTransition<TNext, TIntermediate>(Func<bool> canTransitionCallback, bool remeber = true, bool hideLastView = true, bool toggleLayer = false) where TNext : View where TIntermediate : View;
        public void ShowWithTransition(System.Type nextViewType, System.Type transitionViewType, Func<bool> canTransitionCallback, bool remeber = true, bool hideLastView = true, bool toggleLayer = false);
        public void ShowLastWithTransition<TIntermediate>(Func<bool> canTransitionCallback, bool hideLastView = true, bool toggleLayer = false) where TIntermediate : View;

        /// <summary>
        /// Displays the last view in the history
        /// </summary>
        public void ShowLast();

        /// <summary>
        /// Hides all Views.
        /// </summary>
        public void HideAllViews();

        /// <summary>
        /// Shows all Views.
        /// </summary>
        public void ShowAllViews();

        public bool IsInTransition();

        public void ClearHistory();
    }
}
