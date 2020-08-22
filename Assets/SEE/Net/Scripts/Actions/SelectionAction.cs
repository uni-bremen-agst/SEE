using SEE.Controls;
using UnityEngine.Assertions;

namespace SEE.Net
{

    /// <summary>
    /// Highlights hoverable objects for all clients, once a client selects an object.
    /// This can also stop highlighting objects on deselection.
    /// </summary>
    public class SelectionAction : AbstractAction
    {
        /// <summary>
        /// The ID of the object to deselect. Is <see cref="uint.MaxValue"/>, if the
        /// object does not exist.
        /// </summary>
        public uint oldID;

        /// <summary>
        /// The ID of the object to select. Is <see cref="uint.MaxValue"/>, if the object
        /// does not exist.
        /// </summary>
        public uint newID;



        /// <summary>
        /// Constructs a new selection action. At least one of the parameters must not be
        /// <code>null</code>. If one of the objects is null, it is simply not
        /// (de)selected. The objects must not be identical.
        /// </summary>
        /// <param name="oldHoverableObject">The hoverable object to deselect.</param>
        /// <param name="newHoverableObject">The hoverable object to select.</param>
        public SelectionAction(HoverableObject oldHoverableObject, HoverableObject newHoverableObject) : base(false)
        {
            Assert.IsTrue(oldHoverableObject != newHoverableObject);
            Assert.IsTrue(oldHoverableObject != null || newHoverableObject != null);

            oldID = oldHoverableObject ? oldHoverableObject.id : uint.MaxValue;
            newID = newHoverableObject ? newHoverableObject.id : uint.MaxValue;
        }



        /// <summary>
        /// Updates the game state for future clients.
        /// </summary>
        /// <returns><code>true</code>.</returns>
        protected override bool ExecuteOnServer()
        {
            if (oldID != uint.MaxValue)
            {
                Server.gameState.selectedGameObjectIDs.Remove(oldID);
            }
            if (newID != uint.MaxValue)
            {
                Server.gameState.selectedGameObjectIDs.Add(newID);
            }
            return true;
        }

        /// <summary>
        /// Deselects hoverable object with <see cref="oldID"/> if it exists and selects
        /// hoverable object with <see cref="newID"/> if it exists.
        /// </summary>
        /// <returns><code>true</code>.</returns>
        protected override bool ExecuteOnClient()
        {
            HoverableObject oldHoverableObject = (HoverableObject)InteractableObject.Get(oldID);
            HoverableObject newHoverableObject = (HoverableObject)InteractableObject.Get(newID);

            Assert.IsTrue(oldHoverableObject != null || newHoverableObject != null);

            if (oldHoverableObject)
            {
                Outline outline = oldHoverableObject.GetComponent<Outline>();
                if (outline)
                {
                    UnityEngine.Object.Destroy(outline);
                }
            }

            if (newHoverableObject)
            {
                if (newHoverableObject.GetComponent<Outline>() == null)
                {
                    Outline.Create(newHoverableObject.gameObject, false);
                }
            }

            return true;
        }

        protected override bool UndoOnServer()
        {
            Utils.Assertions.InvalidCodePath();
            return false;
        }

        protected override bool UndoOnClient()
        {
            Utils.Assertions.InvalidCodePath();
            return false;
        }

        protected override bool RedoOnServer()
        {
            Utils.Assertions.InvalidCodePath();
            return false;
        }

        protected override bool RedoOnClient()
        {
            Utils.Assertions.InvalidCodePath();
            return false;
        }
    }

}
