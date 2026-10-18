using UnityEngine;

namespace PlazmaGames.Core
{
    public class Promise<T>
    {
        private T _value;
        private bool _isResolved = false;
        private System.Action<T> _then = null;

        public bool IsResolved() => _isResolved;

        public void Resolve(T value = default)
        {
            _value = value;
            _isResolved = true;
            
            _then?.Invoke(_value);
        }

        public Promise Then(System.Action<T> func)
        {
            Promise p = new Promise();
            _then = (T) =>
            {
                func(T);
                p.Resolve();
            };
            return p;
        }
        
        public Promise Then(System.Func<T, bool> func)
        {
            Promise p = new Promise();
            _then = (T) =>
            {
                if (func(T) == Promise.Continue) p.Resolve();
            };
            return p;
        }
        
        public Promise<U> Then<U>(System.Func<T, Promise<U>> func)
        {
            var chained = new Promise<U>();
            _then = (tValue) =>
            {
                Promise<U> p = func(tValue);
                p?.Then((uValue) =>
                {
                    chained.Resolve(uValue);
                });
            };
            
            return chained;
        }

        public static Promise<U> All<U>(params Promise<U>[] promises)
        {
            Promise<U> combined = new Promise<U>();

            if (promises == null || promises.Length == 0)
            {
                combined.Resolve(default);
                return combined;
            }

            int remaining = promises.Length;

            foreach (Promise<U> p in promises)
            {
                p.Then(_ =>
                {
                    remaining--;
                    if (remaining <= 0)
                    {
                        combined.Resolve(default);
                    }
                });
            }

            return combined;
        }

        public static Promise All(params Promise[] promises)
        {
            Promise combined = new Promise();
            if (promises == null || promises.Length == 0)
            {
                combined.Resolve();
                return combined;
            }

            int remaining = promises.Length;

            foreach (Promise p in promises)
            {
                p.Then(_ =>
                {
                    remaining--;
                    if (remaining <= 0)
                    {
                        combined.Resolve();
                    }
                });
            }

            return combined;
        }

        public static Promise CreateExisting(ref Promise value)
        {
            if (value != null)
            {
                Debug.LogWarning("Another Promise is Already In Commission.");
                return null;
            }

            value = new Promise();
            return value;
        }

        public static void ResolveExisting(ref Promise value)
        {
            Promise tmp = value;
            value = null;
            tmp?.Resolve();
        }

        public static Promise<T> CreateExisting(ref Promise<T> value)
        {
            if (value != null)
            {
                Debug.LogWarning("Another Promise is Already In Commission.");
                return null;
            }

            value = new Promise<T>();
            return value;
        }

        public static void ResolveExisting(ref Promise<T> value, T param)
        {
            Promise<T> tmp = value;
            value = null;
            tmp?.Resolve(param);
        }
    }

    public class Promise : Promise<int>
    {
        public static readonly bool Continue = true;
        public static readonly bool Break = false;
    }
}
