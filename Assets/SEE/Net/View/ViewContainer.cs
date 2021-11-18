using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Net
{
    /// <summary>
    /// A view container contains multiple views, which synchronize objects for multiple
    /// clients. Only one view container per object is allowed! It must be located at the
    /// root!
    /// </summary>
    [DisallowMultipleComponent]
    public class ViewContainer : MonoBehaviour
    {
        /// <summary>
        /// The value of an invalid ID.
        /// </summary>
        public const uint InvalidID = uint.MaxValue;

        /// <summary>
        /// A dictionary, mapping unique IDs to its corresponding view container.
        /// </summary>
        private static readonly Dictionary<uint, ViewContainer> viewContainers = new Dictionary<uint, ViewContainer>();

        /// <summary>
        /// The unique ID of the view container.
        /// </summary>
        [SerializeField] public uint id = InvalidID;

        /// <summary>
        /// The end point of the owner of the view container.
        /// </summary>
        [SerializeField] public IPEndPoint owner;

        /// <summary>
        /// The path to the prefab, which instantiated this view container.
        /// </summary>
        [SerializeField] public string prefabPath;

        /// <summary>
        /// All views, this view container manages.
        /// </summary>
        [SerializeField] public readonly AbstractView[] views = new AbstractView[1];

        /// <summary>
        /// Initializes a view container.
        ///
        /// <paramref name="owner"/> and <paramref name="prefabPath"/> must not be
        /// <code>null</code>.
        /// </summary>
        /// <param name="id">The unique ID of the view container.</param>
        /// <param name="owner">The owner end point of the view container.</param>
        /// <param name="prefabPath">The prefab path, that instantiated this view
        /// container.</param>
        public void Initialize(uint id, IPEndPoint owner, string prefabPath)
        {
#if UNITY_EDITOR
            if (owner?.Address == null)
            {
                throw new ArgumentNullException(nameof(owner));
            }
            if (views == null)
            {
                throw new NullReferenceException($"{nameof(views)} may not be null!");
            }
#endif
            this.id = id;
            this.owner = owner;
            this.prefabPath = prefabPath;
            viewContainers.Add(id, this);
            foreach (AbstractView view in views)
            {
                view.Initialize(this);
            }
        }

        /// <summary>
        /// Destroys the view container and removes its entry from
        /// <see cref="viewContainers"/>.
        /// </summary>
        public void OnDestroy()
        {
            if (id != InvalidID)
            {
                bool result = viewContainers.Remove(id);
                Assert.IsTrue(result);
            }
        }

        /// <summary>
        /// Finds the view container of given ID.
        /// </summary>
        /// <param name="id">The unique ID of a view container.</param>
        /// <returns>The view container of given ID or <code>null</code>, if it does not
        /// exist.</returns>
        public static ViewContainer GetViewContainerByID(uint id)
        {
            if (!viewContainers.ContainsKey(id))
            {
                return null;
            }
            return viewContainers[id];
        }

        /// <summary>
        /// Finds all view containers with given owner.
        /// </summary>
        /// <param name="owner">The owner end point of the view containers.</param>
        /// <returns>An array containing all the end points with given owner.</returns>
        public static ViewContainer[] GetViewContainersByOwner(IPEndPoint owner)
        {
            return viewContainers.Values.Where(viewContainer => viewContainer.owner.Equals(owner)).ToArray();
        }

        /// <summary>
        /// Finds the index of the given view inside of this view container.
        /// </summary>
        /// <param name="view">The view.</param>
        /// <returns>The index of the view or <code>-1</code>, if this view container
        /// does not contain the given view.</returns>
        public int GetIndexOf(AbstractView view)
        {
            for (int i = 0; i < views.Length; i++)
            {
                if (views[i] == null)
                {
                    break;
                }
                if (views[i].Equals(view))
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Finds the view of given index.
        /// </summary>
        /// <param name="index">The index of the view.</param>
        /// <returns>The view of given index or <code>null</code>, if the index is out of
        /// bounds.</returns>
        public AbstractView GetViewByIndex(int index)
        {
            if (index < 0 || index >= views.Length)
            {
                return null;
            }
            return views[index];
        }

        /// <summary>
        /// Whether this client is the owner of the view container.
        /// </summary>
        /// <returns><code>true</code> if this client is the owner of the view container,
        /// <code>false</code> otherwise.</returns>
        public bool IsOwner()
        {
            bool isOwner = Network.UseInOfflineMode || owner == null || Client.Connection == null;

            if (!isOwner && owner.Port == ((IPEndPoint)Client.Connection.ConnectionInfo.LocalEndPoint).Port)
            {
                if (Network.LookupLocalIPAddresses().Contains(owner.Address))
                {
                    return true;
                }
            }
            return isOwner;
        }
    }
}
