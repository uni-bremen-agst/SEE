using SEE.Controls;
using SEE.Game;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Net
{
    
    public static class ZoomStack
    {
        public static readonly Stack<uint> stack = new Stack<uint>();
        
        public static void Push(uint id) => stack.Push(id);
        public static void Pop() => stack.Pop();
        public static void Clear() => stack.Clear();
    }

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
            ZoomStack.Push(id);
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
            ZoomStack.Pop();
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
            ZoomStack.Clear();
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
