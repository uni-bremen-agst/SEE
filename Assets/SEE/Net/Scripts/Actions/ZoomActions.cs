using SEE.Controls;
using SEE.Game;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Net
{

    public class ZoomIntoAction : AbstractAction
    {
        public uint id;



        public ZoomIntoAction(HoverableObject hoverableObject) : base(true)
        {
            Assert.IsNotNull(hoverableObject);
            id = hoverableObject.id;
        }



        protected override bool ExecuteOnServer()
        {
            return true;
        }

        protected override bool ExecuteOnClient()
        {
            foreach (HoverableObject hoverableObject in Object.FindObjectsOfType<HoverableObject>()) // TODO(torben): faster acquisition
            {
                if (hoverableObject.id == id)
                {
                    Controls.SelectionAction selectionAction = Object.FindObjectOfType<Controls.SelectionAction>();
                    Controls.SelectionAction.Animation.Start();
                    Transformer.ZoomInto(selectionAction.gameObject, hoverableObject.gameObject);
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

    public class ZoomOutOfAction : AbstractAction
    {
        public uint id;



        public ZoomOutOfAction(HoverableObject hoverableObject) : base(true)
        {
            Assert.IsNotNull(hoverableObject);
            id = hoverableObject.id;
        }



        protected override bool ExecuteOnServer()
        {
            return true;
        }

        protected override bool ExecuteOnClient()
        {
            foreach (HoverableObject hoverableObject in Object.FindObjectsOfType<HoverableObject>()) // TODO(torben): faster acquisition
            {
                if (hoverableObject.id == id)
                {
                    Controls.SelectionAction selectionAction = Object.FindObjectOfType<Controls.SelectionAction>();
                    Controls.SelectionAction.Animation.Start();
                    Transformer.ZoomOutOf(selectionAction.gameObject, hoverableObject.gameObject);
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

    public class ZoomRootAction : AbstractAction
    {
        public uint id;



        public ZoomRootAction(HoverableObject hoverableObject) : base(true)
        {
            Assert.IsNotNull(hoverableObject);
            id = hoverableObject.id;
        }



        protected override bool ExecuteOnServer()
        {
            return true;
        }

        protected override bool ExecuteOnClient()
        {
            foreach (HoverableObject hoverableObject in Object.FindObjectsOfType<HoverableObject>()) // TODO(torben): faster acquisition
            {
                if (hoverableObject.id == id)
                {
                    Controls.SelectionAction selectionAction = Object.FindObjectOfType<Controls.SelectionAction>();
                    Controls.SelectionAction.Animation.Start();
                    Transformer.ZoomRoot(selectionAction.gameObject, hoverableObject.gameObject);
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
