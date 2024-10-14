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
        internal static readonly Dictionary<ulong, HashSet<InteractableObject>> SelectedObjects
            = new Dictionary<ulong, HashSet<InteractableObject>>();

        /// <summary>
        /// The id of the interactable.
        /// </summary>
        public string ID;

        /// <summary>
        /// Whether the interactable should be selected.
        /// </summary>
        public bool Select;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="interactable">The interactable to be (de)selected.</param>
        /// <param name="select">Whether the interactable should be selected.</param>
        public SetSelectNetAction(InteractableObject interactable, bool select)
        {
            Assert.IsNotNull(interactable);

            ID = interactable.name;
            Select = select;
        }

        /// <summary>
        /// Adds/removes the interactable objects of given id to
        /// <see cref="SelectedObjects"/>.
        /// </summary>
        public override void ExecuteOnServer()
        {
            if (Select)
            {
                InteractableObject interactable = InteractableObject.Get(ID);
                if (interactable)
                {
                    ulong requester = Requester;
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
                InteractableObject interactable = InteractableObject.Get(ID);
                if (interactable)
                {
                    ulong requester = Requester;
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
        public override void ExecuteOnClient()
        {
            InteractableObject interactable = InteractableObject.Get(ID);
            if (interactable)
            {
                interactable.SetSelect(Select, false);
            }
        }
    }
}
