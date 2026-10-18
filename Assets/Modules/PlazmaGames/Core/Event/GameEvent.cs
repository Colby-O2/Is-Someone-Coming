using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PlazmaGames.Core.Events
{
    public class GameEvent
    {
        private IList<EventResponse> _listeners = new List<EventResponse>();

        public int ListenerCount { get => _listeners.Count; }

        public void Invoke(Component sender, object data)
        {
            foreach (EventResponse listener in _listeners.OrderBy(e => -e.priority)) listener.Invoke(sender, data);
        }

        public void AddListener(EventResponse listener)
        {
            _listeners.Add(listener);
        }

        public void RemoveListener(EventResponse listener)
        {
            if (_listeners.Contains(listener))
            {
                _listeners.Remove(listener);
            }
        }
    }
}
