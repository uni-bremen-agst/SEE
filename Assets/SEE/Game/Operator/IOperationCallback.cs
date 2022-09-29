using System;

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
}