using System.Collections.Generic;
using UnityEngine;

namespace SEE.Game.GestureRecognition
{
    /// <summary>
    /// 
    /// </summary>
    public abstract class AbstractGestureHandler
    {
        
        
        /// <summary>
        /// 
        /// </summary>
        public abstract void HandleGesture(DollarPGestureRecognizer.RecognizerResult result, Vector3[] rawPoints, GestureContext context);

        public struct GestureContext
        {
            public GameObject ParentObject;
            public GameObject Source;
            public GameObject Target;
            public Vector3 HeightOffset;
        }
    }
}