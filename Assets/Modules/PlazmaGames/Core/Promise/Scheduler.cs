using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

namespace PlazmaGames.Core
{
    public class Scheduler
    {
        private Promise p;
        class TimedEvent
        {
            public float time;
            public Promise promise;

            public TimedEvent(float time, Promise promise)
            {
                this.time = time;
                this.promise = promise;
            }
        }
        
        class ConditionalEvent
        {
            public System.Func<bool> condition;
            public Promise promise;

            public ConditionalEvent(System.Func<bool> condition, Promise promise)
            {
                this.condition = condition;
                this.promise = promise;
            }
        }

        class IntervalEvent
        {
            public System.Func<bool> func;
            public float interval;
            public Promise promise;

            public IntervalEvent(System.Func<bool> func, float interval, Promise promise)
            {
                this.func = func;
                this.interval = interval;
                this.promise = promise;
            }
        }
        
        private float _time = 0;
        private List<ConditionalEvent> _queuedConditionalEvents = new();
        private List<ConditionalEvent> _conditionalEvents = new();
        
        private List<TimedEvent> _timedEvents = new();
        private List<TimedEvent> _queuedTimedEvents = new();
        
        private List<IntervalEvent> _queuedIntervals = new();
        private List<IntervalEvent> _intervals = new();

        public Promise When(System.Func<bool> condition)
        {
            Promise p = new();
            _queuedConditionalEvents.Add(new ConditionalEvent(condition, p));
            return p;
        }

        public Promise Wait(float seconds)
        {
            Promise p = new();
            _queuedTimedEvents.Add(new TimedEvent(_time + seconds, p));
            return p;
        }

        public Promise Every(float seconds, System.Func<bool> func)
        {
            Promise p = new Promise();
            _queuedIntervals.Add(new IntervalEvent(func, seconds, p));
            return p;
        }

        public void Tick(float deltaTime)
        {
            if (_queuedConditionalEvents.Count > 0)
            {
                _conditionalEvents.AddRange(_queuedConditionalEvents);
                _queuedConditionalEvents.Clear();
            }
            
            if (_queuedTimedEvents.Count > 0)
            {
                _timedEvents.AddRange(_queuedTimedEvents);
                _queuedTimedEvents.Clear();
                Sort();
            }
            
            if (_queuedIntervals.Count > 0)
            {
                _intervals.AddRange(_queuedIntervals);
                _queuedIntervals.Clear();
            }
            
            _time += deltaTime;
            for (int i = _timedEvents.Count - 1; i >= 0; i--)
            {
                TimedEvent te = _timedEvents[i];
                if (te.time > _time) break;
                te.promise.Resolve();
                _timedEvents.RemoveAt(i);
            }

            for (int i = _conditionalEvents.Count - 1; i >= 0; i--)
            {
                ConditionalEvent ce = _conditionalEvents[i];
                if (ce.condition.Invoke())
                {
                    ce.promise.Resolve();
                    _conditionalEvents.RemoveAt(i);
                }
            }

            float prevTime = _time - deltaTime;
            for (int i = _intervals.Count - 1; i >= 0; i--)
            {
                IntervalEvent ie = _intervals[i];
                if (Mathf.FloorToInt(prevTime / ie.interval) < Mathf.FloorToInt(_time / ie.interval))
                {
                    if (!ie.func.Invoke())
                    {
                        ie.promise.Resolve();
                        _intervals.RemoveAt(i);
                    }
                }
            }
        }

        private void Sort()
        {
            _timedEvents.Sort((a, b) => b.time.CompareTo(a.time));
        }
    }
}
