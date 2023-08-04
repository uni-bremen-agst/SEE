using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using SEE.Net.Actions;
using SEE.Net.Util;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SocialPlatforms;

namespace SEE.Net
{
    /// <summary>
    /// DOC
    /// </summary>
    public class ClientServerNetwork : NetworkBehaviour
    {
        private void Update()
        {
            if (!IsOwner)
            {
                return;
            }
        }

        [ServerRpc]
        public void BroadcastActionServerRpc(string serializedAction)
        {
            Debug.Log("Broadcast action");
            ExecuteActionClientRpc(serializedAction);
        }

        [ClientRpc]
        public void ExecuteActionClientRpc(string serializedAction)
        {
            if (IsOwner) return;
            AbstractNetAction action = ActionSerializer.Deserialize(serializedAction);
            Debug.Log("Execute action on client: " + action.ToString());
            action.ExecuteOnClient();
        }
    }
}
