using SEE.Controls;
using UnityEngine;

namespace SEE.Net
{

    public class GrabAction : AbstractAction
    {
        public uint id;
        public Vector3 startPosition;
        public Vector3 endPosition;
        public bool grab;
        public bool actionFinalized;



        public GrabAction(GrabbableObject grabbableObject, Vector3 startPosition, bool grab, bool actionFinalized = true) : base(true)
        {
            id = grabbableObject.id;
            this.startPosition = startPosition;
            endPosition = grabbableObject.transform.position;
            this.grab = grab;
            this.actionFinalized = actionFinalized;
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
                if (grab)
                {
                    grabbableObject.Grab(null, IsRequester());
                }
                else
                {
                    grabbableObject.transform.position = endPosition;
                    grabbableObject.Release(IsRequester());
                }
                if (!grab && !actionFinalized)
                {
                    Controls.SelectionAction selectionAction = Object.FindObjectOfType<Controls.SelectionAction>();
                    Controls.SelectionAction.Animation.Start();
                    iTween.MoveTo(grabbableObject.gameObject,
                        iTween.Hash(
                            "position", startPosition,
                            "time", 0.75f,
                            "oncompletetarget", selectionAction.gameObject,
                            "oncomplete", "ResetCompleted"
                        )
                    );
                }
                return true;
            }
            return false;
        }

        protected override bool UndoOnServer()
        {
            throw new System.NotImplementedException();
        }

        protected override bool UndoOnClient()
        {
            throw new System.NotImplementedException();
        }

        protected override bool RedoOnServer()
        {
            throw new System.NotImplementedException();
        }

        protected override bool RedoOnClient()
        {
            throw new System.NotImplementedException();
        }
    }

}
