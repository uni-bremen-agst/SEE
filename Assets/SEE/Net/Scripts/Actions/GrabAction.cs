using SEE.Controls;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Net
{

    /// <summary>
    /// !!! IMPORTANT !!!
    ///   See <see cref="AbstractAction"/> before modifying this class!
    ///   
    /// Action for grabbing/releasing grabbable objects.
    /// </summary>
    public class GrabAction : AbstractAction
    {
        /// <summary>
        /// The unique ID of the grabbable object.
        /// </summary>
        public uint id;

        /// <summary>
        /// The local start location of the object, when it was first grabbed.
        /// </summary>
        public Vector3 startLocalPosition;

        /// <summary>
        /// The local end position of the object for when it is released. Is ignored, if
        /// <see cref="grab"/> is <code>false</code>.
        /// </summary>
        public Vector3 endLocalPosition;

        /// <summary>
        /// Whether the object is now grabbed. If <code>false</code>, the object is released.
        /// </summary>
        public bool grab;

        /// <summary>
        /// Whether the object was released successfully without canceling. Is ignored,
        /// if <see cref="grab"/> is false.
        /// </summary>
        public bool actionFinalized;



        /// <summary>
        /// Constructs a new grab action.
        /// </summary>
        /// <param name="grabbableObject">The grabbable object to grab/release.</param>
        /// <param name="startLocalPosition">The start location for grabbing the object.
        /// </param>
        /// <param name="grab">Whether the object is now grabbed. If <code>false</code>, the
        /// object is released.</param>
        /// <param name="actionFinalized">Whether the object was released successfully
        /// without canceling. Is ignored, if <see cref="grab"/> is false.</param>
        public GrabAction(GrabbableObject grabbableObject, Vector3 startLocalPosition, bool grab, bool actionFinalized = true) : base(!grab && actionFinalized)
        {
            id = grabbableObject.id;
            this.startLocalPosition = startLocalPosition;
            endLocalPosition = grabbableObject.transform.localPosition;
            this.grab = grab;
            this.actionFinalized = actionFinalized;
        }

        

        /// <summary>
        /// Updates the game state for future clients.
        /// </summary>
        /// <returns><code>true</code>.</returns>
        protected override bool ExecuteOnServer()
        {
            if (grab)
            {
                Server.gameState.selectedGameObjectIDs.Remove(id);
            }
            else
            {
                Server.gameState.selectedGameObjectIDs.Add(id);
            }
            return true;
        }

        /// <summary>
        /// Grabs/releases the grabbable object. If <see cref="grab"/> and
        /// <see cref="actionFinalized"/> are <code>false</code>, the object is smoothly
        /// moved back to its original position.
        /// </summary>
        /// <returns><code>true</code>.</returns>
        protected override bool ExecuteOnClient()
        {
            GrabbableObject grabbableObject = (GrabbableObject)InteractableObject.Get(id);
            if (grabbableObject)
            {
                if (grab)
                {
                    grabbableObject.Grab(null, IsRequester());
                }
                else
                {
                    grabbableObject.transform.localPosition = endLocalPosition;
                    grabbableObject.Release(IsRequester());
                    if (!actionFinalized)
                    {
                        Controls.SelectionAction selectionAction = Object.FindObjectOfType<Controls.SelectionAction>();
                        Controls.SelectionAction.Animation.Start();
                        iTween.MoveTo(grabbableObject.gameObject,
                            iTween.Hash(
                                "position", startLocalPosition,
                                "time", 0.75f,
                                "oncompletetarget", selectionAction.gameObject,
                                "oncomplete", "ResetCompleted",
                                "islocal", true
                            )
                        );
                    }
                }
                return true;
            }
            return false;
        }

        protected override bool UndoOnServer()
        {
            return true;
        }
        
        protected override bool UndoOnClient()
        {
            GrabbableObject grabbableObject = (GrabbableObject)InteractableObject.Get(id);
            Assert.IsNotNull(grabbableObject);
            grabbableObject.transform.localPosition = startLocalPosition;
            return true;
        }

        protected override bool RedoOnServer()
        {
            return ExecuteOnServer();
        }

        protected override bool RedoOnClient()
        {
            return ExecuteOnClient();
        }
    }

}
