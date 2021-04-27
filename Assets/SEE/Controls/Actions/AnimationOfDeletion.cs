using SEE.GO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace SEE.Controls
{
    public static class AnimationOfDeletion
    {


        /// <summary>
        /// The animation time of the animation of moving a node to the top of the garbage can.
        /// </summary>
        public const float TimeForAnimation = 1f;

        /// <summary>
        /// The waiting time of the animation for moving a node into a garbage can from over the garbage can.
        /// </summary>
        private const float TimeToWait = 1f;

        public static IEnumerator DelayEdges(GameObject edge)
        {
            yield return new WaitForSeconds(TimeForAnimation + TimeToWait);
            edge.SetVisibility(true, true);
        }
    }

}