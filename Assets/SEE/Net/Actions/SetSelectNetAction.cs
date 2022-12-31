using System.Collections.Generic;
using System.Net;
using SEE.Controls;
using UnityEngine.Assertions;

namespace SEE.Net.Actions
{
    /// <summary>
    /// !!! IMPORTANT !!!
    ///   See <see cref="AbstractNetAction"/> before modifying this class!
    /// </summary>
    public class SetSelectNetAction : AbstractNetAction
    {
        /// <summary>
        /// Every selected object of the end point of every client. This is only used by
        /// the server.
        /// </summary>
        internal static readonly Dictionary<IPEndPoint, HashSet<InteractableObject>> SelectedObjects
            = new Dictionary<IPEndPoint, HashSet<InteractableObject>>();

        /// <summary>
        /// The id of the interactable.
        /// </summary>
        public string id;

        /// <summary>
        /// Whether the interactable should be selected.
        /// </summary>
        public bool select;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="interactable">The interactable to be (de)selected.</param>
        /// <param name="select">Whether the interactable should be selected.</param>
        public SetSelectNetAction(InteractableObject interactable, bool select)
        {
            Assert.IsNotNull(interactable);

            id = interactable.name;
            this.select = select;
        }

        /// <summary>
        /// Adds/removes the interactable objects of given id to
        /// <see cref="SelectedObjects"/>.
        /// </summary>
        protected override void ExecuteOnServer()
        {
            if (select)
            {
                InteractableObject interactable = InteractableObject.Get(id);
                if (interactable)
                {
                    IPEndPoint requester = GetRequester();
                    if (!SelectedObjects.TryGetValue(requester, out HashSet<InteractableObject> interactables))
                    {
                        interactables = new HashSet<InteractableObject>();
                        SelectedObjects.Add(requester, interactables);
                    }
                    interactables.Add(interactable);
                }
            }
            else
            {
                InteractableObject interactable = InteractableObject.Get(id);
                if (interactable)
                {
                    IPEndPoint requester = GetRequester();
                    if (SelectedObjects.TryGetValue(requester, out HashSet<InteractableObject> interactables))
                    {
                        interactables.Remove(interactable);
                        if (interactables.Count == 0)
                        {
                            SelectedObjects.Remove(requester);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Sets the select value for the interactable object of given id.
        /// </summary>
        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                InteractableObject interactable = InteractableObject.Get(id);
                if (interactable)
                {
                    interactable.SetSelect(select, false);
                }
            }
        }
    }
}
