using UnityEngine;

namespace SEE.Controls
{

    public class SynchronizedGrabAction : AbstractAction
    {
        public uint id;
        public Vector3 startPosition;
        public bool grab;
        public bool actionFinalized;



        public SynchronizedGrabAction(GrabbableObject grabbableObject, Vector3 startPosition, bool grab, bool actionFinalized = true) : base(true)
        {
            id = grabbableObject.id;
            this.startPosition = startPosition;
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
                        grabbableObject.Release(IsRequester());
                    }
                    if (!grab && !actionFinalized)
                    {
                        // TODO: tween
                        //if (IsRequester())
                        //{
                        //    SelectionAction.Animation.Start(); // TODO: this should be possible for each client!
                        //    iTween.MoveTo(grabbableObject.gameObject,
                        //        iTween.Hash(
                        //            "position", startPosition,
                        //            "time", 0.75f,
                        //            "oncompletetarget", actor.gameObject,
                        //            "oncomplete", "ResetCompleted"
                        //        )
                        //    );
                        //}
                        grabbableObject.transform.position = startPosition;
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
