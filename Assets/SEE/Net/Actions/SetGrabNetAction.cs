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
    public class SetGrabNetAction : AbstractNetAction
    {
        /// <summary>
        /// Every grabbed object of the end point of every client. This is only used by
        /// the server.
        /// </summary>
        internal static readonly Dictionary<IPEndPoint, HashSet<InteractableObject>> GrabbedObjects
            = new Dictionary<IPEndPoint, HashSet<InteractableObject>>();

        /// <summary>
        /// The id of the interactable.
        /// </summary>
        public string ID;

        /// <summary>
        /// Whether the interactable should be grabbed.
        /// </summary>
        public bool Grab;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="interactable">The interactable to be (un)grabbed.</param>
        /// <param name="grab">Whether the interactable should be grabbed.</param>
        public SetGrabNetAction(InteractableObject interactable, bool grab)
        {
            Assert.IsNotNull(interactable);

            ID = interactable.name;
            Grab = grab;
        }

        /// <summary>
        /// Adds/removes the interactable objects of given id to
        /// <see cref="GrabbedObjects"/>.
        /// </summary>
        protected override void ExecuteOnServer()
        {
            if (Grab)
            {
                InteractableObject interactable = InteractableObject.Get(ID);
                if (interactable)
                {
                    IPEndPoint requester = GetRequester();
                    if (!GrabbedObjects.TryGetValue(requester, out HashSet<InteractableObject> interactables))
                    {
                        interactables = new HashSet<InteractableObject>();
                        GrabbedObjects.Add(requester, interactables);
                    }
                    interactables.Add(interactable);
                }
            }
            else
            {
                InteractableObject interactable = InteractableObject.Get(ID);
                if (interactable)
                {
                    IPEndPoint requester = GetRequester();
                    if (GrabbedObjects.TryGetValue(requester, out HashSet<InteractableObject> interactables))
                    {
                        interactables.Remove(interactable);
                        if (interactables.Count == 0)
                        {
                            GrabbedObjects.Remove(requester);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Sets the grab value for the interactable object of given id.
        /// </summary>
        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                InteractableObject interactable = InteractableObject.Get(ID);
                if (interactable)
                {
                    interactable.SetGrab(Grab, false);
                }
            }
        }
    }
}
