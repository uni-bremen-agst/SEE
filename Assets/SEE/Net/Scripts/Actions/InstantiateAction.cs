using SEE.Utils;
using System.Net;
using UnityEngine;

namespace SEE.Net
{

    /// <summary>
    /// !!! IMPORTANT !!!
    ///   See <see cref="AbstractAction"/> before modifying this class!
    ///   
    /// Instantiates a prefab for each client. The prefab must contain exactly one
    /// <see cref="ViewContainer"/> in the root object.
    /// </summary>
    public class InstantiateAction : AbstractAction
    {
        /// <summary>
        /// The next unique ID of the view container;
        /// </summary>
        private static uint nextViewID = 0;

        /// <summary>
        /// The path of the prefab to instantiate.
        /// </summary>
        public string prefabPath;

        /// <summary>
        /// The IP-address of the owner of the prefab.
        /// </summary>
        public string ownerIpAddress;

        /// <summary>
        /// The port of the owner of the view container.
        /// </summary>
        public int ownerPort;

        /// <summary>
        /// The position of the prefab.
        /// </summary>
        public Vector3 position;

        /// <summary>
        /// The rotation of the prefab.
        /// </summary>
        public Quaternion rotation;

        /// <summary>
        /// The scale of the prefab.
        /// </summary>
        public Vector3 scale;

        /// <summary>
        /// The unique ID, that will be assigned the new view container.
        /// </summary>
        public uint viewContainerID;

        /// <summary>
        /// Constructs an instantiate action with defaults for position, rotation and
        /// scale.
        /// </summary>
        /// <param name="prefabPath">The path of the prefab.</param>
        public InstantiateAction(string prefabPath) : base(true)
        {
            Initialize(prefabPath, Vector3.zero, Quaternion.identity, Vector3.one);
        }

        /// <summary>
        /// Constructs an instantiate action.
        /// </summary>
        /// <param name="prefabPath">The path of the prefab.</param>
        /// <param name="position">The position of the prefab.</param>
        /// <param name="rotation">The rotation of the prefab.</param>
        /// <param name="scale">The scale of the prefab.</param>
        public InstantiateAction(string prefabPath, Vector3 position, Quaternion rotation, Vector3 scale) : base(true)
        {
            Initialize(prefabPath, position, rotation, scale);
        }

        /// <summary>
        /// Initializes an instantiate action.
        /// </summary>
        /// <param name="prefabPath">The path of the prefab.</param>
        /// <param name="position">The position of the prefab.</param>
        /// <param name="rotation">The rotation of the prefab.</param>
        /// <param name="scale">The scale of the prefab.</param>
        private void Initialize(string prefabPath, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            this.prefabPath = prefabPath;
            ownerIpAddress = Network.UseInOfflineMode ? null : Client.LocalEndPoint.Address.ToString();
            ownerPort = Network.UseInOfflineMode ? -1 : Client.LocalEndPoint.Port;
            this.position = position;
            this.rotation = rotation;
            this.scale = scale;
            viewContainerID = ViewContainer.InvalidID;
        }

        /// <summary>
        /// Assignes a unique ID for the view container.
        /// </summary>
        /// <returns><code>true</code>.</returns>
        protected override bool ExecuteOnServer()
        {
            viewContainerID = nextViewID++;
            return true;
        }

        /// <summary>
        /// Instantiates the prefab.
        /// </summary>
        /// <returns><code>true</code> if prefab could be instantiated, <code>false</code> otherwise.</returns>
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

        /// <summary>
        /// Destroys the instantiated prefab.
        /// </summary>
        /// <returns><code>true</code>.</returns>
        protected override bool UndoOnClient()
        {
            Object.Destroy(ViewContainer.GetViewContainerByID(viewContainerID).gameObject);
            return true;
        }

        protected override bool RedoOnServer()
        {
            return true;
        }

        /// <summary>
        /// Re-instantiates the prefab.
        /// </summary>
        /// <returns>The result of <see cref="ExecuteOnClient"/>.</returns>
        protected override bool RedoOnClient()
        {
            return ExecuteOnClient();
        }
    }

}
