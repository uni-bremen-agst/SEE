using System;
using System.Collections.Generic;
using Antlr4.Runtime;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Game.GestureRecognition
{

    /// <summary>
    /// Struct that holds detailed context information for the gesture.
    /// For an edge the Source and Target fields are expected to be filled.
    /// For a node The the ParentObject and HeightOffset fields are expected to be filled.
    /// </summary>
    public struct GestureContext
    {
        public GameObject ParentObject;
        public GameObject Source;
        public GameObject Target;
        public Vector3 HeightOffset;
    }


    /// <summary>
    /// Class that handles the recognized gestures. e.g Adding a new Cluster node when a square gesture was detected.
    /// </summary>
    public static class GestureHandler
    {

        #region ElementTypeDefinitions

        private const string ClusterType = "Cluster";
        private const string ComponentType = "Component";

        #endregion

        #region GestureTypeDefinitions

        
        private const string CircleGesture = "circle";
        private const string SquareGesture = "square";
        private const string LineGesture = "edge";

        #endregion
        

        
        /// <summary>
        /// Handles the further gesture processing based on the recognized gesture.
        /// If no matching gesture definition was found, an exception is thrown.
        /// </summary>
        /// <param name="result">The result of the <see cref="DollarPGestureRecognizer"/></param>
        /// <param name="rawPoints">The world space points of the gesture to calculate the 3D bouding box from.</param>
        /// <param name="context">The context within the gesture was created. e.g to identify source and target nodes for new edges.</param>
        /// <exception cref="Exception">Thrown when no handler method for the passed gesture was found.</exception>
        public static void HandleGesture(DollarPGestureRecognizer.RecognizerResult result, Vector3[] rawPoints, GestureContext context)
        {
            switch (result.Match.Name)
            {
                case CircleGesture:
                    CreateNode(rawPoints,context, ComponentType);
                    break;
                case SquareGesture:
                    CreateNode(rawPoints,context, ClusterType);
                    break;
                case LineGesture:
                    CreateEdge(context);
                    break;
                default:
                    throw new Exception($"Unhandled gesture {result.Match.Name} was found.\n");
                    
                    
                    
                    
            }
            
        }
        
        /// <summary>
        /// Creates a new game edge, as well as an edge for the underlying <see cref="Graph"/>
        /// </summary>
        /// <param name="context">The gesture context, holding the source and target nodes</param>
        private static void CreateEdge(GestureContext context)
        {
            Assert.IsNotNull(context.Source);
            Assert.IsNotNull(context.Target);
            GameEdgeAdder.AddArchitectureEdge(context.Source, context.Target);
        }

        /// <summary>
        /// Creates a new game node, as well as a node for the underlying <see cref="Graph"/>.
        /// </summary>
        /// <param name="rawPoints">The world space gesture points. Used to calculate the world bounding box.</param>
        /// <param name="context">The gesture context holding the parent object and the yLevel</param>
        /// <param name="nodeType">The type the created graph node should have.</param>
        private static void CreateNode(Vector3[] rawPoints, GestureContext context, string nodeType)
        {
            Assert.IsNotNull(context.ParentObject);
            
            //Calculate the world space bounding box of the gesture shape.
            GestureBoundingBox box = GestureBoundingBox.Get(rawPoints);
            // Create a new architecture node
            GameNodeAdder.AddArchitectureNode(context.ParentObject,
                box.center - context.HeightOffset, new Vector3(box.width, 0.01f, box.height),
                nodeType: nodeType);
        }
        
        
        
    }
}