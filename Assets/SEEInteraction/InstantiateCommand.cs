using SEE.Net;
using SEE.Net.Internal;
using System.Net;
using UnityEngine;

namespace SEE.Command
{

    public class InstantiateCommand : AbstractCommand
    {
        private static int lastViewID = -1;

        public string prefabPath;
        public string ownerIpAddress;
        public int ownerPort;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
        public int viewID;

        public InstantiateCommand(string prefabPath) : base(true)
        {
            Initialize(prefabPath, Vector3.zero, Quaternion.identity, Vector3.one);
        }

        public InstantiateCommand(string prefabPath, Vector3 position, Quaternion rotation, Vector3 scale) : base(true)
        {
            Initialize(prefabPath, position, rotation, scale);
        }

        private void Initialize(string prefabPath, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            this.prefabPath = prefabPath;
            ownerIpAddress = Net.Network.UseInOfflineMode ? null : Client.LocalEndPoint.Address.ToString();
            ownerPort = Net.Network.UseInOfflineMode ? -1 : Client.LocalEndPoint.Port;
            this.position = position;
            this.rotation = rotation;
            this.scale = scale;
            viewID = -1;
        }

        internal override void ExecuteOnClient()
        {
            GameObject prefab = Resources.Load<GameObject>(prefabPath);
            if (!prefab)
            {
                Debug.LogError("Prefab of path '" + prefabPath + "' could not be found!");
                return;
            }
            GameObject go = Object.Instantiate(prefab, null, true);
            if (!go)
            {
                Debug.Log("Object could not be instantiated with prefab '" + prefab + "'!");
                return;
            }
            if (!Net.Network.UseInOfflineMode)
            {
                go.GetComponent<ViewContainer>().Initialize(viewID, new IPEndPoint(IPAddress.Parse(ownerIpAddress), ownerPort));
            }
            go.transform.position = position;
            go.transform.rotation = rotation;
            go.transform.localScale = scale;
        }

        internal override void ExecuteOnServer()
        {
            viewID = ++lastViewID;
        }

        internal override void RedoOnClient()
        {
            throw new System.NotImplementedException();
        }

        internal override void RedoOnServer()
        {
            throw new System.NotImplementedException();
        }

        internal override void UndoOnClient()
        {
            throw new System.NotImplementedException();
        }

        internal override void UndoOnServer()
        {
            throw new System.NotImplementedException();
        }
    }

}
