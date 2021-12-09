using System.Collections.Generic;
using System.Net;
using SEE.Utils;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Net
{

    /// <summary>
    /// Abstract class for all prefab related actions.
    /// </summary>
    public abstract class PrefabAction : AbstractNetAction
    {
        /// <summary>
        /// Contains every <see cref="InstantiatePrefabAction"/> of every connected
        /// client. This is only used/updated by the server itself.
        /// </summary>
        private static readonly Dictionary<IPEndPoint, List<InstantiatePrefabAction>> Dict = new Dictionary<IPEndPoint, List<InstantiatePrefabAction>>();

        /// <summary>
        /// Returns all active actions of given end point.
        /// </summary>
        /// <param name="ipEndPoint">The end point to be queried.</param>
        /// <returns>All active actions of given end point or <c>null</c>, if none
        /// was found.</returns>
        internal static List<InstantiatePrefabAction> GetActions(IPEndPoint ipEndPoint)
        {
            Dict.TryGetValue(ipEndPoint, out List<InstantiatePrefabAction> result);
            return result;
        }

        /// <summary>
        /// Returns a list of all active actions of every client.
        /// </summary>
        /// <returns>A list of all active actions of every client.</returns>
        internal static List<InstantiatePrefabAction> GetAllActions()
        {
            List<InstantiatePrefabAction> result = new List<InstantiatePrefabAction>();

            foreach (List<InstantiatePrefabAction> actions in Dict.Values)
            {
                result.AddRange(actions);
            }

            return result;
        }

        /// <summary>
        /// Adds the given action, instantiated by given end point to <see cref="Dict"/>.
        /// </summary>
        /// <param name="ipEndPoint">The owning end point.</param>
        /// <param name="action">The action.</param>
        protected static void Add(IPEndPoint ipEndPoint, InstantiatePrefabAction action)
        {
            if (!Dict.ContainsKey(ipEndPoint))
            {
                Dict.Add(ipEndPoint, new List<InstantiatePrefabAction>());
            }
            if (!Dict[ipEndPoint].Contains(action))
            {
                Dict[ipEndPoint].Add(action);
            }
        }

        /// <summary>
        /// Removes the action, that was requested by given end point and created the
        /// view container of given id from <see cref="Dict"/>.
        /// </summary>
        /// <param name="ipEndPoint">The owning end point.</param>
        /// <param name="viewContainerID">The created view container.</param>
        protected static void Remove(IPEndPoint ipEndPoint, uint viewContainerID)
        {
            if (Dict.TryGetValue(ipEndPoint, out List<InstantiatePrefabAction> actions))
            {
                for (int i = 0; i < actions.Count; i++)
                {
                    if (actions[i].viewContainerID == viewContainerID)
                    {
                        actions.RemoveAt(i);
                        if (actions.Count == 0)
                        {
                            Dict.Remove(ipEndPoint);
                        }
                        break;
                    }
                }
            }
        }
    }

    /// <summary>
    /// !!! IMPORTANT !!!
    ///   See <see cref="AbstractNetAction"/> before modifying this class!
    ///   
    /// Instantiates a prefab for each client. The prefab must contain exactly one
    /// <see cref="ViewContainer"/> in the root object.
    /// </summary>
    public class InstantiatePrefabAction : PrefabAction
    {
        /// <summary>
        /// The next unique ID of the view container;
        /// </summary>
        private static uint nextViewID = 0;

        /// <summary>
        /// The IP-address of the owner of the prefab.
        /// </summary>
        public string ownerIpAddress;

        /// <summary>
        /// The path of the prefab to instantiate.
        /// </summary>
        public string prefabPath;

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
        /// Constructs an instantiate action.
        /// </summary>
        /// <param name="owner">The owner of the object to be instantiated.</param>
        /// <param name="prefabPath">The path of the prefab.</param>
        /// <param name="position">The position of the prefab.</param>
        /// <param name="rotation">The rotation of the prefab.</param>
        /// <param name="scale">The scale of the prefab.</param>
        public InstantiatePrefabAction(IPEndPoint owner, string prefabPath, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            Initialize(owner, prefabPath, position, rotation, scale);
        }

        /// <summary>
        /// Initializes an instantiate action.
        /// </summary>
        /// <param name="owner">The owner of the object to be instantiated.</param>
        /// <param name="prefabPath">The path of the prefab.</param>
        /// <param name="position">The position of the prefab.</param>
        /// <param name="rotation">The rotation of the prefab.</param>
        /// <param name="scale">The scale of the prefab.</param>
        private void Initialize(IPEndPoint owner, string prefabPath, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            ownerIpAddress = owner?.Address.ToString();
            ownerPort = owner?.Port ?? -1;
            this.prefabPath = prefabPath;
            this.position = position;
            this.rotation = rotation;
            this.scale = scale;
            viewContainerID = ViewContainer.InvalidID;
        }

        /// <summary>
        /// Assigns a unique ID for the view container.
        /// </summary>
        protected override void ExecuteOnServer()
        {
            if (viewContainerID == ViewContainer.InvalidID)
            {
                viewContainerID = nextViewID++;
                if (ownerIpAddress != null && ownerPort != -1)
                {
                    IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Parse(ownerIpAddress), ownerPort);
                    Add(ipEndPoint, this);
                }
            }
        }

        /// <summary>
        /// Instantiates the prefab.
        /// </summary>
        protected override void ExecuteOnClient()
        {
            GameObject go = PrefabInstantiator.InstantiatePrefab(prefabPath);
            
            IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Parse(ownerIpAddress), ownerPort);
            go.name += " - " + ipEndPoint;

            if (!Network.UseInOfflineMode)
            {
                go.GetComponent<ViewContainer>().Initialize(viewContainerID, ipEndPoint, prefabPath);
            }
            go.transform.position = position;
            go.transform.rotation = rotation;
            go.transform.localScale = scale;
        }
    }

    /// <summary>
    /// !!! IMPORTANT !!!
    ///   See <see cref="AbstractNetAction"/> before modifying this class!
    ///   
    /// Destroys an object that was instantiated by using an
    /// <see cref="InstantiatePrefabAction"/>.
    /// </summary>
    public class DestroyPrefabAction : PrefabAction
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
        public DestroyPrefabAction(ViewContainer viewContainer)
        {
            Assert.IsNotNull(viewContainer);
            Assert.IsNotNull(viewContainer.gameObject);

            prefabPath = viewContainer.prefabPath;
            ownerIpAddress = Network.UseInOfflineMode ? null : viewContainer.owner.Address.ToString();
            ownerPort = Network.UseInOfflineMode ? -1 : viewContainer.owner.Port;
            position = viewContainer.gameObject.transform.position;
            rotation = viewContainer.gameObject.transform.rotation;
            scale = viewContainer.gameObject.transform.localScale;
            viewContainerID = viewContainer.id;
        }

        protected override void ExecuteOnServer()
        {
            IPEndPoint owner = new IPEndPoint(IPAddress.Parse(ownerIpAddress), ownerPort);
            Remove(owner, viewContainerID);
        }

        /// <summary>
        /// Destroys the object with given view container.
        /// </summary>
        protected override void ExecuteOnClient()
        {
            ViewContainer viewContainer = ViewContainer.GetViewContainerByID(viewContainerID);
            if (viewContainer)
            {
                Object.Destroy(viewContainer.gameObject);
            }
        }
    }

}
