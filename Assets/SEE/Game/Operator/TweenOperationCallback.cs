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