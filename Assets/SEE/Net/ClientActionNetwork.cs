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
        public void Start()
        {
            if(!IsServer && !IsHost)
            {
                ServerActionNetwork serverNetwork = GameObject.Find("Server").GetComponent<ServerActionNetwork>();
                serverNetwork.SyncClientServerRpc(NetworkManager.Singleton.LocalClientId);
            }
        }

        [ClientRpc]
        public void ExecuteActionUnsafeClientRpc(string serializedAction)
        {
            if (IsHost || IsServer)
            {
                return;
            }
            AbstractNetAction action = ActionSerializer.Deserialize(serializedAction);
            action.ExecuteOnClient();
        }

        [ClientRpc]
        public void ExecuteActionClientRpc(string serializedAction)
        {
            if (IsHost  || IsServer)
            {
                return;
            }
            AbstractNetAction action = ActionSerializer.Deserialize(serializedAction);
            if(action.Requester != NetworkManager.Singleton.LocalClientId)
            {
                action.ExecuteOnClient();
            }
        }

        [ClientRpc]
        public void FileTransferClientRpc(byte[] fileData, string fileName, ulong targetClient)
        {
            // Hier kannst du das Byte-Array 'fileData' auf der Clientseite verarbeiten
            // und die Datei mit dem Namen 'fileName' speichern, wenn benötigt.
            Debug.Log($"Received file '{fileName}' with {fileData.Length} bytes.");

            // Beachte, dass du hier möglicherweise weitere Logik hinzufügen musst,
            // um die Datei zu speichern oder zu verarbeiten.
        }
    }
}
