using System;
using DG.Tweening;
using UnityEngine.Assertions;

namespace SEE.Game.Operator
{
    /// <summary>
    /// An implementation of the operation callback intended for a single tween.
    /// </summary>
    public class TweenOperationCallback : IOperationCallback<TweenCallback>
    {
        /// <summary>
        /// The callback this tween operates on.
        /// </summary>
        private readonly Tween targetTween;

        /// <summary>
        /// Creates a new <see cref="TweenOperationCallback"/> operating on the given <paramref name="targetTween"/>.
        /// </summary>
        /// <param name="targetTween">The tween this class operates on.</param>
        public TweenOperationCallback(Tween targetTween)
        {
            Assert.IsNotNull(targetTween);
            this.targetTween = targetTween;
        }

        public void SetOnComplete(TweenCallback callback)
        {
            targetTween.OnComplete((TweenCallback)Delegate.Combine(targetTween.onComplete, callback));
        }

        public void SetOnKill(TweenCallback callback)
        {
            targetTween.OnKill((TweenCallback)Delegate.Combine(targetTween.onKill, callback));
        }

        public void SetOnPlay(TweenCallback callback)
        {
            targetTween.OnPlay((TweenCallback)Delegate.Combine(targetTween.onPlay, callback));
        }

        public void SetOnPause(TweenCallback callback)
        {
            targetTween.OnPause((TweenCallback)Delegate.Combine(targetTween.onPause, callback));
        }

        public void SetOnRewind(TweenCallback callback)
        {
            targetTween.OnRewind((TweenCallback)Delegate.Combine(targetTween.onRewind, callback));
        }

        public void SetOnUpdate(TweenCallback callback)
        {
            targetTween.OnUpdate((TweenCallback)Delegate.Combine(targetTween.onUpdate, callback));
        }

        /// <summary>
        /// Sets a callback that will be fired once when the animator starts (meaning when the animator is set in a
        /// playing state the first time, after any eventual delay).
        /// **All existing callbacks for `OnStart` will be removed.**
        /// </summary>
        public void SetOnStart(TweenCallback callback)
        {
            // We can't combine delegates here because `onStart` is an internal property in DOTween.
            targetTween.OnStart(callback);
        }
    }
}