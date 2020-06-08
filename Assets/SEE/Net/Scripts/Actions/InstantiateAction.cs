using SEE.Utils;
using System.Net;
using UnityEngine;

namespace SEE.Net
{

    public class InstantiateAction : AbstractAction
    {
        private static int lastViewID = -1;

        public string prefabPath;
        public string ownerIpAddress;
        public int ownerPort;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
        public int viewContainerID;

        public InstantiateAction(string prefabPath) : base(true)
        {
            Initialize(prefabPath, Vector3.zero, Quaternion.identity, Vector3.one);
        }

        public InstantiateAction(string prefabPath, Vector3 position, Quaternion rotation, Vector3 scale) : base(true)
        {
            Initialize(prefabPath, position, rotation, scale);
        }

        private void Initialize(string prefabPath, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            this.prefabPath = prefabPath;
            ownerIpAddress = Network.UseInOfflineMode ? null : Client.LocalEndPoint.Address.ToString();
            ownerPort = Network.UseInOfflineMode ? -1 : Client.LocalEndPoint.Port;
            this.position = position;
            this.rotation = rotation;
            this.scale = scale;
            viewContainerID = -1;
        }

        protected override bool ExecuteOnServer()
        {
            viewContainerID = ++lastViewID;
            return true;
        }

        protected override bool ExecuteOnClient()
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

        protected override bool UndoOnServer()
        {
            return true;
        }

        protected override bool UndoOnClient()
        {
            Object.Destroy(ViewContainer.GetViewContainerByID(viewContainerID).gameObject);
            return true;
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
