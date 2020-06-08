using SEE.Controls;
using SEE.Game;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Net
{

    public class ZoomIntoAction : AbstractAction
    {
        public uint id;



        public ZoomIntoAction(HoverableObject hoverableObject) : base(false)
        {
            Assert.IsNotNull(hoverableObject);
            id = hoverableObject.id;
        }



        protected override bool ExecuteOnServer()
        {
            Server.gameState.zoomIDStack.Push(id);
            return true;
        }

        protected override bool ExecuteOnClient()
        {
            HoverableObject hoverableObject = (HoverableObject)InteractableObject.Get(id);
            if (hoverableObject)
            {
                Controls.SelectionAction selectionAction = Object.FindObjectOfType<Controls.SelectionAction>();
                Controls.SelectionAction.Animation.Start();
                Transformer.ZoomInto(selectionAction.gameObject, hoverableObject.gameObject);
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

    public class ZoomOutOfAction : AbstractAction
    {
        public uint id;



        public ZoomOutOfAction(HoverableObject hoverableObject) : base(false)
        {
            Assert.IsNotNull(hoverableObject);
            id = hoverableObject.id;
        }



        protected override bool ExecuteOnServer()
        {
            Server.gameState.zoomIDStack.Pop();
            return true;
        }

        protected override bool ExecuteOnClient()
        {
            HoverableObject hoverableObject = (HoverableObject)InteractableObject.Get(id);
            if (hoverableObject)
            {
                Controls.SelectionAction selectionAction = Object.FindObjectOfType<Controls.SelectionAction>();
                Controls.SelectionAction.Animation.Start();
                Transformer.ZoomOutOf(selectionAction.gameObject, hoverableObject.gameObject);
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

    public class ZoomRootAction : AbstractAction
    {
        public uint id;



        public ZoomRootAction(HoverableObject hoverableObject) : base(false)
        {
            Assert.IsNotNull(hoverableObject);
            id = hoverableObject.id;
        }



        protected override bool ExecuteOnServer()
        {
            Server.gameState.zoomIDStack.Clear();
            return true;
        }

        protected override bool ExecuteOnClient()
        {
            HoverableObject hoverableObject = (HoverableObject)InteractableObject.Get(id);
            if (hoverableObject)
            {
                Controls.SelectionAction selectionAction = Object.FindObjectOfType<Controls.SelectionAction>();
                Controls.SelectionAction.Animation.Start();
                Transformer.ZoomRoot(selectionAction.gameObject, hoverableObject.gameObject);
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
