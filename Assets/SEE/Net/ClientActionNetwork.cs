using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using SEE.Net.Actions;
using SEE.Net.Util;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SocialPlatforms;

namespace SEE.Net
{ 
    /// <summary>
    /// DOC
    /// </summary>
    public class ClientActionNetwork : NetworkBehaviour
    {
        /// <summary>
        /// Fetches the multiplayer city files from the backend and syncs the current server state with this client.
        /// </summary>
        public void Start()
        {
            if(!IsServer && !IsHost)
            {
                ServerActionNetwork serverNetwork = GameObject.Find("Server").GetComponent<ServerActionNetwork>();
                serverNetwork.SyncFilesServerRpc();
                serverNetwork.SyncClientServerRpc(NetworkManager.Singleton.LocalClientId);
            }
        }


        /// <summary>
        /// Fetches the Source file from the backend which should be a zipped file and unzips it.
        /// </summary>
        IEnumerator GetSource()
        {
            Debug.Log("DOMAIN IS: " + Network.BackendDomain);
            using UnityWebRequest webRequest = UnityWebRequest.Get("http://"  + Network.BackendDomain + "/api/v1/file/client/source?serverId=" + Network.ServerId + "&roomPassword=" + Network.Instance.RoomPassword);
            webRequest.downloadHandler = new DownloadHandlerFile(Application.streamingAssetsPath + "/Multiplayer/src.zip");

            // Request and wait for the desired page.
            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error fetching source from backend: " + webRequest.error);
            }
            else
            {
                try
                {
                    // unzip the source code
                    ZipFile.ExtractToDirectory(Application.streamingAssetsPath + "/Multiplayer/src.zip", Application.streamingAssetsPath + "/Multiplayer/src");
                }
                catch (Exception e)
                {
                    Debug.LogError("Error unzipping source code: " + e.Message);
                }
            }
        }

        /// <summary>
        /// Fetches the Gxl file from the backend
        /// </summary>
        IEnumerator GetGxl()
        {
            using UnityWebRequest webRequest = UnityWebRequest.Get("http://" + Network.BackendDomain + "/api/v1/file/client/gxl?serverId=" + Network.ServerId + "&roomPassword=" + Network.Instance.RoomPassword);
            webRequest.downloadHandler = new DownloadHandlerFile(Application.streamingAssetsPath + "/Multiplayer/multiplayer.gxl");

            // Request and wait for the desired page.
            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error fetching source from backend: " + webRequest.error);
            }
        }

        /// <summary>
        /// Fetches the Gxl file from the backend
        /// </summary>
        IEnumerator GetConfig()
        {
            using UnityWebRequest webRequest = UnityWebRequest.Get("http://" + Network.BackendDomain + "/api/v1/file/client/csv?serverId=" + Network.ServerId + "&roomPassword=" + Network.Instance.RoomPassword);
            webRequest.downloadHandler = new DownloadHandlerFile(Application.streamingAssetsPath + "/Multiplayer/multiplayer.cfg");

            // Request and wait for the desired page.
            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error fetching source from backend: " + webRequest.error);
            }
        }

        /// <summary>
        /// Fetches the solution file from the backend
        /// </summary>
        IEnumerator GetSolution()
        {
            using UnityWebRequest webRequest = UnityWebRequest.Get("http://" + Network.BackendDomain + "/api/v1/file/client/solution?serverId=" + Network.ServerId + "&roomPassword=" + Network.Instance.RoomPassword);
            webRequest.downloadHandler = new DownloadHandlerFile(Application.streamingAssetsPath + "/Multiplayer/multiplayer.sln");

            // Request and wait for the desired page.
            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error fetching source from backend: " + webRequest.error);
            } 
        }

        /// <summary>
        /// Fetches the csv file from the backend
        /// </summary>
        IEnumerator GetCsv()
        {
            using UnityWebRequest webRequest = UnityWebRequest.Get("http://" + Network.BackendDomain + "/api/v1/file/client/csv?serverId=" + Network.ServerId + "&roomPassword=" + Network.Instance.RoomPassword);
            webRequest.downloadHandler = new DownloadHandlerFile(Application.streamingAssetsPath + "/Multiplayer/multiplayer.csv");

            // Request and wait for the desired page.
            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error fetching source from backend: " + webRequest.error);
            }
        }

        /// <summary>
        /// Executes an Action, even if the sender and this client are the same, this is used for synchronizing server state
        /// </summary>
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

        /// <summary>
        /// Executes an action on the client
        /// </summary>
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

        /// <summary>
        /// Allows the server to set the server id given by the backend.
        /// Then fetches the 
        /// </summary>
        [ClientRpc]
        public void SyncFilesClientRpc(string serverId, string backendDomain)
        {
            Network.ServerId = serverId;
            Network.BackendDomain = backendDomain;
            if(Directory.Exists(Application.streamingAssetsPath + "/Multiplayer/"))
            {
                Directory.Delete(Application.streamingAssetsPath + "/Multiplayer/", true);
                Directory.CreateDirectory(Application.streamingAssetsPath + "/Multiplayer/");
            }
            StartCoroutine(GetSource());
            StartCoroutine(GetGxl());
            StartCoroutine(GetCsv());
            StartCoroutine(GetConfig());
            StartCoroutine(GetSolution());

        }
    }
}
