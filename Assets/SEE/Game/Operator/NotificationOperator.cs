using System;
using DG.Tweening;
using SEE.Game.City;
using SEE.Utils;
using UnityEngine;

namespace SEE.Game.Operator
{
    /// <summary>
    /// An operator offering methods for animating notification movements.
    /// </summary>
    public class NotificationOperator : AbstractOperator
    {
        /// <summary>
        /// Operation handling position on the Y axis.
        /// </summary>
        private TweenOperation<float> positionY;

        /// <summary>
        /// The Y position this notification is supposed to be at.
        /// </summary>
        public float TargetPositionY => positionY.TargetValue;

        /// <summary>
        /// The base animation duration for this operator.
        /// May be set from outside.
        /// </summary>
        private float TheBaseAnimationDuration
        {
            get;
            set;
        } = 1f;

        protected override float BaseAnimationDuration => TheBaseAnimationDuration;

        /// <summary>
        /// Moves the notification to the <paramref name="newY"/> position, taking <paramref name="duration"/> seconds.
        /// </summary>
        /// <param name="newY">The desired new target Y-coordinate.</param>
        /// <param name="factor">Factor to apply to the <see cref="BaseAnimationDuration"/>
        /// that controls the animation duration.
        /// If set to 0, will execute directly, that is, the value is set before control is returned to the caller.
        /// </param>
        /// <returns>An operation callback for the requested animation.</returns>
        public IOperationCallback<Action> MoveToY(float newY, float factor = 1)
        {
            return positionY.AnimateTo(newY, ToDuration(factor));
        }

        private void OnEnable()
        {
            RectTransform rectTransform = (RectTransform)transform;
            positionY = new TweenOperation<float>(PositionYAction, rectTransform.anchoredPosition.y);
            return;

            Tween[] PositionYAction(float p, float d) => new Tween[]
            {
                rectTransform.DOAnchorPosY(p, d).Play()
            };
        }

        private void OnDisable()
        {
            positionY?.KillAnimator();
            positionY = null;
        }
    }
}
