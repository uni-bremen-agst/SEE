using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Controls
{

    public class MoveAction : AbstractAction
    {
        public uint id;
        public Vector3 startPosition;
        public Vector3 endPosition;



        public MoveAction(GrabbableObject grabbableObject, Vector3 startPosition, Vector3 endPosition) : base(false)
        {
            id = grabbableObject.id;
            this.startPosition = startPosition;
            this.endPosition = endPosition;
        }



        protected override bool ExecuteOnServer()
        {
            return true;
        }

        protected override bool ExecuteOnClient()
        {
            foreach (GrabbableObject grabbableObject in Object.FindObjectsOfType<GrabbableObject>())
            {
                if (grabbableObject.id == id)
                {
                    Assert.IsTrue(grabbableObject.isGrabbed);
                    grabbableObject.transform.position = endPosition;
                    return true;
                }
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
