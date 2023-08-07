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
    public class ClientActionNetwork : NetworkBehaviour
    {
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
