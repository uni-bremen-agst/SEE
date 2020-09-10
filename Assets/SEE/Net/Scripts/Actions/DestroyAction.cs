using SEE.Utils;
using System.Net;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Net
{

    /// <summary>
    /// !!! IMPORTANT !!!
    ///   See <see cref="AbstractAction"/> before modifying this class!
    ///   
    /// Destroys an object that was instantiated by using an
    /// <see cref="InstantiateAction"/>.
    /// </summary>
    public class DestroyAction : AbstractAction
    {
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
        /// Constructs a destroy action for the object with given view container.
        /// </summary>
        /// <param name="viewContainer">The view container of the object to be destroyed.
        /// </param>
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

        /// <summary>
        /// Destroys the object with given view container.
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Reconstructs the object with given properties.
        /// </summary>
        /// <returns><code>true</code> if the reconstruction was successfull,
        /// <code>false</code> otherwise.</returns>
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
