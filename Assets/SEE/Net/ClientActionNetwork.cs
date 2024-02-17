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
        /// Where files are stored locally on the server side (relative directory).
        /// </summary>
        public string RelativeServertContentDirectory = "/Multiplayer/";
        /// <summary>
        /// Where files are stored locally on the server side (absolute directory).
        /// </summary>
        private string AbsoluteServerContentDirectory => Application.streamingAssetsPath + RelativeServertContentDirectory;

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
        /// Then fetches the all data files.
        /// </summary>
        [ClientRpc]
        public void SyncFilesClientRpc(string serverId, string backendDomain)
        {
            Network.ServerId = serverId;
            Network.BackendDomain = backendDomain;
            if (Directory.Exists(AbsoluteServerContentDirectory))
            {
                // FIXME: Isn't that a bit dangerous? All content will be deleted.
                Directory.Delete(AbsoluteServerContentDirectory, true);
                Directory.CreateDirectory(AbsoluteServerContentDirectory);
            }
            StartCoroutine(GetAllData());
        }


        private IEnumerator GetAllData()
        {
            Debug.Log($"Server REST API is: {Network.ClientRestAPI}.\n");

            Coroutine getSource = StartCoroutine(GetSource());
            Coroutine getGXL = StartCoroutine(GetGxl());
            Coroutine getConfig = StartCoroutine(GetConfig());
            Coroutine getCSV = StartCoroutine(GetCsv());
            Coroutine getSolution = StartCoroutine(GetSolution());

            yield return getSource;
            yield return getGXL;
            yield return getConfig;
            yield return getCSV;
            yield return getSolution;

            UnzipSources();

            ServerActionNetwork serverNetwork = GameObject.Find("Server").GetComponent<ServerActionNetwork>();
            serverNetwork.SyncClientServerRpc(NetworkManager.Singleton.LocalClientId);
        }

        /// <summary>
        /// The name of the zip file containing the source code.
        /// </summary>
        const string zippedSourcesFilename = "src.zip";

        private void UnzipSources()
        {
            string absoluteSourceArchiveFileName = AbsoluteServerContentDirectory + zippedSourcesFilename;
            if (File.Exists(absoluteSourceArchiveFileName))
            {
                try
                {
                    ZipFile.ExtractToDirectory(absoluteSourceArchiveFileName,
                                               AbsoluteServerContentDirectory + "src");
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error unzipping source-code zip file {absoluteSourceArchiveFileName}: {e.Message}.\n");
                }
            }
            else
            {
                Debug.LogError($"Source-code zip file {absoluteSourceArchiveFileName} was not downloaded.\n");
            }
        }

        /// <summary>
        /// Fetches the source-code zip file from the backend.
        /// </summary>
        private IEnumerator GetSource()
        {
            return GetFile("source", zippedSourcesFilename);
        }

        /// <summary>
        /// Fetches the Gxl file from the backend.
        /// </summary>
        private IEnumerator GetGxl()
        {
            return GetFile("gxl", "multiplayer.gxl");
        }

        private IEnumerator GetFile(string dataType, string filename)
        {
            string url = Network.ClientRestAPI + dataType + "?serverId=" + Network.ServerId
                         + "&roomPassword=" + Network.Instance.RoomPassword;

            using UnityWebRequest webRequest = UnityWebRequest.Get(url);
            webRequest.downloadHandler = new DownloadHandlerFile(AbsoluteServerContentDirectory + filename);

            // Request and wait for the desired page.
            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Error fetching {filename} from backend via {url}: {webRequest.error}.\n");
            }
        }

        /// <summary>
        /// Fetches the configuration file from the backend.
        /// </summary>
        private IEnumerator GetConfig()
        {
            return GetFile("config", "multiplayer.cfg");
            /*

            using UnityWebRequest webRequest = UnityWebRequest.Get(Network.ClientRestAPI + "csv?serverId=" + Network.ServerId
                                                                   + "&roomPassword=" + Network.Instance.RoomPassword);
            webRequest.downloadHandler = new DownloadHandlerFile(AbsoluteServerContentDirectory + "multiplayer.cfg");

            // Request and wait for the desired page.
            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error fetching source from backend: " + webRequest.error);
            }
            StartCoroutine(GetSolution());
            */
        }

        /// <summary>
        /// Fetches the solution file from the backend.
        /// </summary>
        private IEnumerator GetSolution()
        {
            // FIXME: The code commented out below used GetCsv.
            return GetFile("solution", "multiplayer.sln");
            /*

            using UnityWebRequest webRequest = UnityWebRequest.Get(Network.ClientRestAPI + "solution?serverId=" + Network.ServerId
                                                                   + "&roomPassword=" + Network.Instance.RoomPassword);
            webRequest.downloadHandler = new DownloadHandlerFile(AbsoluteServerContentDirectory + "multiplayer.sln");

            // Request and wait for the desired page.
            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error fetching source from backend: " + webRequest.error);
            }
            StartCoroutine(GetCsv());
            */
        }

        /// <summary>
        /// Fetches the csv file from the backend
        /// </summary>
        private IEnumerator GetCsv()
        {
            return GetFile("csv", "multiplayer.csv");

            /*
            using UnityWebRequest webRequest = UnityWebRequest.Get(Network.ClientRestAPI + "csv?serverId=" + Network.ServerId
                                                                   + "&roomPassword=" + Network.Instance.RoomPassword);
            webRequest.downloadHandler = new DownloadHandlerFile(AbsoluteServerContentDirectory + "multiplayer.csv");

            // Request and wait for the desired page.
            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error fetching source from backend: " + webRequest.error);
            }
            */
        }
    }
}
