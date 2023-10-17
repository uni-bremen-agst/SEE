using System;
using System.Collections.Generic;
using System.Linq;

namespace SEE.Game.Operator
{
    /// <summary>
    /// Will fire a callback when every callback that has been passed in has fired the
    /// corresponding event. Hence, this can be seen as an AND-linked set of callbacks.
    /// </summary>
    /// <typeparam name="C">the type of the callback of each operation</typeparam>
    public class AndCombinedOperationCallback<C> : IOperationCallback<Action> where C : MulticastDelegate
    {
        /// <summary>
        /// User-defined callback for OnComplete.
        /// </summary>
        private Action onComplete;

        /// <summary>
        /// User-defined callback for OnKill.
        /// </summary>
        private Action onKill;

        /// <summary>
        /// User-defined callback for OnPlay.
        /// </summary>
        private Action onPlay;

        /// <summary>
        /// User-defined callback for OnPause.
        /// </summary>
        private Action onPause;

        /// <summary>
        /// User-defined callback for OnRewind.
        /// </summary>
        private Action onRewind;

        /// <summary>
        /// User-defined callback for OnStart.
        /// </summary>
        private Action onStart;

        /// <summary>
        /// User-defined callback for OnUpdate.
        /// </summary>
        private Action onUpdate;

        /// <summary>
        /// Mapping from each combined callback to a bitmask representing whether the respective method was called.
        /// Bitmask order: OnComplete, OnKill, OnPlay, OnPause, OnRewind, OnStart, OnUpdate
        /// </summary>
        private readonly IDictionary<IOperationCallback<C>, int> callbackCounter = new Dictionary<IOperationCallback<C>, int>();

        /// <summary>
        /// List of all callbacks combined by this class.
        /// </summary>
        private readonly IList<IOperationCallback<C>> callbacks = new List<IOperationCallback<C>>();

        /// <summary>
        /// Function which converts from an <see cref="Action"/> to a delegate of type <typeparamref name="C"/>.
        /// </summary>
        private readonly Func<Action, C> callbackConverter;

        /// <summary>
        /// Create a new <see cref="AndCombinedOperationCallback{C}"/> based on the given <paramref name="callbacks"/>.
        /// </summary>
        /// <param name="callbacks">The callbacks this operation callback combines. Each callback method of this class
        /// will only be triggered once every callback class of this parameter has fired the respective method.</param>
        /// <param name="callbackConverter">Converts from an <see cref="Action"/> to the type of the given
        /// <paramref name="callbacks"/>' callbacks.</param>
        public AndCombinedOperationCallback(IEnumerable<IOperationCallback<C>> callbacks, Func<Action, C> callbackConverter = null)
        {
            if (typeof(C) == typeof(Action))
            {
                this.callbackConverter = a => a as C;
            }
            else if (callbackConverter == null)
            {
                throw new ArgumentException("callbackConverter must not be null when generic parameter C != Action!");
            }
            else
            {
                this.callbackConverter = callbackConverter;
            }
            foreach (IOperationCallback<C> operationCallback in callbacks)
            {
                this.callbacks.Add(operationCallback);
                // We initialize the dictionary with 0, indicating that no callback has been triggered.
                callbackCounter[operationCallback] = 0;
            }

            // We register ourselves as a listener on each callback method only once the callback is set.
            // This avoids unnecessary checks and handles cases in which the callback triggers instantly
            // upon registration (e.g., DummyOperationCallback).
        }

        /// <summary>
        /// Handles a callback method triggered by one of the composited callbacks.
        /// </summary>
        /// <param name="callback">The callback which triggered the callback method</param>
        /// <param name="index">Index of the callback method. See <see cref="callbackCounter"/> for a reference.</param>
        /// <exception cref="IndexOutOfRangeException">When <paramref name="index"/> is not in [0;6]</exception>
        private void HandleSingleCallback(IOperationCallback<C> callback, int index)
        {
            if ((callbackCounter[callback] & (1 << index)) == 1)
            {
                // Callback already triggered.
                return;
            }
            callbackCounter[callback] |= 1 << index;
            if (callbackCounter.All(x => (x.Value & (1 << index)) == 1))
            {
                // All callbacks have been triggered, so we trigger the actual user-defined callback.
                switch (index)
                {
                    case 0: onComplete?.Invoke();
                        break;
                    case 1: onKill?.Invoke();
                        break;
                    case 2: onPlay?.Invoke();
                        break;
                    case 3: onPause?.Invoke();
                        break;
                    case 4: onRewind?.Invoke();
                        break;
                    case 5: onStart?.Invoke();
                        break;
                    case 6: onUpdate?.Invoke();
                        break;
                    default: throw new IndexOutOfRangeException();
                }
            }
        }

        public IOperationCallback<Action> OnComplete(Action callback)
        {
            onComplete = callback;
            foreach (IOperationCallback<C> operationCallback in callbacks)
            {
                operationCallback.OnComplete(callbackConverter(() => HandleSingleCallback(operationCallback, 0)));
            }
            return this;
        }

        public IOperationCallback<Action> OnKill(Action callback)
        {
            onKill = callback;
            foreach (IOperationCallback<C> operationCallback in callbacks)
            {
                operationCallback.OnKill(callbackConverter(() => HandleSingleCallback(operationCallback, 1)));
            }
            return this;
        }

        public IOperationCallback<Action> OnPlay(Action callback)
        {
            onPlay = callback;
            foreach (IOperationCallback<C> operationCallback in callbacks)
            {
                operationCallback.OnPlay(callbackConverter(() => HandleSingleCallback(operationCallback, 2)));
            }
            return this;
        }

        public IOperationCallback<Action> OnPause(Action callback)
        {
            onPause = callback;
            foreach (IOperationCallback<C> operationCallback in callbacks)
            {
                operationCallback.OnPause(callbackConverter(() => HandleSingleCallback(operationCallback, 3)));
            }
            return this;
        }

        public IOperationCallback<Action> OnRewind(Action callback)
        {
            onRewind = callback;
            foreach (IOperationCallback<C> operationCallback in callbacks)
            {
                operationCallback.OnRewind(callbackConverter(() => HandleSingleCallback(operationCallback, 4)));
            }
            return this;
        }

        public IOperationCallback<Action> OnStart(Action callback)
        {
            onStart = callback;
            foreach (IOperationCallback<C> operationCallback in callbacks)
            {
                operationCallback.OnStart(callbackConverter(() => HandleSingleCallback(operationCallback, 5)));
            }
            return this;
        }

        public IOperationCallback<Action> OnUpdate(Action callback)
        {
            onUpdate = callback;
            foreach (IOperationCallback<C> operationCallback in callbacks)
            {
                operationCallback.OnUpdate(callbackConverter(() => HandleSingleCallback(operationCallback, 6)));
            }
            return this;
        }
    }
}
