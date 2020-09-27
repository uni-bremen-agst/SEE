using SEE.Controls;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Net
{

    /// <summary>
    /// !!! IMPORTANT !!!
    ///   See <see cref="AbstractAction"/> before modifying this class!
    ///   
    /// Moves a grabbable object to new position for every client.
    /// </summary>
    public class MoveAction : AbstractAction
    {
        /// <summary>
        /// The unique ID of the object to move.
        /// </summary>
        public uint id;

        /// <summary>
        /// The new position of the object.
        /// </summary>
        public Vector3 position;



        /// <summary>
        /// Constructs a move action for given grabbable object and position.
        /// 
        /// <paramref name="grabbableObject"/> must not be <code>null</code>.
        /// </summary>
        /// <param name="grabbableObject">The grabbable object to move.</param>
        /// <param name="position">The new position of the grabbable object.</param>
        public MoveAction(GrabbableObject grabbableObject, Vector3 position) : base(false)
        {
            Assert.IsNotNull(grabbableObject);
            id = grabbableObject.id;
            this.position = position;
        }



        protected override bool ExecuteOnServer()
        {
            return true;
        }

        /// <summary>
        /// Updates the position of the grabbable object.
        /// </summary>
        /// <returns><code>true</code> if position could be updated, <code>false</code> otherwise.</returns>
        protected override bool ExecuteOnClient()
        {
            GrabbableObject grabbableObject = (GrabbableObject)InteractableObject.Get(id);
            if (grabbableObject)
            {
                Assert.IsTrue(grabbableObject.IsGrabbed);
                grabbableObject.transform.position = position;
                return true;
            }
            return false;
        }

        protected override bool UndoOnServer()
        {
            return false;
        }

        protected override bool UndoOnClient()
        {
            return false;
        }

        protected override bool RedoOnServer()
        {
            return false;
        }

        protected override bool RedoOnClient()
        {
            return false;
        }
    }

}
