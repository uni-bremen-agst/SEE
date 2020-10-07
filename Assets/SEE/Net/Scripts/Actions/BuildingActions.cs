using SEE.Controls;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Net
{

    /// <summary>
    /// !!! IMPORTANT !!!
    ///   See <see cref="AbstractAction"/> before modifying this class!
    /// </summary>
    public class HighlightBuildingAction : AbstractAction
    {
        public uint id;

        public HighlightBuildingAction(HighlightableObject highlightableObject)
        {
            Assert.IsNotNull(highlightableObject);

            id = highlightableObject.id;
        }

        protected override void ExecuteOnServer()
        {
        }

        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                ((HighlightableObject)InteractableObject.Get(id))?.Hovered(false);
            }
        }
    }

    /// <summary>
    /// !!! IMPORTANT !!!
    ///   See <see cref="AbstractAction"/> before modifying this class!
    /// </summary>
    public class UnhighlightBuildingAction : AbstractAction
    {
        public uint id;

        public UnhighlightBuildingAction(HighlightableObject highlightableObject)
        {
            Assert.IsNotNull(highlightableObject);

            id = highlightableObject.id;
        }

        protected override void ExecuteOnServer()
        {
        }

        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                ((HighlightableObject)InteractableObject.Get(id))?.Unhovered();
            }
        }
    }
    
    /// <summary>
    /// !!! IMPORTANT !!!
    ///   See <see cref="AbstractAction"/> before modifying this class!
    /// </summary>
    public class GrabBuildingAction : AbstractAction
    {
        public uint id;

        public GrabBuildingAction(GrabbableObject grabbableObject)
        {
            Assert.IsNotNull(grabbableObject);

            id = grabbableObject.id;
        }

        protected override void ExecuteOnServer()
        {
        }

        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                ((GrabbableObject)InteractableObject.Get(id))?.Grab(false);
            }
        }
    }

    /// <summary>
    /// !!! IMPORTANT !!!
    ///   See <see cref="AbstractAction"/> before modifying this class!
    /// </summary>
    public class ReleaseBuildingAction : AbstractAction
    {
        public uint id;

        public ReleaseBuildingAction(GrabbableObject grabbableObject)
        {
            Assert.IsNotNull(grabbableObject);

            id = grabbableObject.id;
        }

        protected override void ExecuteOnServer()
        {
        }

        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                ((GrabbableObject)InteractableObject.Get(id))?.Release(false);
            }
        }
    }

    /// <summary>
    /// !!! IMPORTANT !!!
    ///   See <see cref="AbstractAction"/> before modifying this class!
    /// </summary>
    public class SynchronizeBuildingTransformAction : AbstractAction
    {
        public string uniqueGameObjectName;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 localScale;

        public SynchronizeBuildingTransformAction(GameObject gameObject, bool syncLocalScale)
        {
            Assert.IsNotNull(gameObject);

            uniqueGameObjectName = gameObject.name;
            position = gameObject.transform.position;
            rotation = gameObject.transform.rotation;
            localScale = syncLocalScale ? gameObject.transform.localScale : Vector3.zero;
        }

        protected override void ExecuteOnServer()
        {
        }

        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                GameObject gameObject = GameObject.Find(uniqueGameObjectName);
                if (gameObject)
                {
                    gameObject.GetComponent<Synchronizer>()?.NotifyJustReceivedUpdate();
                    gameObject.transform.position = position;
                    gameObject.transform.rotation = rotation;
                    if (localScale.sqrMagnitude > 0.0f)
                    {
                        gameObject.transform.localScale = localScale;
                    }
                }
            }
        }
    }
}
