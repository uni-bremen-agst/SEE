using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Controls.Actions
{
    public class NodeAction : MonoBehaviour
    {
        /// <summary>
        /// The gameObject which contains the CanvasGenerator-Scripts and the actual CanvasObject-Script
        /// </summary>
        protected GameObject canvasObject;

        /// <summary>
        /// The Object that the Cursor hovers over
        /// </summary>
        protected GameObject hoveredObject = null;

        /// <summary>
        /// The current name of the gameObject which contains the Canvas-operations and components.
        /// </summary>
        private readonly string nameOfCanvasObject = "CanvasObject";

        /// <summary>
        /// Finds the GameObject, which contains the CanvasOperations and components
        /// and saves it in the canvasObject-variable.
        /// </summary>
        public void InitialiseCanvasObject()
        {
            canvasObject = GameObject.Find(nameOfCanvasObject);
        }

        protected void LocalAnyHoverIn(InteractableObject interactableObject)
        {
            try
            {
                Assert.IsNull(hoveredObject);
                hoveredObject = interactableObject.gameObject;
            }
            catch
            {
                //There are AssertionExceptions 
            }
        }

        protected void LocalAnyHoverOut(InteractableObject interactableObject)
        {
            try
            {
                Assert.IsTrue(hoveredObject == interactableObject.gameObject);
                hoveredObject = null;
            }
            catch
            {
                //There are AssertionExceptions
            }
        }


    }
}
