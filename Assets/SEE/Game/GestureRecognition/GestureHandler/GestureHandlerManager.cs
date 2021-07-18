using System;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Game.GestureRecognition
{
    
    
    /// <summary>
    /// 
    /// </summary>
    public static class GestureHandlerManager
    {
        
        /// <summary>
        /// Mapping from the gesture names to their respective <see cref="AbstractGestureHandler"/> implementation.
        /// </summary>
        private static readonly Dictionary<string, AbstractGestureHandler> GestureHandlers = new Dictionary<string, AbstractGestureHandler>
        {
            {"circle", new CircleGestureHandler()},
            {"square", new SquareGestureHandler()},
            {"line", new LineGestureHandler()}
        };

        /// <summary>
        /// 
        /// </summary>
        /// <param name="result"></param>
        /// <param name="rawPoints"></param>
        /// <param name="context"></param>
        /// <exception cref="Exception"></exception>
        public static void HandleGesture(DollarPGestureRecognizer.RecognizerResult result, Vector3[] rawPoints, AbstractGestureHandler.GestureContext context)
        {
            if (GestureHandlers.TryGetValue(result.Match.Name, out AbstractGestureHandler handler))
            {
                handler.HandleGesture(result, rawPoints, context);
            }
            else
            {
                throw new Exception(
                    $"Unhandled gesture {result.Match.Name} found. No Handler is registered for gesture {result.Match.Name}");
            }
            
        }
        
    }
}