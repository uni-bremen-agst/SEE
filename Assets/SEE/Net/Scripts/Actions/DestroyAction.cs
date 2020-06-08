using SEE.Utils;
using System.Net;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Net
{

    public class DestroyAction : AbstractAction
    {
        public string prefabPath;
        public string ownerIpAddress;
        public int ownerPort;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
        public int viewContainerID;

        public DestroyAction(ViewContainer viewContainer) : base(true)
        {
            Assert.IsNotNull(viewContainer.gameObject);

            prefabPath = viewContainer.prefabPath;
            ownerIpAddress = Network.UseInOfflineMode ? null : Client.LocalEndPoint.Address.ToString();
            ownerPort = Network.UseInOfflineMode ? -1 : Client.LocalEndPoint.Port;
            position = viewContainer.gameObject.transform.position;
            rotation = viewContainer.gameObject.transform.rotation;
            scale = viewContainer.gameObject.transform.localScale;
            viewContainerID = viewContainer.id;
        }

        protected override bool ExecuteOnServer()
        {
            return true;
        }

        protected override bool ExecuteOnClient()
        {
            ViewContainer viewContainer = ViewContainer.GetViewContainerByID(viewContainerID);
            if (viewContainer)
            {
                Object.Destroy(viewContainer.gameObject);
                return true;
            }
            return false;
        }

        protected override bool UndoOnServer()
        {
            return true;
        }

        protected override bool UndoOnClient()
        {
            if (!ViewContainer.GetViewContainerByID(viewContainerID))
            {
                GameObject prefab = Resources.Load<GameObject>(prefabPath);
                if (!prefab)
                {
                    Assertions.InvalidCodePath("Prefab of path '" + prefabPath + "' could not be found!");
                    return false;
                }

                GameObject go = Object.Instantiate(prefab, null, true);
                if (!go)
                {
                    Assertions.InvalidCodePath("Object could not be instantiated with prefab '" + prefab + "'!");
                    return false;
                }

                if (!Network.UseInOfflineMode)
                {
                    go.GetComponent<ViewContainer>().Initialize(viewContainerID, new IPEndPoint(IPAddress.Parse(ownerIpAddress), ownerPort), prefabPath);
                }
                go.transform.position = position;
                go.transform.rotation = rotation;
                go.transform.localScale = scale;
                return true;
            }
            return false;
        }

        protected override bool RedoOnServer()
        {
            return true;
        }

        protected override bool RedoOnClient()
        {
            return ExecuteOnClient();
        }
    }

}
