using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Game.GestureRecognition
{
    /// <summary>
    /// Implementation of <see cref="AbstractGestureHandler"/> to handle nodes created by the circle gesture.
    /// The created node game object matches the size and position of the passed gesture.
    /// The newly generated graph node has the type Component.
    /// </summary>
    public class CircleGestureHandler: AbstractGestureHandler
    {


        private const string NodeType = "Component";

        public override void HandleGesture(DollarPGestureRecognizer.RecognizerResult result, Vector3[] rawPoints, GestureContext context)
        {
            Assert.IsNotNull(context.ParentObject);
            
            //Calculate the world space bounding box of the gesture shape.
            GestureBoundingBox box = GestureBoundingBox.Get(rawPoints);
            // Create a new architecture node
            GameObject newSquareNode = GameNodeAdder.AddArchitectureNode(context.ParentObject,
                box.center - context.HeightOffset, new Vector3(box.width, 0.01f, box.height),
                nodeType: NodeType);
        }

        
    }
}