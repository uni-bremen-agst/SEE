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
        /// Changes a GameObject's scale over time and makes a <paramref name="callback"/> 
        /// if this is not null.
        /// </summary>
        /// <param name="gameObject">
        /// A <see cref="GameObject"/> to be the target of the animation.
        /// </param>
        /// <param name="localScale">
        /// A <see cref="Vector3"/> for the final scale.
        /// </param>
        /// <param name="MaxAnimationTime">
        /// A <see cref="System.Single"/> for the time in seconds the animation will take to complete.
        /// </param>
        /// <param name="callback">
        /// A <see cref="Action"/> The invoked callback after the animation.
        /// </param>
        public static void Scale(GameObject gameObject, Vector3 localScale, float MaxAnimationTime, Action<object> callback = null)
        {
            if (callback != null)
            {
                gameObject.transform.DOScale(localScale, MaxAnimationTime).OnComplete(()=>{callback?.Invoke(gameObject);});
            }
            else
            {
                gameObject.transform.DOScale(localScale, MaxAnimationTime);
            }            
        }
    }
}
