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
        public GameObject hoveredObject = null;

        /// <summary>
        /// The current name of the gameObject which contains the Canvas-operations and components.
        /// </summary>
        private readonly string nameOfCanvasObject = "CanvasObject";

        /// <summary>
        /// Removes this script or rather the child-script.
        /// </summary>
        public virtual void RemoveScript()
        {
            Destroy(this);
        }

        /// <summary>
        /// Finds the GameObject, which contains the CanvasOperations and components
        /// and saves it in the canvasObject-variable.
        /// </summary>
        public void InitialiseCanvasObject()
        {
            canvasObject = GameObject.Find(nameOfCanvasObject);
        }

        public void ChangeState(ActionState.Type ThisActionState)
        {
            // An anonymous delegate is registered for the event <see cref="ActionState.OnStateChanged"/>.
            // This delegate will be called from <see cref="ActionState"/> upon every
            // state changed where the passed parameter is the newly entered state.
            ActionState.OnStateChanged += (ActionState.Type newState) =>
                {
                // Is this our action state where we need to do something?
                if (newState == ThisActionState)
                    {
                    // The monobehaviour is enabled and Update() will be called by Unity.
                    enabled = true;
                        InteractableObject.LocalAnyHoverIn += LocalAnyHoverIn;
                        InteractableObject.LocalAnyHoverOut += LocalAnyHoverOut;
                    }
                    else
                    {
                    // The monobehaviour is diabled and Update() no longer be called by Unity.
                    enabled = false;
                        InteractableObject.LocalAnyHoverIn -= LocalAnyHoverIn;
                        InteractableObject.LocalAnyHoverOut -= LocalAnyHoverOut;
                    }
                };
            enabled = ActionState.Is(ThisActionState);

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
