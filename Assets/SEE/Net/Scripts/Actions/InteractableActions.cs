using SEE.Controls;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Net
{

    /// <summary>
    /// !!! IMPORTANT !!!
    ///   See <see cref="AbstractAction"/> before modifying this class!
    /// </summary>
    public class SetHoverAction : AbstractAction
    {
        internal static readonly Dictionary<IPEndPoint, HashSet<InteractableObject>> HoveredObjects = new Dictionary<IPEndPoint, HashSet<InteractableObject>>();

        public uint id;
        public bool hover;

        public SetHoverAction(InteractableObject interactable, bool hover)
        {
            Assert.IsNotNull(interactable);

            id = interactable.ID;
            this.hover = hover;
        }

        protected override void ExecuteOnServer()
        {
            if (hover)
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

        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                InteractableObject interactable = InteractableObject.Get(id);
                if (interactable)
                {
                    interactable.SetHover(hover, false);
                }
            }
        }
    }

    /// <summary>
    /// !!! IMPORTANT !!!
    ///   See <see cref="AbstractAction"/> before modifying this class!
    /// </summary>
    public class SetSelectAction : AbstractAction
    {
        internal static readonly Dictionary<IPEndPoint, HashSet<InteractableObject>> SelectedObjects = new Dictionary<IPEndPoint, HashSet<InteractableObject>>();

        public uint id;
        public bool select;

        public SetSelectAction(InteractableObject interactable, bool select)
        {
            Assert.IsNotNull(interactable);

            id = interactable.ID;
            this.select = select;
        }

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
    ///   See <see cref="AbstractAction"/> before modifying this class!
    /// </summary>
    public class SetGrabAction : AbstractAction
    {
        internal static readonly Dictionary<IPEndPoint, HashSet<InteractableObject>> GrabbedObjects = new Dictionary<IPEndPoint, HashSet<InteractableObject>>();

        public uint id;
        public bool grab;

        public SetGrabAction(InteractableObject interactable, bool grab)
        {
            Assert.IsNotNull(interactable);

            id = interactable.ID;
            this.grab = grab;
        }

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
    /// !!! IMPORTANT !!!
    ///   See <see cref="AbstractAction"/> before modifying this class!
    /// </summary>
    public class SynchronizeInteractableAction : AbstractAction
    {
        public uint id;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 localScale;

        public SynchronizeInteractableAction(InteractableObject interactable, bool syncLocalScale)
        {
            Assert.IsNotNull(interactable);

            id = interactable.ID;
            position = interactable.transform.position;
            rotation = interactable.transform.rotation;
            localScale = syncLocalScale ? interactable.transform.localScale : Vector3.zero;
        }

        protected override void ExecuteOnServer()
        {
        }

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
