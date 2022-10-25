using System;
using System.Collections.Generic;
using SEE.GO;
using SEE.Utils;
using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// An action to grab and move nodes.
    /// </summary>
    internal class MoveAction : AbstractPlayerAction
    {
        /// <summary>
        /// Returns a new instance of <see cref="MoveAction"/>.
        /// </summary>
        /// <returns>new instance of <see cref="MoveAction"/></returns>
        internal static ReversibleAction CreateReversibleAction() => new MoveAction();

        /// <summary>
        /// Returns a new instance of <see cref="MoveAction"/> that can continue
        /// with the user interaction so far.
        /// </summary>
        /// <returns>new instance</returns>
        public override ReversibleAction NewInstance() => new MoveAction();

        /// <summary>
        /// Returns the set of IDs of all game objects changed by this action.
        /// <see cref="ReversibleAction.GetChangedObjects"/>
        /// </summary>
        /// <returns>returns the ID of the currently grabbed object if any; otherwise the empty set</returns>
        public override HashSet<string> GetChangedObjects()
        {
            return grabbedObject.IsGrabbed ? new HashSet<string> { grabbedObject.Name } : new HashSet<string>();
        }

        /// Returns the <see cref="ActionStateType"/> of this action.
        /// </summary>
        /// <returns><see cref="ActionStateType.Move"/></returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateType.Move;
        }

        /// <summary>
        /// Data structure to manage the game object that was grabbed.
        /// Provides the necessary capability for Undo/Redo.
        /// </summary>
        private struct GrabbedObject
        {
            /// <summary>
            /// The game object that was grabbed. May be null.
            /// </summary>
            private GameObject gameObject;

            /// <summary>
            /// Grabs the given <paramref name="gameObject"/>.
            /// </summary>
            /// <param name="gameObject">object to be grabbed</param>
            /// <exception cref="ArgumentNullException">thrown if <paramref name="gameObject"/> is null</exception>
            public void Grab(GameObject gameObject)
            {
                if (gameObject != null)
                {
                    this.gameObject = gameObject;
                    originalPositionOfGrabbedObject = gameObject.transform.position;
                    IsGrabbed = true;
                }
                else
                {
                    throw new ArgumentNullException("Parameter must not be null");
                }
            }

            /// <summary>
            /// The currently grabbed object is considered to be ungrabbed.
            /// </summary>
            /// <exception cref="InvalidOperationException">thrown if not object is currently grabbed</exception>
            public void UnGrab()
            {
                if (!IsGrabbed || gameObject == null)
                {
                    throw new InvalidOperationException("No object is being grabbed.");
                }
                else
                {
                    IsGrabbed = false;
                    // Note: We do not set gameObject to null because we may need its
                    // value later for Undo/Redo.
                }
            }

            /// <summary>
            /// Whether an object has been grabbed.
            /// </summary>
            /// <returns>true if an object has been grabbed</returns>
            /// <remarks>The default value for <c>bool</c> is <c>false</c>, which is exactly
            /// what we need</remarks>
            internal bool IsGrabbed { private set; get; }

            /// <summary>
            /// The name of the grabbed object if any was grabbed; otherwise the empty string.
            /// </summary>
            internal string Name
            {
                get => gameObject != null ? gameObject.name : String.Empty;
            }

            /// <summary>
            /// The position of the grabbed object in world space.
            /// </summary>
            /// <exception cref="InvalidOperationException">in case no object is currently grabbed</exception>
            internal Vector3 Position
            {
                get
                {
                    if (IsGrabbed && gameObject != null)
                    {
                        return gameObject.transform.position;
                    }
                    else
                    {
                        throw new InvalidOperationException("No object is being grabbed.");
                    }
                }
            }

            /// <summary>
            /// The original position of <see cref="GameObject"/> when it was grabbed.
            /// Required to return it to is original position when the action is canceled
            /// or undone.
            /// </summary>
            private Vector3 originalPositionOfGrabbedObject;

            /// <summary>
            /// The current position of <see cref="GameObject"/> in world space. More precisely, the
            /// position that was last set via <see cref="MoveTo(Vector3)"/>.
            /// </summary>
            private Vector3 currentPositionOfGrabbedObject;

            /// <summary>
            /// Returns the grabbed object to its original position when it was grabbed.
            /// This method will called for Undo.
            /// </summary>
            internal void MoveToOrigin()
            {
                /// Note: We cannot call <see cref="MoveTo(Vector3)"/> because that
                /// would also update <see cref="currentPositionOfGrabbedObject"/>.
                if (gameObject)
                {
                    gameObject.transform.position = originalPositionOfGrabbedObject;
                }
            }

            /// <summary>
            /// Returns the grabbed object to its last position before it was returned
            /// to its origin via <see cref="MoveToOrigin"/>.
            /// This method will called for Redo.
            /// </summary>
            internal void MoveToLastUserRequestedPosition()
            {
                MoveTo(currentPositionOfGrabbedObject);
            }

            /// <summary>
            /// Moves the grabbed object to <paramref name="targetPosition"/> in world space.
            /// </summary>
            /// <param name="targetPosition"></param>
            internal void MoveTo(Vector3 targetPosition)
            {
                if (gameObject)
                {
                    gameObject.transform.position = targetPosition;
                    currentPositionOfGrabbedObject = targetPosition;
                }
            }
        }

        /// <summary>
        /// The currently grabbed object if any.
        /// </summary>
        private GrabbedObject grabbedObject = new GrabbedObject();

        /// <summary>
        /// The distance from the the position of <see cref="grabbedObject"/> when it was grabbed to
        /// the origin of the user's pointing device (e.g., main camera on a desktop, controller
        /// in VR) in world space. This distance will be maintained while the user has grabbed
        /// an object.
        /// </summary>
        private float distanceToUser;

        /// <summary>
        /// Index of the left mouse button.
        /// </summary>
        private const int LeftMouseButton = 0;

        /// <summary>
        /// <see cref="ReversibleAction.Update"/>.
        /// </summary>
        /// <returns>true if completed</returns>
        public override bool Update()
        {
            if (SEEInput.Cancel()) // cancel movement
            {
                Debug.Log("Dragging cancelled.\n");
                if (grabbedObject.IsGrabbed)
                {
                    Debug.Log($"Dragged object {grabbedObject.Name} returned to its location.\n");

                    // The grabbed object needs to be returned to its original location.
                    grabbedObject.MoveToOrigin();

                    // Reset action.
                    grabbedObject.UnGrab();
                    currentState = ReversibleAction.Progress.NoEffect;
                }

            }
            else if (Input.GetMouseButton(LeftMouseButton)) // start or continue moving a grabbed object
            {
                if (!grabbedObject.IsGrabbed)
                {
                    // User is starting dragging.
                    InteractableObject hoveredObject = InteractableObject.HoveredObjectWithWorldFlag;
                    if (hoveredObject && hoveredObject.gameObject.HasNodeRef())
                    {
                        grabbedObject.Grab(hoveredObject.gameObject);
                        distanceToUser = Vector3.Distance(Raycasting.UserPointsTo().origin, grabbedObject.Position);
                        currentState = ReversibleAction.Progress.InProgress;
                        Debug.Log($"Starting dragging of {grabbedObject.Name} at distance {distanceToUser}.\n");
                    }
                    else
                    {
                        Debug.Log("Nothing to be dragged.\n");
                    }
                }
                else
                {
                    // Assert: grabbedObject != null
                    Ray ray = Raycasting.UserPointsTo();
                    Vector3 targetPosition = ray.origin + distanceToUser * ray.direction;
                    // User is continuing dragging.
                    Debug.Log($"Continuing dragging {grabbedObject.Name} to {targetPosition}.\n");
                    grabbedObject.MoveTo(targetPosition);
                }
            }
            else if (grabbedObject.IsGrabbed) // dragging has ended
            {
                Debug.Log($"Dragging of {grabbedObject.Name} finalized.\n");
                // Finalize the action with the grabbed object.
                grabbedObject.UnGrab();
                // Action is finished.
                currentState = ReversibleAction.Progress.Completed;
                return true;
            }
            return false;
        }

        /// <summary>
        /// <see cref="ReversibleAction.Undo"/>.
        /// </summary>
        public override void Undo()
        {
            base.Undo();
            grabbedObject.MoveToOrigin();
        }

        /// <summary>
        /// <see cref="ReversibleAction.Redo"/>.
        /// </summary>
        public override void Redo()
        {
            base.Redo();
            grabbedObject.MoveToLastUserRequestedPosition();
        }
    }
}