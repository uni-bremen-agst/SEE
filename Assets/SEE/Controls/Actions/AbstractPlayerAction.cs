using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// An abstract superclass of all PlayerActions such as NewNodeAction, ScaleNodeAction, EditNodeAction and AddEdgeAction.
    /// The most important attribute for all of them is the hoveredObject, which will be instantiated and updated by LocalAnyHoverIn and LocalAnyHoverOut.
    /// </summary>
    public abstract class AbstractPlayerAction : MonoBehaviour
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
        /// true, if the active script is already initialised, else false
        /// </summary>
        protected bool instantiated = false;

        /// <summary>
        /// Finds the GameObject which contains the CanvasOperations and components
        /// and saves it in the canvasObject-variable.
        /// </summary>
        public void InitializeCanvasObject()
        {
            canvasObject = GameObject.Find(nameOfCanvasObject);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="interactableObject"></param>
        protected void LocalAnyHoverIn(InteractableObject interactableObject)
        {
            try
            {
                Assert.IsNull(hoveredObject);
                hoveredObject = interactableObject.gameObject;
            }
            catch
            {
              // FIXME: There are AssertionExceptions 
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="interactableObject"></param>
        protected void LocalAnyHoverOut(InteractableObject interactableObject)
        {
            try
            {
                Assert.IsTrue(hoveredObject == interactableObject.gameObject);
                hoveredObject = null;
            }
            catch
            {
            //FIXME: There are AssertionExceptions
            }
        }


    }
}
