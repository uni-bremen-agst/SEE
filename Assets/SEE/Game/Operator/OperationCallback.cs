using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine.Assertions;

namespace SEE.Game.Operator
{
    /// <summary>
    /// Defines various methods used to set callbacks.
    /// Setting callbacks will *not* implicitly override any previously existing callbacks.
    /// Documentation is copied and slightly modified from DOTween's documentation.
    /// </summary>
    /// <typeparam name="T">The type of the callback delegate</typeparam>
    public interface IOperationCallback<in T> where T : MulticastDelegate
    {
        /// <summary>
        /// Sets a callback that will be fired the moment the animator reaches completion, all loops included.
        /// </summary>
        void SetOnComplete(T callback);

        /// <summary>
        /// Sets a callback that will be fired the moment the animator is killed.
        /// </summary>
        void SetOnKill(T callback);

        /// <summary>
        /// Sets a callback that will be fired when the animator is set in a playing state, after any eventual delay.
        /// Also called each time the animator resumes playing from a paused state.
        /// </summary>
        void SetOnPlay(T callback);

        /// <summary>
        /// Sets a callback that will be fired when the animator state changes from playing to paused.
        /// </summary>
        void SetOnPause(T callback);

        /// <summary>
        /// Sets a callback that will be fired when the animator is rewinded,
        /// either by calling Rewind or by reaching the start position while playing backwards.
        /// </summary>
        /// <remarks>
        /// Rewinding an animator that is already rewinded will not fire this callback.
        /// </remarks>
        void SetOnRewind(T callback);

        /// <summary>
        /// Sets a callback that will be fired once when the animator starts (meaning when the animator is set in a
        /// playing state the first time, after any eventual delay).
        /// </summary>
        void SetOnStart(T callback);

        /// <summary>
        /// Sets a callback that will be fired every time the animator updates.
        /// </summary>
        /// <param name="callback"></param>
        void SetOnUpdate(T callback);

        // Missing from DOTween, we can add them once we need them: OnStepComplete, OnWaypointChange
    }

    /// <summary>
    /// An implementation of the operation callback intended for an animation which finishes instantly.
    /// Corresponding callbacks (e.g., OnComplete) will thus be called instantly.
    /// </summary>
    /// <typeparam name="T">the type of the callback delegate</typeparam>
    public class DummyOperationCallback<T> : IOperationCallback<T> where T : MulticastDelegate
    {
        // NOTE: We use reflection to invoke the callback because of the generic type parameter.
        //       This is very inefficient (by a factor of ~200, see https://stackoverflow.com/a/3465152),
        //       but AFAIK there is no good way around this.

        public void SetOnComplete(T callback)
        {
            // Happens instantly.
            callback.DynamicInvoke();
        }

        public void SetOnKill(T callback)
        {
            // Happens instantly.
            callback.DynamicInvoke();
        }

        public void SetOnPlay(T callback)
        {
            // Happens instantly.
            callback.DynamicInvoke();
        }

        public void SetOnPause(T callback)
        {
            // Nothing to be done.
        }

        public void SetOnRewind(T callback)
        {
            // Nothing to be done.
        }

        public void SetOnStart(T callback)
        {
            // Happens instantly.
            callback.DynamicInvoke();
        }

        public void SetOnUpdate(T callback)
        {
            // Nothing to be done.
        }
    }

    /// <summary>
    /// Will fire each respective callback when every one of the passed in callbacks has fired the corresponding event.
    /// Hence, this can be seen as an AND-linked set of callbacks.
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
        private readonly IDictionary<IOperationCallback<C>, int> CallbackCounter = new Dictionary<IOperationCallback<C>, int>();

        /// <summary>
        /// List of all callbacks combined by this class.
        /// </summary>
        private readonly IList<IOperationCallback<C>> Callbacks = new List<IOperationCallback<C>>();

        /// <summary>
        /// Function which converts from an <see cref="Action"/> to a delegate of type <typeparamref name="C"/>.
        /// </summary>
        private readonly Func<Action, C> CallbackConverter;

        /// <summary>
        /// Create a new <see cref="AndCombinedOperationCallback{C}"/> based on the given <paramref name="callbacks"/>.
        /// </summary>
        /// <param name="callbacks">The callbacks this operation callback combines. Each callback method of this class
        /// will only be triggered once every callback class of this parameter has fired the respective method.</param>
        /// <param name="callbackConverter">Converts from an <see cref="Action"/> to the type of the given
        /// <paramref name="callbacks"/>' callbacks.</param>
        public AndCombinedOperationCallback(IEnumerable<IOperationCallback<C>> callbacks, Func<Action, C> callbackConverter)
        {
            CallbackConverter = callbackConverter;
            foreach (IOperationCallback<C> operationCallback in callbacks)
            {
                Callbacks.Add(operationCallback);
                // We initialize the dictionary with 0, indicating that no callback has been triggered.
                CallbackCounter[operationCallback] = 0;
            }

            // We register ourselves as a listener on each callback method only once the callback is set.
            // This avoids unnecessary checks and handles cases in which the callback triggers instantly
            // upon registration (e.g., DummyOperationCallback).
        }
        
        /// <summary>
        /// Handles a callback method triggered by one of the composited callbacks.
        /// </summary>
        /// <param name="callback">The callback which triggered the callback method</param>
        /// <param name="index">Index of the callback method. See <see cref="CallbackCounter"/> for a reference.</param>
        /// <exception cref="IndexOutOfRangeException">When <paramref name="index"/> is not in [0;6]</exception>
        private void HandleSingleCallback(IOperationCallback<C> callback, int index)
        {
            if ((CallbackCounter[callback] & (1 << index)) == 1)
            {
                // Callback already triggered.
                return;
            }
            CallbackCounter[callback] |= 1 << index;
            if (CallbackCounter.All(x => (x.Value & (1 << index)) == 1))
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

        public void SetOnComplete(Action callback)
        {
            onComplete = callback;
            foreach (IOperationCallback<C> operationCallback in Callbacks)
            {
                operationCallback.SetOnComplete(CallbackConverter(() => HandleSingleCallback(operationCallback, 0)));
            }
        }

        public void SetOnKill(Action callback)
        {
            onKill = callback;
            foreach (IOperationCallback<C> operationCallback in Callbacks)
            {
                operationCallback.SetOnKill(CallbackConverter(() => HandleSingleCallback(operationCallback, 1)));
            }
        }

        public void SetOnPlay(Action callback)
        {
            onPlay = callback;
            foreach (IOperationCallback<C> operationCallback in Callbacks)
            {
                operationCallback.SetOnPlay(CallbackConverter(() => HandleSingleCallback(operationCallback, 2)));
            }
        }

        public void SetOnPause(Action callback)
        {
            onPause = callback;
            foreach (IOperationCallback<C> operationCallback in Callbacks)
            {
                operationCallback.SetOnPause(CallbackConverter(() => HandleSingleCallback(operationCallback, 3)));
            }
        }

        public void SetOnRewind(Action callback)
        {
            onRewind = callback;
            foreach (IOperationCallback<C> operationCallback in Callbacks)
            {
                operationCallback.SetOnRewind(CallbackConverter(() => HandleSingleCallback(operationCallback, 4)));
            }
        }

        public void SetOnStart(Action callback)
        {
            onStart = callback;
            foreach (IOperationCallback<C> operationCallback in Callbacks)
            {
                operationCallback.SetOnStart(CallbackConverter(() => HandleSingleCallback(operationCallback, 5)));
            }
        }

        public void SetOnUpdate(Action callback)
        {
            onUpdate = callback;
            foreach (IOperationCallback<C> operationCallback in Callbacks)
            {
                operationCallback.SetOnUpdate(CallbackConverter(() => HandleSingleCallback(operationCallback, 6)));
            }
        }
    }

    /// <summary>
    /// An implementation of the operation callback intended for a single tween.
    /// </summary>
    public class TweenOperationCallback : IOperationCallback<TweenCallback>
    {
        /// <summary>
        /// The callback this tween operates on.
        /// </summary>
        private readonly Tween TargetTween;

        /// <summary>
        /// Creates a new <see cref="TweenOperationCallback"/> operating on the given <paramref name="targetTween"/>.
        /// </summary>
        /// <param name="targetTween">The tween this class operates on.</param>
        public TweenOperationCallback(Tween targetTween)
        {
            Assert.IsNotNull(targetTween);
            TargetTween = targetTween;
        }

        public void SetOnComplete(TweenCallback callback)
        {
            TargetTween.OnComplete((TweenCallback)Delegate.Combine(TargetTween.onComplete, callback));
        }

        public void SetOnKill(TweenCallback callback)
        {
            TargetTween.OnKill((TweenCallback)Delegate.Combine(TargetTween.onKill, callback));
        }

        public void SetOnPlay(TweenCallback callback)
        {
            TargetTween.OnPlay((TweenCallback)Delegate.Combine(TargetTween.onPlay, callback));
        }

        public void SetOnPause(TweenCallback callback)
        {
            TargetTween.OnPause((TweenCallback)Delegate.Combine(TargetTween.onPause, callback));
        }

        public void SetOnRewind(TweenCallback callback)
        {
            TargetTween.OnRewind((TweenCallback)Delegate.Combine(TargetTween.onRewind, callback));
        }

        public void SetOnUpdate(TweenCallback callback)
        {
            TargetTween.OnUpdate((TweenCallback)Delegate.Combine(TargetTween.onUpdate, callback));
        }

        /// <summary>
        /// Sets a callback that will be fired once when the animator starts (meaning when the animator is set in a
        /// playing state the first time, after any eventual delay).
        /// **All existing callbacks for `OnStart` will be removed.**
        /// </summary>
        public void SetOnStart(TweenCallback callback)
        {
            // We can't combine delegates here because `onStart` is an internal property in DOTween.
            TargetTween.OnStart(callback);
        }
    }
}