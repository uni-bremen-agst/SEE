using System;
using DG.Tweening;
using SEE.GO;
using UnityEngine;

namespace SEE.Utils
{
    /// <summary>
    /// A class for animating objects (tweening).
    /// </summary>
    internal class Tweens : MonoBehaviour
    {
        /// <summary>
        /// Sets the capacity of DOTween.
        /// </summary>
        static Tweens()
        {
            DOTween.SetTweensCapacity(2000, 100);
        }

        /// <summary>
        /// Moves a GameObject to a specific position over a given duration
        /// and makes a <paramref name="callback"/> if this is not null.
        /// </summary>
        /// <param name="gameObject">
        /// A <see cref="GameObject"/> to be the target of the animation.
        /// </param>
        /// <param name="position">
        /// A <see cref="Vector3"/> for the destination Vector3.
        /// </param>
        /// <param name="MaxAnimationTime">
        /// A <see cref="System.Single"/> for the time in seconds the animation will take to complete.
        /// </param>
        /// <param name="callback">
        /// A <see cref="Action"/> The invoked callback after the animation.
        /// </param>
        /// <remarks>For game objects representing graph nodes, use <see cref="NodeOperator.Move"/> instead.</remarks>
        public static void Move(GameObject gameObject, Vector3 position, float MaxAnimationTime, Action<object> callback = null)
        {
            if (gameObject.HasNodeRef())
            {
                Debug.LogWarning($"[Tweens.Move({gameObject.name})] When moving nodes, use the NodeOperator instead.\n");
            }
            if (callback != null)
            {
                gameObject.transform.DOMove(position, MaxAnimationTime).OnComplete(()=>{callback?.Invoke(gameObject);});
            }
            else
            {
                gameObject.transform.DOMove(position, MaxAnimationTime);
            }
        }

        /// <summary>
        /// Changes the scale of <paramref name="gameObject"/> over time and calls <paramref name="callback"/>
        /// at the end of the animation if this is not null.
        /// </summary>
        /// <param name="gameObject">
        /// A <see cref="GameObject"/> to be the target of the animation.
        /// </param>
        /// <param name="localScale">
        /// The final local scale to be reached.
        /// </param>
        /// <param name="duration">
        /// The time in seconds the animation should take to complete.
        /// </param>
        /// <param name="callback">
        /// A <see cref="Action"/> The invoked callback after the animation.
        /// </param>
        /// <remarks>For game objects representing graph nodes, use <see cref="NodeOperator.Move"/> instead.</remarks>
        public static void Scale(GameObject gameObject, Vector3 localScale, float duration, Action<object> callback = null)
        {
            if (gameObject.HasNodeRef())
            {
                Debug.LogWarning($"[Tweens.Scale({gameObject.name})] When scaling nodes, use the NodeOperator instead.\n");
            }
            if (callback != null)
            {
                gameObject.transform.DOScale(localScale, duration).OnComplete(()=>{callback(gameObject);});
            }
            else
            {
                gameObject.transform.DOScale(localScale, duration);
            }
        }
    }
}
