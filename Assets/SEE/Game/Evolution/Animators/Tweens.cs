using System;
using System.Reflection;
using DG.Tweening;
using UnityEngine;


namespace SEE.Game.Evolution
{
    internal class Tweens : MonoBehaviour
    {

        /// <summary>
        /// Moves a GameObject to a specific position over a given duration.
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

        public static void Move(GameObject gameObject, Vector3 position, float MaxAnimationTime)
        {
            gameObject.transform.DOMove(position, MaxAnimationTime);
        }

        /// <summary>
        /// Moves a GameObject to a specific position over a given duration.
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

        public static void Move(GameObject gameObject, Vector3 position, float MaxAnimationTime, GameObject callBackTarget, String callBackMethod, System.Object callBackParams)
        {
            gameObject.transform.DOMove(position, MaxAnimationTime);

        }




        /// <summary>
        /// Randomly shakes a GameObject's rotation by a diminishing amount over time with.
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
        public static void ShakeRotate(GameObject gameObject, float MaxAnimationTime, Vector3 vector)
        {
            gameObject.transform.DOShakeRotation(MaxAnimationTime, vector);
        }

        /// <summary>
        /// Changes a GameObject's scale over time.
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
        public static void Scale(GameObject gameObject, Vector3 localScale, float MaxAnimationTime)
        {
            gameObject.transform.DOScale(localScale, MaxAnimationTime);
        }
    }

}