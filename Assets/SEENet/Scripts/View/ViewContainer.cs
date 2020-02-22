using SEE.Net.Internal;
using System;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

namespace SEE.Net
{

    [DisallowMultipleComponent]
    public class ViewContainer : MonoBehaviour
    {
        public const int INVALID_ID = -1;

        [SerializeField] public int id = INVALID_ID;
        [SerializeField] public IPEndPoint owner;
        [SerializeField] private View[] views = new View[1];
        
        private static Dictionary<int, ViewContainer> viewContainers = new Dictionary<int, ViewContainer>(); // TODO: could probably just be an array

        public void Initialize(int id, IPEndPoint owner)
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
            viewContainers.Add(id, this);
            for (int i = 0; i < views.Length; i++)
            {
                views[i].Initialize(this);
            }
        }
        public static ViewContainer GetViewContainerByID(int id)
        {
            if (!viewContainers.ContainsKey(id))
            {
                return null;
            }
            return viewContainers[id];
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
