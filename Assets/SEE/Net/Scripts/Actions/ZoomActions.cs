using SEE.Controls;
using SEE.Game;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Net
{

    /// <summary>
    /// Zooms into a <see cref="HoverableObject"/> for every client.
    /// </summary>
    public class ZoomIntoAction : AbstractAction
    {
        /// <summary>
        /// The unique ID of the object.
        /// </summary>
        public uint id;



        /// <summary>
        /// Constructs a zoom-into-action for given object.
        /// </summary>
        /// <param name="hoverableObject">The object to zoom into.</param>
        public ZoomIntoAction(HoverableObject hoverableObject) : base(false)
        {
            Assert.IsNotNull(hoverableObject);
            id = hoverableObject.id;
        }



        /// <summary>
        /// Modifies the game state for future clients.
        /// </summary>
        /// <returns></returns>
        protected override bool ExecuteOnServer()
        {
            Server.gameState.zoomIDStack.Push(id);
            return true;
        }

        /// <summary>
        /// Zooms into the object of given ID.
        /// </summary>
        /// <returns></returns>
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

    /// <summary>
    /// Zooms out of a <see cref="HoverableObject"/> for every client.
    /// </summary>
    public class ZoomOutOfAction : AbstractAction
    {
        /// <summary>
        /// The unique ID of the object.
        /// </summary>
        public uint id;



        /// <summary>
        /// Constructs a zoom-out-of-action for given object.
        /// </summary>
        /// <param name="hoverableObject">The object to zoom out of.</param>
        public ZoomOutOfAction(HoverableObject hoverableObject) : base(false)
        {
            Assert.IsNotNull(hoverableObject);
            id = hoverableObject.id;
        }



        /// <summary>
        /// Modifies the game state for future clients.
        /// </summary>
        /// <returns></returns>
        protected override bool ExecuteOnServer()
        {
            Server.gameState.zoomIDStack.Pop();
            return true;
        }

        /// <summary>
        /// Zooms out of the object of given ID.
        /// </summary>
        /// <returns></returns>
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

    /// <summary>
    /// Zooms back to the root for every client.
    /// </summary>
    public class ZoomRootAction : AbstractAction
    {
        /// <summary>
        /// The unique ID of the object.
        /// </summary>
        public uint id;



        /// <summary>
        /// Constructs a zoom-root-action.
        /// </summary>
        /// <param name="hoverableObject">The currently focused object.</param>
        public ZoomRootAction(HoverableObject hoverableObject) : base(false)
        {
            Assert.IsNotNull(hoverableObject);
            id = hoverableObject.id;
        }



        /// <summary>
        /// Modifies the game state for future clients.
        /// </summary>
        /// <returns></returns>
        protected override bool ExecuteOnServer()
        {
            Server.gameState.zoomIDStack.Clear();
            return true;
        }

        /// <summary>
        /// Zooms back to the root from object of given ID.
        /// </summary>
        /// <returns></returns>
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
