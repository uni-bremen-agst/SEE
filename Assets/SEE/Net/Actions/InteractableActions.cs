using System.Collections.Generic;
using System.Net;
using SEE.Controls;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Net
{
    /// <summary>
    /// !!! IMPORTANT !!!
    ///   See <see cref="AbstractNetAction"/> before modifying this class!
    /// </summary>
    public class SetHoverAction : AbstractNetAction
    {
        /// <summary>
        /// Every hovered object of the end point of every client. This is only used by
        /// the server.
        /// </summary>
        internal static readonly Dictionary<IPEndPoint, HashSet<InteractableObject>> HoveredObjects = new Dictionary<IPEndPoint, HashSet<InteractableObject>>();

        /// <summary>
        /// The id of the interactable.
        /// </summary>
        public string id;

        /// <summary>
        /// The hover flags of the interactable.
        /// </summary>
        public uint hoverFlags;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="interactable">The interactable to be (un)hovered.</param>
        /// <param name="hoverFlags">The hover flags of the interactable.</param>
        public SetHoverAction(InteractableObject interactable, uint hoverFlags)
        {
            Assert.IsNotNull(interactable);

            id = interactable.name;
            this.hoverFlags = hoverFlags;
        }

        /// <summary>
        /// Adds/removes the interactable objects of given id to
        /// <see cref="HoveredObjects"/>.
        /// </summary>
        protected override void ExecuteOnServer()
        {
            if (hoverFlags != 0)
            {
                InteractableObject interactable = InteractableObject.Get(id);
                if (interactable)
                {
                    IPEndPoint requester = GetRequester();
                    if (!HoveredObjects.TryGetValue(requester, out HashSet<InteractableObject> interactables))
                    {
                        interactables = new HashSet<InteractableObject>();
                        HoveredObjects.Add(requester, interactables);
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
                    if (HoveredObjects.TryGetValue(requester, out HashSet<InteractableObject> interactables))
                    {
                        interactables.Remove(interactable);
                        if (interactables.Count == 0)
                        {
                            HoveredObjects.Remove(requester);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Sets the hover value for the interactable object of given id.
        /// </summary>
        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                InteractableObject interactable = InteractableObject.Get(id);
                if (interactable)
                {
                    interactable.SetHoverFlags(hoverFlags, false);
                }
            }
        }
    }

    /// <summary>
    /// !!! IMPORTANT !!!
    ///   See <see cref="AbstractNetAction"/> before modifying this class!
    /// </summary>
    public class SetSelectAction : AbstractNetAction
    {
        /// <summary>
        /// Every selected object of the end point of every client. This is only used by
        /// the server.
        /// </summary>
        internal static readonly Dictionary<IPEndPoint, HashSet<InteractableObject>> SelectedObjects = new Dictionary<IPEndPoint, HashSet<InteractableObject>>();

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
        public SetSelectAction(InteractableObject interactable, bool select)
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

    /// <summary>
    /// !!! IMPORTANT !!!
    ///   See <see cref="AbstractNetAction"/> before modifying this class!
    /// </summary>
    public class SetGrabAction : AbstractNetAction
    {
        /// <summary>
        /// Every grabbed object of the end point of every client. This is only used by
        /// the server.
        /// </summary>
        internal static readonly Dictionary<IPEndPoint, HashSet<InteractableObject>> GrabbedObjects = new Dictionary<IPEndPoint, HashSet<InteractableObject>>();

        /// <summary>
        /// The id of the interactable.
        /// </summary>
        public string id;

        /// <summary>
        /// Whether the interactable should be grabbed.
        /// </summary>
        public bool grab;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="interactable">The interactable to be (un)grabbed.</param>
        /// <param name="grab">Whether the interactable should be grabbed.</param>
        public SetGrabAction(InteractableObject interactable, bool grab)
        {
            Assert.IsNotNull(interactable);

            id = interactable.name;
            this.grab = grab;
        }

        /// <summary>
        /// Adds/removes the interactable objects of given id to
        /// <see cref="GrabbedObjects"/>.
        /// </summary>
        protected override void ExecuteOnServer()
        {
            if (grab)
            {
                InteractableObject interactable = InteractableObject.Get(id);
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
                InteractableObject interactable = InteractableObject.Get(id);
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
                InteractableObject interactable = InteractableObject.Get(id);
                if (interactable)
                {
                    interactable.SetGrab(grab, false);
                }
            }
        }
    }

    /// <summary>
    /// Updates position, rotation and potentially local scale of an interactable
    /// object.
    ///
    /// !!! IMPORTANT !!!
    ///   See <see cref="AbstractNetAction"/> before modifying this class!
    /// </summary>
    public class SynchronizeInteractableAction : AbstractNetAction
    {
        /// <summary>
        /// The id of the interactable.
        /// </summary>
        public string id;

        /// <summary>
        /// The position of the interactable.
        /// </summary>
        public Vector3 position;

        /// <summary>
        /// The rotation of the interactable.
        /// </summary>
        public Quaternion rotation;

        /// <summary>
        /// The local scale of the interactable or <see cref="Vector3.zero"/>, if the
        /// local scale is not to be synchronized.
        /// </summary>
        public Vector3 localScale;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="interactable">The interactable to be synchronized.</param>
        /// <param name="syncLocalScale">Whether the local scale is to be synchronized.
        /// </param>
        public SynchronizeInteractableAction(InteractableObject interactable, bool syncLocalScale)
        {
            Assert.IsNotNull(interactable);

            id = interactable.name;
            position = interactable.transform.position;
            rotation = interactable.transform.rotation;
            localScale = syncLocalScale ? interactable.transform.localScale : Vector3.zero;
        }

        protected override void ExecuteOnServer()
        {
        }

        /// <summary>
        /// Updates position, rotation and potentially local scale of the interactable
        /// object of given id.
        /// </summary>
        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                InteractableObject interactable = InteractableObject.Get(id);
                if (interactable)
                {
                    interactable.InteractableSynchronizer?.NotifyJustReceivedUpdate();
                    interactable.transform.position = position;
                    interactable.transform.rotation = rotation;
                    if (localScale.sqrMagnitude > 0.0f)
                    {
                        interactable.transform.localScale = localScale;
                    }
                }
            }
        }
    }
}
