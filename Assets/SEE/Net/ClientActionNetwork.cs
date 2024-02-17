using System;
using System.Collections;
using System.IO;
using System.IO.Compression;
using SEE.Net.Actions;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Networking;

namespace SEE.Net
{
    /// <summary>
    /// DOC
    /// </summary>
    public class ClientActionNetwork : NetworkBehaviour
    {
        /// <summary>
        /// The protocol. Either "http://" or "https://".
        /// </summary>
        private const string Protocol = "http://";
        /// <summary>
        /// The URL part identifying the client REST API.
        /// </summary>
        private const string ClientAPI = "/api/v1/file/client/";
        /// <summary>
        /// Where files are stored locally on the server side (relative directory).
        /// </summary>
        private const string RelativeServertContentDirectory = "/Multiplayer/";

        /// <summary>
        /// Where files are stored locally on the server side (absolute directory).
        /// </summary>
        private string AbsoluteServerContentDirectory => Application.streamingAssetsPath + RelativeServertContentDirectory;

        /// <summary>
        /// The complete URL of the Client REST API.
        /// </summary>
        private string ClientRestAPI => Protocol + Network.BackendDomain + ClientAPI;

        /// <summary>
        /// Fetches the multiplayer city files from the backend and syncs the current
        /// server state with this client.
        /// </summary>
        public void Start()
        {
            if (!IsServer && !IsHost)
            {
                ServerActionNetwork serverNetwork = GameObject.Find("Server").GetComponent<ServerActionNetwork>();
                serverNetwork.SyncFilesServerRpc();
            }
        }

        /// <summary>
        /// Fetches the Source file from the backend which should be a zipped file and unzips it.
        /// </summary>
        IEnumerator GetSource()
        {
            Debug.Log($"DOMAIN IS: {Network.BackendDomain}.\n");
            using UnityWebRequest webRequest = UnityWebRequest.Get(ClientRestAPI + "source?serverId=" + Network.ServerId
                                                                   + "&roomPassword=" + Network.Instance.RoomPassword);
            webRequest.downloadHandler = new DownloadHandlerFile(AbsoluteServerContentDirectory + "src.zip");

            // Request and wait for the desired page.
            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error fetching source from backend: " + webRequest.error);
                StartCoroutine(GetGxl());
            }
            else
            {
                try
                {
                    // unzip the source code
                    ZipFile.ExtractToDirectory(AbsoluteServerContentDirectory + "src.zip",
                                               AbsoluteServerContentDirectory + "src");
                    StartCoroutine(GetGxl());
                }
                catch (Exception e)
                {
                    Debug.LogError("Error unzipping source code: " + e.Message);
                }
            }
        }

        /// <summary>
        /// Fetches the Gxl file from the backend.
        /// </summary>
        IEnumerator GetGxl()
        {
            using UnityWebRequest webRequest = UnityWebRequest.Get(ClientRestAPI + "gxl?serverId=" + Network.ServerId
                                                                   + "&roomPassword=" + Network.Instance.RoomPassword);
            webRequest.downloadHandler = new DownloadHandlerFile(AbsoluteServerContentDirectory + "multiplayer.gxl");

            // Request and wait for the desired page.
            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error fetching source from backend: " + webRequest.error);
            }
            StartCoroutine(GetConfig());
        }

        /// <summary>
        /// Fetches the Gxl file from the backend.
        /// </summary>
        IEnumerator GetConfig()
        {
            using UnityWebRequest webRequest = UnityWebRequest.Get(ClientRestAPI + "csv?serverId=" + Network.ServerId
                                                                   + "&roomPassword=" + Network.Instance.RoomPassword);
            webRequest.downloadHandler = new DownloadHandlerFile(AbsoluteServerContentDirectory + "multiplayer.cfg");

            // Request and wait for the desired page.
            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error fetching source from backend: " + webRequest.error);
            }
            StartCoroutine(GetSolution());
        }

        /// <summary>
        /// Fetches the solution file from the backend.
        /// </summary>
        IEnumerator GetSolution()
        {
            using UnityWebRequest webRequest = UnityWebRequest.Get(ClientRestAPI + "solution?serverId=" + Network.ServerId
                                                                   + "&roomPassword=" + Network.Instance.RoomPassword);
            webRequest.downloadHandler = new DownloadHandlerFile(AbsoluteServerContentDirectory + "multiplayer.sln");

            // Request and wait for the desired page.
            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error fetching source from backend: " + webRequest.error);
            }
            StartCoroutine(GetCsv());
        }

        /// <summary>
        /// Fetches the csv file from the backend
        /// </summary>
        IEnumerator GetCsv()
        {
            using UnityWebRequest webRequest = UnityWebRequest.Get(ClientRestAPI + "csv?serverId=" + Network.ServerId
                                                                   + "&roomPassword=" + Network.Instance.RoomPassword);
            webRequest.downloadHandler = new DownloadHandlerFile(AbsoluteServerContentDirectory + "multiplayer.csv");

            // Request and wait for the desired page.
            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error fetching source from backend: " + webRequest.error);
            }
            ServerActionNetwork serverNetwork = GameObject.Find("Server").GetComponent<ServerActionNetwork>();
            serverNetwork.SyncClientServerRpc(NetworkManager.Singleton.LocalClientId);
        }

        /// <summary>
        /// Executes an Action, even if the sender and this client are the same, this is used
        /// for synchronizing server state.
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
        /// Then fetches the source file.
        /// </summary>
        [ClientRpc]
        public void SyncFilesClientRpc(string serverId, string backendDomain)
        {
            Network.ServerId = serverId;
            Network.BackendDomain = backendDomain;
            if(Directory.Exists(AbsoluteServerContentDirectory))
            {
                Directory.Delete(AbsoluteServerContentDirectory, true);
                Directory.CreateDirectory(AbsoluteServerContentDirectory);
            }
            StartCoroutine(GetSource());
        }
    }
}
