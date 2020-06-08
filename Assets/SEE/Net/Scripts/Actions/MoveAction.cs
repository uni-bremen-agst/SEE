using SEE.Controls;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Net
{

    public class MoveAction : AbstractAction
    {
        public uint id;
        public Vector3 position;



        public MoveAction(GrabbableObject grabbableObject, Vector3 position) : base(false)
        {
            id = grabbableObject.id;
            this.position = position;
        }



        protected override bool ExecuteOnServer()
        {
            return true;
        }

        protected override bool ExecuteOnClient()
        {
            GrabbableObject grabbableObject = (GrabbableObject)InteractableObject.Get(id);
            if (grabbableObject)
            {
                Assert.IsTrue(grabbableObject.isGrabbed);
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
