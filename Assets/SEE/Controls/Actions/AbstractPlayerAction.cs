using SEE.Utils;
using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// An abstract superclass of all PlayerActions such as NewNodeAction, ScaleNodeAction, EditNodeAction and AddEdgeAction.
    /// The most important attribute for all of them is the hoveredObject, which will be instantiated and updated by LocalAnyHoverIn and LocalAnyHoverOut.
    /// </summary>
    public abstract class AbstractPlayerAction : ReversibleAction
    {
        /// <summary>
        /// The current name of the gameObject that contains the canvas operations and components.
        /// </summary>
        protected readonly string nameOfCanvasObject = "CanvasObject";

        /// <summary>
        /// The gameObject that contains the CanvasGenerator and the actual CanvasObject.
        /// </summary>
        protected GameObject canvasObject;

        /// <summary>
        /// The object that the cursor hovers over.
        /// </summary>
        protected GameObject hoveredObject = null;

        /// <summary>
        /// True if the active script is already initialized, else false.
        /// </summary>
        protected bool instantiated = false;

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
        /// The undo operation which has to be implemented specifically by subclasses
        /// to revert the effect of an executed action.
        /// See <see cref="ReversibleAction.Undo"/>.
        /// </summary>
        public virtual void Undo() 
        {
            // intentionally left blank; can be overridden by subclasses
        }

        /// <summary>
        /// The redo operation which has to be implemented specifically by subclasses
        /// to revert the effect of an undone action, in other words, to return to 
        /// the state at the point in time when <see cref="Undo"/> was called.
        /// See <see cref="ReversibleAction.Redo"/>.
        /// </summary>
        public virtual void Redo()
        {
            // intentionally left blank; can be overridden by subclasses
        }

        /// <summary>
        /// Will be called once when the action is started to be executing for the
        /// first time. Intended for intialization purposes.
        /// See <see cref="ReversibleAction.Awake"/>.
        /// </summary>
        public virtual void Awake()
        {
            // intentionally left blank; can be overridden by subclasses
        }

        /// <summary>
        /// Will be called after <see cref="Awake"/> and then again whenever the
        /// action is re-enabled (<see cref="Stop"/> was called before then).
        /// See <see cref="ReversibleAction.Start"/>.
        /// </summary>
        public virtual void Start()
        {
            // intentionally left blank; can be overridden by subclasses
        }

        /// <summary>
        /// Will be called upon every frame when this action is being executed.
        /// See <see cref="ReversibleAction.Update"/>.
        /// </summary>
        public virtual void Update()
        {
            // intentionally left blank; can be overridden by subclasses
        }

        /// <summary>
        /// Will be called when another action is to be executed. This signals that
        /// the action is to be put on hold. No <see cref="Update"/> will occur
        /// while on hold. It may be re-enabled by <see cref="Start"/> again.
        /// </summary>
        public virtual void Stop()
        {
            // intentionally left blank; can be overridden by subclasses
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
