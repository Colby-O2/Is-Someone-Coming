using PlazmaGames.Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace PlazmaGames.Core.Events
{
    [System.Serializable]
    public class GameEventCallback : UnityEvent<Component, object> { }

    [System.Serializable]
    public class EventResponse
    {
        public int priority = 0; 
        public GameEventCallback response = new GameEventCallback();

        public EventResponse(params UnityAction<Component, object>[] actions)
        {
            foreach (UnityAction<Component, object> action in actions) response.AddListener(action);
        }

        internal void Invoke(Component sender, object data)
        {
            response?.Invoke(sender, data);
        }
    }
}
