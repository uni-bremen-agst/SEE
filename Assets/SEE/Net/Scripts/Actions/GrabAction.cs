using SEE.Controls;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Net
{

    public class GrabAction : AbstractAction
    {
        public uint id;
        public Vector3 startLocalPosition;
        public Vector3 endLocalPosition;
        public bool grab;
        public bool actionFinalized;



        public GrabAction(GrabbableObject grabbableObject, Vector3 startLocalPosition, bool grab, bool actionFinalized = true) : base(!grab && actionFinalized)
        {
            id = grabbableObject.id;
            this.startLocalPosition = startLocalPosition;
            endLocalPosition = grabbableObject.transform.localPosition;
            this.grab = grab;
            this.actionFinalized = actionFinalized;
        }

        

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
