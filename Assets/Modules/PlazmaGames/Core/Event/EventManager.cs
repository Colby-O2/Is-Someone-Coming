using PlazmaGames.Core.Debugging;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace PlazmaGames.Core.Events
{
    public class EventManager
    {
        private IDictionary<string, GameEvent> _events = new Dictionary<string, GameEvent>();

        public void Emit(string eventID, Component sender = null, object data = null)
        {
            try
            {
                if (HasEvent(eventID))
                {
                    _events[eventID].Invoke(sender, data);
                    PlazmaDebug.Log($"Called event of type {eventID}.", "Event", Color.green, 2);
                }
            }
            catch (Exception e)
            {
                PlazmaDebug.LogExpection(e, string.Empty, "EventSystem");
            }
        }

        public void AddListener(string eventID, EventResponse listener)
        {
            if (!HasEvent(eventID))
            {
                GameEvent e = new GameEvent();
                e.AddListener(listener);
                _events.Add(eventID, e);
                PlazmaDebug.Log($"Adding event of type {eventID}.", "Event", Color.green, 2);
            }
            else
            {
                _events[eventID].AddListener(listener);
                PlazmaDebug.Log($"Adding a reponse from an event of type {eventID}.", "Event", Color.green, 2);
            }
        }

        public void RemoveListener(string eventID, EventResponse listener)
        {
            if (HasEvent(eventID)) 
            { 

                GameEvent e = _events[eventID];
                if (e.ListenerCount > 0)
                {
                    e.RemoveListener(listener);
                    PlazmaDebug.Log($"Removing event of type {eventID}.", "Event", Color.green, 2);
                }

                if (e.ListenerCount <= 0)
                {
                    _events.Remove(eventID);
                    PlazmaDebug.Log($"Removing a reponse from an event of type {eventID}.", "Event", Color.green, 2);
                }
            }
        }

        public void RemoveAllListener()
        {
            _events = new Dictionary<string, GameEvent>();
        }

        public bool HasEvent(string eventID) 
        {
            return _events.ContainsKey(eventID);
        }
    }
}
