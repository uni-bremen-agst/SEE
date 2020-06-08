using System;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Net
{

    [DisallowMultipleComponent]
    public class ViewContainer : MonoBehaviour
    {
        public const uint InvalidID = uint.MaxValue;

        [SerializeField] public uint id = InvalidID;
        [SerializeField] public IPEndPoint owner;
        [SerializeField] public string prefabPath;
        [SerializeField] private View[] views = new View[1];
        
        private static Dictionary<uint, ViewContainer> viewContainers = new Dictionary<uint, ViewContainer>();



        public void Initialize(uint id, IPEndPoint owner, string prefabPath)
        {
#if UNITY_EDITOR
            if (owner == null || owner.Address == null)
            {
                throw new ArgumentNullException("owner");
            }
            if (views == null)
            {
                throw new ArgumentNullException("views");
            }
#endif
            this.id = id;
            this.owner = owner;
            this.prefabPath = prefabPath;
            viewContainers.Add(id, this);
            for (int i = 0; i < views.Length; i++)
            {
                views[i].Initialize(this);
            }
        }

        public void OnDestroy()
        {
            if (id != InvalidID)
            {
                bool result = viewContainers.Remove(id);
                Assert.IsTrue(result);
            }
        }

        public static ViewContainer GetViewContainerByID(uint id)
        {
            if (!viewContainers.ContainsKey(id))
            {
                return null;
            }
            return viewContainers[id];
        }

        public static ViewContainer[] GetViewContainersByOwner(IPEndPoint owner)
        {
            List<ViewContainer> result = new List<ViewContainer>();
            foreach (ViewContainer viewContainer in viewContainers.Values)
            {
                if (viewContainer.owner.Equals(owner))
                {
                    result.Add(viewContainer);
                }
            }
            return result.ToArray();
        }

        public int GetIndexOf(View view)
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

        public View GetViewByIndex(int index)
        {
            if (index < 0 || index >= views.Length)
            {
                return null;
            }
            return views[index];
        }

        public bool IsOwner()
        {
            bool isOwner = Network.UseInOfflineMode || owner == null || Client.Connection == null;

            if (!isOwner && owner.Port == ((IPEndPoint)Client.Connection.ConnectionInfo.LocalEndPoint).Port)
            {
                foreach (IPAddress ipAddress in Network.LookupLocalIPAddresses())
                {
                    if (ipAddress.Equals(owner.Address))
                    {
                        return true;
                    }
                }
            }
            return isOwner;
        }
    }

}
