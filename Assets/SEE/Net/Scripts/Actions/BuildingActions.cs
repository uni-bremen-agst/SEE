using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Net
{
    public class SetParentOfBuildingTransformAction : AbstractAction
    {
        public string uniqueGameObjectName;
        public string uniqueParentName;

        public SetParentOfBuildingTransformAction(GameObject gameObject, GameObject parent) : base(false)
        {
            Assert.IsNotNull(gameObject);

            uniqueGameObjectName = gameObject.name;
            uniqueParentName = parent ? parent.name : null;
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
                GameObject parent = uniqueParentName != null ? GameObject.Find(uniqueParentName) : null;
                if (gameObject)
                {
                    result = true;
                    gameObject.transform.parent = parent ? parent.transform : null;
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
