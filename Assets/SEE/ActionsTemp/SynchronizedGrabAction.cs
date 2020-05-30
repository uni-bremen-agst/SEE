using UnityEngine;

namespace SEE.Controls
{

    public class SynchronizedGrabAction : AbstractAction
    {
        public uint id;
        public Vector3 startPosition;
        public Vector3 endPosition;
        public bool grab;
        public bool actionFinalized;



        public SynchronizedGrabAction(GrabbableObject grabbableObject, Vector3 startPosition, bool grab, bool actionFinalized = true) : base(true)
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
            foreach (GrabbableObject grabbableObject in Object.FindObjectsOfType<GrabbableObject>()) // TODO(torben): save them in a faster way!
            {
                if (grabbableObject.id == id)
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
                        SelectionAction selectionAction = Object.FindObjectOfType<SelectionAction>();
                        SelectionAction.Animation.Start();
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
