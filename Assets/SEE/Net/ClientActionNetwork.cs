using System;
using System.Collections;
using System.IO;
using System.IO.Compression;
using Cysharp.Threading.Tasks;
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
        /// <summary>
        /// Where multiplayer files are stored locally relative to the streaming assets.
        /// </summary>
        private const string ServerContentDirectory = "Multiplayer/";

        /// <summary>
        /// The data structure for loggint into the backend.
        /// </summary>
        [System.Serializable]
        private struct LoginData
        {
            public string Username;
            public string Password;

            public LoginData(string username, string password)
            {
                Username = username;
                Password = password;
            }

            public string ToJson()
            {
                return JsonUtility.ToJson(this);
            }
        }

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
            if (IsHost || IsServer)
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
            // GetAllData();
        }


        /// <summary>
        /// Retrieves all data files from the server.
        /// </summary>
        private async UniTask GetAllData()
        {
            Debug.Log($"Backend API URL is: {Network.ClientRestAPI}.\n");

            if (!await LogIn())
            {
                Debug.Log("Login was NOT successful!");
                return;
            }

            // TODO download files to ServerContentDirectory

            // TODO Unzip() ZIP files to ServerContentDirectory

            Network.ServerNetwork.Value?.SyncClientServerRpc(NetworkManager.Singleton.LocalClientId);
        }

        /// <summary>
        /// Unzips a file if it exists.
        /// Throws an <c>IOException</c> if the file does not exist in local storage.
        /// Additionally, the exceptions raised by <c>ZipFile.ExtractToDirectory()</c> are not caught.
        /// </summary>
        /// <param name="zipPath">The path of the ZIP file to be extracted.</param>
        /// <param name="targetPath">The path of the target directory in which the file should be extracted.</param>
        private void Unzip(string zipPath, string targetPath)
        {
            var filePath = System.IO.Path.Combine(Application.streamingAssetsPath, zipPath);
            var dirPath = System.IO.Path.Combine(Application.streamingAssetsPath, targetPath);
            if (!File.Exists(filePath))
            {
                throw new IOException($"The file does not exist: '{filePath}'");
            }

            ZipFile.ExtractToDirectory(targetPath, dirPath);
        }

        /// <summary>
        /// Asynchronously retrieves a file from the backend using the specified file ID and path.
        /// Throws an <c>InvalidOperationException</c> if the server ID is not set and an <c>IOException</c>
        /// if the file already exists in local storage.
        /// </summary>
        /// <param name="id">The unique identifier of the file to be retrieved.</param>
        /// <param name="path">The path and filename of the file to be saved locally after retrieval.</param>
        /// <returns>
        /// A <see cref="UniTask{bool}"/> indicating whether the file retrieval was successful.
        /// Returns <c>true</c> if the file is successfully retrieved and saved; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// On successful retrieval, the file is stored with the specified path relative to the
        /// <c>Application.streamingAssetsPath</c>.
        /// </remarks>
        private async UniTask<bool> GetFile(string id, string path)
        {
            if (string.IsNullOrEmpty(Network.ServerId))
            {
                throw new InvalidOperationException("Server ID is not set.");
            }
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(path))
            {
                Debug.LogWarning("Parameters must not be empty!");
                return false;
            }
            var targetPath = System.IO.Path.Combine(Application.streamingAssetsPath, path);
            if (File.Exists(targetPath))
            {
                throw new IOException($"The file already exists: '{targetPath}'");
            }

            string url = Network.ClientRestAPI + "file/download?id=" + id;
            using (UnityWebRequest getRequest = UnityWebRequest.Get(url))
            {

                getRequest.downloadHandler = new DownloadHandlerFile(targetPath);
                UnityWebRequestAsyncOperation asyncOp = getRequest.SendWebRequest();
                await asyncOp.ToUniTask();
                
                if (getRequest.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError(getRequest.error);
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        /// <summary>
        /// Asynchronously logs in to the backend by sending a POST request to the user/signin endpoint.
        /// If the login is successful, the server responds with a cookie containing a JWT (JSON Web Token)
        /// that is stored in Unity's cookie cache and used for subsequent API calls.
        /// </summary>
        /// <returns>
        /// A <see cref="UniTask{bool}"/> indicating whether the login was successful.
        /// Returns <c>true</c> if the login is successful; otherwise, <c>false</c>.
        /// </returns>
        private async UniTask<bool> LogIn()
        {
            string url = Network.ClientRestAPI + "user/signin";
            string postBody = new LoginData(Network.ServerId, Network.Instance.RoomPassword).ToJson();
            UnityWebRequest.ClearCookieCache(new System.Uri(url));
            using (UnityWebRequest signinRequest = UnityWebRequest.Post(url, postBody, "application/json"))
            {
                UnityWebRequestAsyncOperation asyncOp = signinRequest.SendWebRequest();
                await asyncOp.ToUniTask();

                if (signinRequest.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError(signinRequest.error);
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }
    }
}
