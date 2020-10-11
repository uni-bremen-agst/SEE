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
        /// The IDs of the objects to deselect.
        /// </summary>
        public uint id;

        /// <summary>
        /// Whether the objects of given ID should be selected of deselected.
        /// </summary>
        public bool select;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="hoverableObjects">The hoverable object to select.</param>
        /// <param name="select">Whether the objects should be selected or deselected.</param>
        public SelectionAction(HoverableObject hoverableObject, bool select)
        {
            Assert.IsNotNull(hoverableObject);

            id = hoverableObject.id;
            this.select = select;
        }

        protected override void ExecuteOnServer()
        {
        }

        /// <summary>
        /// Selects or deselects the HoverableObjects of given ids.
        /// </summary>
        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                HoverableObject hoverableObject = (HoverableObject)InteractableObject.Get(id);

                if (hoverableObject)
                {
                    if (select)
                    {
                        if (hoverableObject.GetComponent<Outline>() == null)
                        {
                            Outline.Create(hoverableObject.gameObject, false);
                        }
                        else
                        {
                            UnityEngine.Debug.LogWarning("HoverableObject already has an Outline attached!");
                        }
                    }
                    else
                    {
                        Outline outline = hoverableObject.GetComponent<Outline>();
                        if (outline)
                        {
                            UnityEngine.Object.Destroy(outline);
                        }
                        else
                        {
                            UnityEngine.Debug.LogWarning("HoverableObject does not have an Outline attached!");
                        }
                    }
                }
            }
        }
    }

}
