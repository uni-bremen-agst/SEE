using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Net
{
    /// <summary>
    /// !!! IMPORTANT !!!
    ///   See <see cref="AbstractAction"/> before modifying this class!
    /// </summary>
    public class SynchronizeAction : AbstractAction
    {
        public string uniqueGameObjectName;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 localScale;

        public SynchronizeAction(GameObject gameObject, bool syncScale) : base(false)
        {
            Assert.IsNotNull(gameObject);
            uniqueGameObjectName = gameObject.name;
            position = gameObject.transform.position;
            rotation = gameObject.transform.rotation;
            localScale = syncScale ? gameObject.transform.localScale : Vector3.zero;
        }

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
                gameObject.GetComponent<Synchronizer>().NotifyJustReceivedUpdate();
                if (gameObject)
                {
                    result = true;
                    gameObject.transform.position = position;
                    gameObject.transform.rotation = rotation;
                    if (localScale.sqrMagnitude != 0.0f)
                    {
                        gameObject.transform.localScale = localScale;
                    }
                }
            }

            return result;
        }

        protected override bool ExecuteOnServer()
        {
            return true;
        }

        protected override bool UndoOnServer()
        {
            throw new System.NotImplementedException();
        }

        protected override bool UndoOnClient()
        {
            throw new System.NotImplementedException();
        }

        protected override bool RedoOnServer()
        {
            throw new System.NotImplementedException();
        }

        protected override bool RedoOnClient()
        {
            throw new System.NotImplementedException();
        }
    }
}
