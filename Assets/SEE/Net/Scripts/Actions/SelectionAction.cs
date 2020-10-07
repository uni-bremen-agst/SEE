using SEE.Controls;
using UnityEngine.Assertions;

namespace SEE.Net
{

    /// <summary>
    /// !!! IMPORTANT !!!
    ///   See <see cref="AbstractAction"/> before modifying this class!
    ///   
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
        public SelectionAction(HoverableObject oldHoverableObject, HoverableObject newHoverableObject)
        {
            Assert.IsTrue(oldHoverableObject != newHoverableObject);
            Assert.IsTrue(oldHoverableObject != null || newHoverableObject != null);

            oldID = oldHoverableObject ? oldHoverableObject.id : uint.MaxValue;
            newID = newHoverableObject ? newHoverableObject.id : uint.MaxValue;
        }

        protected override void ExecuteOnServer()
        {
        }

        /// <summary>
        /// Deselects hoverable object with <see cref="oldID"/> if it exists and selects
        /// hoverable object with <see cref="newID"/> if it exists.
        /// </summary>
        protected override void ExecuteOnClient()
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
        }
    }

}
