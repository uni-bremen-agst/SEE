using SEE.GO;
using SEE.Utils;
using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// An abstract superclass of all PlayerActions such as NewNodeAction, ScaleNodeAction, EditNodeAction and AddEdgeAction.
    /// The most important attribute for all of them is the hoveredObject, which will be instantiated and updated by LocalAnyHoverIn and LocalAnyHoverOut.
    /// </summary>
    public abstract class AbstractPlayerAction
    {
        /// <summary>
        /// The gameObject that contains the CanvasGenerator and the actual CanvasObject.
        /// </summary>
        protected GameObject canvasObject;

        /// <summary>
        /// The object that the cursor hovers over.
        /// </summary>
        protected GameObject hoveredObject = null;

        /// <summary>
        /// The current name of the gameObject that contains the canvas operations and components.
        /// </summary>
        protected readonly string nameOfCanvasObject = "CanvasObject";

        /// <summary>
        /// True if the active script is already initialized, else false.
        /// </summary>
        protected bool instantiated = false;

        /// <summary>
        /// The ActionHistory which is responsible for the undo/redo operations.
        /// </summary>
        protected ActionHistory actionHistory;

        /// <summary>
        /// The garbage can the deleted nodes will be moved to.
        /// </summary>
        protected GameObject garbageCan;

        /// <summary>
        /// The name of the garbage can gameObject.
        /// </summary>
        protected const string GarbageCanName = "GarbageCan";

        /// <summary>
        /// Finds the GameObject that contains the CanvasOperations and components
        /// and saves it in the <see cref="canvasObject"/>.
        /// </summary>
        /// <returns>true if the <see cref="canvasObject"/> could be found</returns>
        protected bool InitializeCanvasObject()
        {
            canvasObject = GameObject.Find(nameOfCanvasObject);
            return canvasObject != null;
        }

        /// <summary>
        /// The undo operation which has to be implemented specifically by subclasses.
        /// </summary>
        public abstract void Undo();

        /// <summary>
        /// The redo operation which has to be implemented specifically by subclasses.
        /// </summary>
        public abstract void Redo();

        /// <summary>
        /// The operation which has to be done in the specific subclass.
        /// </summary>
        public abstract void Update();

        public void GetActionHistory()
        {
            GameObject playerSettings = GameObject.Find("Player Settings");
            Debug.Log(playerSettings);
            ActionHistory actionHistory = playerSettings.GetComponentInChildren<ActionHistory>();
            Debug.Log(actionHistory);
            this.actionHistory = actionHistory;

        }
        /// <summary>
        /// Sets <see cref="hoveredObject"/> to given <paramref name="interactableObject"/>.
        /// Will be called while any <see cref="InteractableObject"/> is being hovered over.
        /// </summary>
        /// <param name="interactableObject">new value for <see cref="hoveredObject"/></param>
        protected void LocalAnyHoverIn(InteractableObject interactableObject)
        {
            hoveredObject = interactableObject.gameObject;

            // FIXME: For an unknown reason, the mouse events in InteractableObject will be
            // triggered twice per frame, which causes this method to be called twice.
            // We need to further investigate this issue.
            //Debug.LogFormat("{0}.LocalAnyHoverIn({1})\n",
            //                this.GetType().FullName, 
            //                interactableObject == null ? "NULL" : interactableObject.name);
            //try
            //{
            //    if (interactableObject.gameObject != hoveredObject)
            //    {
            //        Assert.IsNull(hoveredObject);
            //        hoveredObject = interactableObject.gameObject;
            //    }
            //}
            //catch (Exception e)
            //{
            //    Debug.LogErrorFormat("{0}.LocalAnyHoverIn throws {1}. [hoveredObject: {2}. interactableObject: {3}.\n",
            //                          this.GetType().FullName,
            //                          e.Message, hoveredObject == null ? "NULL" : hoveredObject.name, interactableObject.name);
            //    // FIXME: There are AssertionExceptions 
            //}
        }

        /// <summary>
        /// Sets <see cref="hoveredObject"/> to <code>null</code>.
        /// Will be called whenever any <see cref="InteractableObject"/> is no longer being hovered over.
        /// </summary>
        /// <param name="interactableObject">object no longer be hovered over (ignored here)</param>
        protected void LocalAnyHoverOut(InteractableObject interactableObject)
        {
            hoveredObject = null;

            // FIXME: For an unknown reason, the mouse events in InteractableObject will be
            // triggered twice per frame, which causes this method to be called twice.
            // We need to further investigate this issue.
            //Debug.LogFormat("{0}.LocalAnyHoverOut({1})\n",
            //                this.GetType().FullName,
            //                interactableObject == null ? "NULL" : interactableObject.name);
            //try
            //{
            //    Assert.IsTrue(hoveredObject == interactableObject.gameObject);
            //    hoveredObject = null;
            //}
            //catch (Exception e)
            //{
            //    Debug.LogErrorFormat("{0}.LocalAnyHoverOut throws {1}. [hoveredObject: {2}. interactableObject: {3}.\n",
            //                         this.GetType().FullName,
            //                         e.Message, hoveredObject == null ? "NULL" : hoveredObject.name, interactableObject.name);
            //    // FIXME: There are AssertionExceptions 
            //}
        }
    }
}
