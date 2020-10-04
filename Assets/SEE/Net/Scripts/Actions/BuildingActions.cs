using SEE.Controls;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Net
{
    public class GrabBuildingAction : AbstractAction
    {
        public uint id;

        public GrabBuildingAction(GrabbableObject grabbableObject) : base(false)
        {
            Assert.IsNotNull(grabbableObject);

            id = grabbableObject.id;
        }

        protected override bool ExecuteOnServer() => true;

        protected override bool ExecuteOnClient()
        {
            bool result = false;

            if (IsRequester())
            {
                result = true;
            }
            else
            {
                GrabbableObject grabbableObject = (GrabbableObject)InteractableObject.Get(id);
                if (grabbableObject)
                {
                    result = true;
                    grabbableObject.Grab(false);
                }
            }

            return result;
        }

        protected override bool UndoOnServer() => throw new System.NotImplementedException();
        protected override bool UndoOnClient() => throw new System.NotImplementedException();
        protected override bool RedoOnServer() => throw new System.NotImplementedException();
        protected override bool RedoOnClient() => throw new System.NotImplementedException();
    }

    public class ReleaseBuildingAction : AbstractAction
    {
        public uint id;

        public ReleaseBuildingAction(GrabbableObject grabbableObject) : base(false)
        {
            Assert.IsNotNull(grabbableObject);

            id = grabbableObject.id;
        }

        protected override bool ExecuteOnServer() => true;

        protected override bool ExecuteOnClient()
        {
            bool result = false;

            if (IsRequester())
            {
                result = true;
            }
            else
            {
                GrabbableObject grabbableObject = (GrabbableObject)InteractableObject.Get(id);
                if (grabbableObject)
                {
                    result = true;
                    grabbableObject.Release(false);
                }
            }

            return result;
        }

        protected override bool UndoOnServer() => throw new System.NotImplementedException();
        protected override bool UndoOnClient() => throw new System.NotImplementedException();
        protected override bool RedoOnServer() => throw new System.NotImplementedException();
        protected override bool RedoOnClient() => throw new System.NotImplementedException();
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

        public SynchronizeBuildingTransformAction(GameObject gameObject) : base(false)
        {
            Assert.IsNotNull(gameObject);

            uniqueGameObjectName = gameObject.name;
            position = gameObject.transform.position;
            rotation = gameObject.transform.rotation;
            localScale = gameObject.transform.localScale;
        }

        protected override bool ExecuteOnServer() => true;

        protected override bool ExecuteOnClient()
        {
            bool result = false;

            if (IsRequester())
            {
                result = true;
            }
            else
            {
                GameObject gameObject = GameObject.Find(uniqueGameObjectName);
                if (gameObject)
                {
                    result = true;
                    gameObject.GetComponent<Synchronizer>()?.NotifyJustReceivedUpdate();
                    gameObject.transform.position = position;
                    gameObject.transform.rotation = rotation;
                    gameObject.transform.localScale = localScale;
                }
            }

            return result;
        }

        protected override bool UndoOnServer() => throw new System.NotImplementedException();
        protected override bool UndoOnClient() => throw new System.NotImplementedException();
        protected override bool RedoOnServer() => throw new System.NotImplementedException();
        protected override bool RedoOnClient() => throw new System.NotImplementedException();
    }
}
