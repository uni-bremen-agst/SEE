using System;
using DG.Tweening;
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
        private TweenOperation<float> PositionY;

        public float TargetPositionY => PositionY.TargetValue;

        /// <summary>
        /// Moves the notification to the <paramref name="newY"/> position, taking <paramref name="duration"/> seconds.
        /// </summary>
        /// <param name="newY">the desired new target Y-coordinate</param>
        /// <param name="duration">Time in seconds the animation should take. If set to 0, will execute directly,
        /// that is, the value is set before control is returned to the caller.</param>
        /// <returns>An operation callback for the requested animation</returns>
        public IOperationCallback<Action> MoveToY(float newY, float duration)
        {
            return PositionY.AnimateTo(newY, duration);
        }

        private void OnEnable()
        {
            RectTransform rectTransform = (RectTransform)transform;
            Tween[] PositionYAction(float p, float d) => new Tween[]
            {
                rectTransform.DOAnchorPosY(p, d).Play()
            };

            PositionY = new TweenOperation<float>(PositionYAction, rectTransform.anchoredPosition.y);
        }

        private void OnDisable()
        {
            PositionY?.KillAnimator();
            PositionY = null;
        }
    }
}