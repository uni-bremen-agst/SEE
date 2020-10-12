using SEE.Controls;
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
                    interactable.SynchronizerObj?.NotifyJustReceivedUpdate();
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
