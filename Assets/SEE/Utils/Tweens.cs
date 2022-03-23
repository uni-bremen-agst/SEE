using System;
using DG.Tweening;
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
        public static void Move(GameObject gameObject, Vector3 position, float MaxAnimationTime, Action<object> callback = null)
        {
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
        /// Randomly shakes a GameObject's rotation by a diminishing amount over time with
        /// and makes a <paramref name="callback"/> if this is not null.
        /// </summary>
        /// <param name="gameObject">
        /// A <see cref="GameObject"/> to be the target of the animation.
        /// </param>
        /// <param name="vector">
        /// A <see cref="Vector3"/> for the magnitude of shake.
        /// </param>
        /// <param name="MaxAnimationTime">
        /// A <see cref="System.Single"/> for the time in seconds the animation will take to complete.
        /// </param>
        /// <param name="callback">
        /// A <see cref="Action"/> The invoked callback after the animation.
        /// </param>
        public static void ShakeRotate(GameObject gameObject, float MaxAnimationTime, Vector3 vector, Action<object> callback = null)
        {
            if (callback != null)
            {
                gameObject.transform.DOShakeRotation(MaxAnimationTime, vector).OnComplete(()=>{callback?.Invoke(gameObject);});
            }
            else
            {
                gameObject.transform.DOShakeRotation(MaxAnimationTime, vector);
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
        /// The final locale scale to be reached.
        /// </param>
        /// <param name="duration">
        /// The time in seconds the animation should take to complete.
        /// </param>
        /// <param name="callback">
        /// A <see cref="Action"/> The invoked callback after the animation.
        /// </param>
        public static void Scale(GameObject gameObject, Vector3 localScale, float duration, Action<object> callback = null)
        {
            if (callback != null)
            {
                gameObject.transform.DOScale(localScale, duration).OnComplete(()=>{callback(gameObject);});
            }
            else
            {
                gameObject.transform.DOScale(localScale, duration);
            }
        }

        /// <summary>
        /// Moves, scales, and shakes <paramref name="gameObject"/> as a sequence of animations.
        /// </summary>
        /// <param name="gameObject">the game object to be animated</param>
        /// <param name="position">the final destination of <paramref name="gameObject"/></param>
        /// <param name="localScale">the final scale of <paramref name="gameObject"/></param>
        /// <param name="strength">the shake strength; each component corresponds to one axis (x, y, z)</param>
        /// <param name="duration">the duration of the whole animation in seconds</param>
        /// <param name="callback">the method to be called when the animation has finished</param>
        /// <param name="moveCallback">the method to be called when the move animation is about to start</param>
        public static void MoveScaleShakeRotate(GameObject gameObject, Vector3 position, Vector3 localScale, Vector3 strength, float duration, Action<object> callback, Action<float> moveCallback = null)
        {
            Sequence sequence = DOTween.Sequence();
            sequence.Append(gameObject.transform.DOScale(localScale, duration / 3));
            sequence.Append(gameObject.transform.DOMove(position, duration / 3).OnStart(() => { moveCallback?.Invoke(duration / 3); }));
            sequence.Append(gameObject.transform.DOShakeRotation(duration: duration / 3,
                            strength: strength, vibrato: 2, randomness: 0, fadeOut: true).OnComplete(() => { callback?.Invoke(gameObject); }));
        }
    }
}
