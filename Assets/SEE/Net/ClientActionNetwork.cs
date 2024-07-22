using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using SEE.Net.Actions;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Networking;

namespace SEE.Net
{
    /// <summary>
    /// RPCs on the client side.
    /// </summary>
    public class ClientActionNetwork : NetworkBehaviour
    {
        // TODO(#749): This file makes heavy use of coroutines to deal with asynchronous code.
        // We should consider refactoring it to use async via UniTask instead, to make it
        // consistent with the rest of SEE and gain improved efficiency. This would also
        // simplify some code (e.g., in GetAllData).

        /// <summary>
        /// Where files are stored locally on the server side (relative directory).
        /// </summary>
        private const string RelativeServerContentDirectory = "/Multiplayer/";
        /// <summary>
        /// Where files are stored locally on the server side (absolute directory).
        /// </summary>
        private string AbsoluteServerContentDirectory => Application.streamingAssetsPath + RelativeServerContentDirectory;

        /// <summary>
        /// The name of the zip file containing the source code.
        /// </summary>
        private const string zippedSourcesFilename = "src.zip";

        /// <summary>
        /// The name of the GXL file containing the city graph from the backend.
        /// </summary>
        private const string gxlFile = "multiplayer.gxl";

        /// <summary>
        /// The name of the configuration file from the backend.
        /// </summary>
        private const string configFile = "multiplayer.cfg";

        /// <summary>
        /// The name of Microsoft Visual Studio solution file from the backend.
        /// </summary>
        private const string solutionFile = "multiplayer.sln";

        /// <summary>
        /// The name of the CSV file containing the metrics from the backend.
        /// </summary>
        private const string metricFile = "multiplayer.csv";

        /// Collect and preserve the fragments of packages.
        public Dictionary<string, List<Fragment>> fragmentsGatherer = new();

        /// <summary>
        /// Fetches the multiplayer city files from the backend and syncs the current
        /// server state with this client.
        /// </summary>
        public void Start()
        {
            if (!IsServer && !IsHost)
            {
                Network.ServerNetwork.Value?.SyncFilesServerRpc();
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
        /// Executes an action on the client.
        /// </summary>
        [ClientRpc]
        public void ExecuteActionClientRpc(string serializedAction)
        {
            if (IsHost  || IsServer)
            {
                return;
            }
            AbstractNetAction action = ActionSerializer.Deserialize(serializedAction);
            if (action.Requester != NetworkManager.Singleton.LocalClientId)
            {
                action.ExecuteOnClient();
            }
        }

        /// <summary>
        /// Receives the fragments of a packet and performs the broadcast when all fragments of the packet are present.
        /// </summary>
        /// <param name="id">The packet id.</param>
        /// <param name="packetSize">The size of fragments of the packet.</param>
        /// <param name="currentFragment">The current fragment.</param>
        /// <param name="data">The data of the fragment</param>
        [ClientRpc]
        public void BroadcastActionClientRpc(string id, int packetSize, int currentFragment, string data)
        {
            Fragment fragment = new(id, packetSize, currentFragment, data);
            if (fragmentsGatherer.TryGetValue(fragment.PacketID, out List<Fragment> fragments))
            {
                fragments.Add(fragment);
            }
            else
            {
                List<Fragment> frags = new() { fragment };
                fragmentsGatherer.Add(fragment.PacketID, frags);
            }
            if (fragmentsGatherer.TryGetValue(fragment.PacketID, out List<Fragment> f)
                && Fragment.CombineFragments(f) != "")
            {
                BroadcastActClientRpc(fragment.PacketID);
            }
        }

        /// <summary>
        /// Performs the broadcast. First, the serialized string is assembled.
        /// </summary>
        /// <param name="key">The packet id.</param>
        /// <param name="recipients">The recipients of the call.</param>
        [ClientRpc]
        private void BroadcastActClientRpc(string key)
        {
            if (IsHost || IsServer)
            {
                return;
            }
            if (fragmentsGatherer.TryGetValue(key, out List<Fragment> fragments))
            {
                string serializedAction = Fragment.CombineFragments(fragments);
                AbstractNetAction action = ActionSerializer.Deserialize(serializedAction);
                if (action.Requester != NetworkManager.Singleton.LocalClientId)
                {
                    action.ExecuteOnClient();
                }
                fragmentsGatherer.Remove(key);
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
            /*
             * We do not want to delete the server content directory altogether.
             * Who knows what other files are in there that we might need.
            if (Directory.Exists(AbsoluteServerContentDirectory))
            {
                Directory.Delete(AbsoluteServerContentDirectory, true);
                Directory.CreateDirectory(AbsoluteServerContentDirectory);
            }
            */

            // For the time being, we will not download the data.
            // This must be re-enabled once the backend is ready.
            // StartCoroutine(GetAllData());
        }


        /// <summary>
        /// Retrieves all data files from the server. These are: (1) the source
        /// code archived in a ZIP file, the GXL file, the configuration file,
        /// the CSV file, and the solution file.
        /// </summary>
        /// <returns>coroutine enumerator</returns>
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

            Network.ServerNetwork.Value?.SyncClientServerRpc(NetworkManager.Singleton.LocalClientId);
        }

        /// <summary>
        /// Unzips the source-code ZIP archive if it exists.
        /// </summary>
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
        /// Fetches the source-code ZIP file from the backend.
        /// </summary>
        private IEnumerator GetSource()
        {
            return GetFile("source", zippedSourcesFilename);
        }

        /// <summary>
        /// Fetches the GXL file from the backend.
        /// </summary>
        private IEnumerator GetGxl()
        {
            return GetFile("gxl", gxlFile);
        }

        /// <summary>
        /// Fetches the configuration file from the backend.
        /// </summary>
        private IEnumerator GetConfig()
        {
            return GetFile("config", configFile);
        }

        /// <summary>
        /// Fetches the solution file from the backend.
        /// </summary>
        private IEnumerator GetSolution()
        {
            return GetFile("solution", solutionFile);
        }

        /// <summary>
        /// Fetches the CSV file from the backend.
        /// </summary>
        private IEnumerator GetCsv()
        {
            return GetFile("csv", metricFile);
        }

        /// <summary>
        /// Fetches a file from the backend.
        /// </summary>
        /// <param name="dataType">the type of file</param>
        /// <param name="filename">the name of the file</param>
        /// <returns>enumerator to continue the execution</returns>
        private IEnumerator GetFile(string dataType, string filename)
        {
            if (string.IsNullOrEmpty(Network.ServerId))
            {
                throw new Exception("Server ID is not set.");
            }
            // FIXME(#750): The room password is submitted in plain text if http and not https is used.
            string url = Network.ClientRestAPI + dataType + "?serverId=" + Network.ServerId
                         + "&roomPassword=" + Network.Instance.RoomPassword;

            using UnityWebRequest webRequest = UnityWebRequest.Get(url);
            webRequest.downloadHandler = new DownloadHandlerFile(AbsoluteServerContentDirectory + filename);

            // Request and wait for the desired page.
            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Error fetching {filename} from backend: {webRequest.error}.\n");
            }
        }
    }
}
