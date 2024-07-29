using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Cysharp.Threading.Tasks;
using SEE.Net.Actions;
using SEE.Game.City;
using SEE.Utils.Paths;
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
        /// This is a dedicated directory where only files are stored that are downloaded from the backend.
        /// </summary>
        private const string ServerContentDirectory = "Multiplayer/";

        /// <summary>
        /// The data structure for logging into the backend.
        /// </summary>
        [System.Serializable]
        private struct LoginData
        {
            public string username;
            public string password;

            public LoginData(string username, string password)
            {
                this.username = username;
                this.password = password;
            }

            public override string ToString()
            {
                return JsonUtility.ToJson(this);
            }

            public static implicit operator string(LoginData loginData)
            {
                return loginData.ToString();
            }
        }

        /// <summary>
        /// The data structure for file metadata from the backend.
        /// </summary>
        [System.Serializable]
        private struct FileData
        {
            public string id;
            public string name;
            public string contentType;
            public string fileType;
            public long size;
            public long creationTime;

            public static FileData FromJson(string json)
            {
                return JsonUtility.FromJson<FileData>(json);
            }

            public override string ToString()
            {
                return JsonUtility.ToJson(this);
            }

            public static implicit operator string(FileData fileData)
            {
                return fileData.ToString();
            }

            public static implicit operator FileData(string json)
            {
                return FromJson(json);
            }
        }

        /// <summary>
        /// A list of <c>FileData</c> for deserialization of server responses.
        /// </summary>
        [System.Serializable]
        private class FileDataList
        {
            public List<FileData> items;

            public static FileDataList FromJson(string json)
            {
                return JsonUtility.FromJson<FileDataList>("{ \"items\": " + json + "}");
            }

            public override string ToString()
            {
                return items.ToString();
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
                Debug.Log("Starting client action network!");
                Network.ServerNetwork.Value?.SyncFilesServerRpc();
            }
            else {
                LoadCityAsync().Forget();
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
            
            // This should be safe to clear as files are downloaded from the backend each time SEE starts.
            string multiplayerDataPath = System.IO.Path.Combine(Application.streamingAssetsPath, ServerContentDirectory);
            if (Directory.Exists(multiplayerDataPath))
            {
                Directory.Delete(multiplayerDataPath, true);
                Directory.CreateDirectory(multiplayerDataPath);
            }

            DownloadAllFilesAsync().Forget();
        }


        /// <summary>
        /// Retrieves all multiplayer files from the backend server.
        /// </summary>
        private async UniTask DownloadAllFilesAsync()
        {
            Debug.Log($"Backend API URL is: {Network.ClientRestAPI}");

            if (!await LogInAsync())
            {
                Debug.LogError("Unable to download files!");
                return;
            }

            List<FileData> files = await GetFilesAsync(Network.ServerId);
            Debug.Log($"Downloading {files.Count} files to: {System.IO.Path.Combine(Application.streamingAssetsPath, ServerContentDirectory)}");
            foreach (FileData file in files)
            {
                try
                {
                    Debug.Log($"Downloading file: {file.name}");
                    string localFileName = System.IO.Path.Combine(ServerContentDirectory, file.name);
                    bool success = await DownloadFileAsync(file.id, localFileName);
                    if (success && file.contentType.ToLower() == "application/zip")
                    {
                        Debug.Log($"Extracting ZIP file: {file.name}");
                        try
                        {
                            Unzip(localFileName, ServerContentDirectory);
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"Error unzipping file: {file.name}");
                            Debug.LogError(e);
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error downloading file: {file.name}");
                    Debug.LogError(e);
                }
            }
            Debug.Log("Done downloading!");

            Network.ServerNetwork.Value?.SyncClientServerRpc(NetworkManager.Singleton.LocalClientId);
            await LoadCityAsync();
        }

        /// <summary>
        /// Loads the <c>SEECity</c> in the scene.
        /// </summary>
        private async UniTask LoadCityAsync()
        {
            var codeCity = GameObject.Find("ImplementationTable/CodeCity");
            var seeCity = codeCity.GetComponent<SEECity>() as SEECity;
            seeCity.Reset();

            string configPath = GetCfg(ServerContentDirectory);
            if (string.IsNullOrWhiteSpace(configPath))
            {
                Debug.Log("No SEECity configuration found in multiplayer data.");
                return;
            }

            Debug.Log($"Loading SEECity configuration from multiplayer data: {configPath}");
            seeCity.ConfigurationPath = new DataPath(configPath);
            seeCity.LoadConfiguration();
            await seeCity.LoadDataAsync();
            seeCity.DrawGraph();
        }

        /// <summary>
        /// Scans the directory with the given path in the streaming assets for a <c>.cfg</c> file.
        /// </summary>
        /// <returns>
        /// The path of the first <c>.cfg</c> file that is found, or <c>null</c> if none was found.
        /// </returns>
        private string GetCfg(string dir)
        {
            string dirPath = System.IO.Path.Combine(Application.streamingAssetsPath, dir);
            var filePaths = Directory.EnumerateFiles(dirPath, "*.cfg", SearchOption.TopDirectoryOnly);
            foreach (string filePath in filePaths)
            {
                return filePath;
            }
            return null;
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

            ZipFile.ExtractToDirectory(filePath, dirPath);
        }

        /// <summary>
        /// Asynchronously retrieves a file from the backend using the specified file ID and path.
        /// Throws an <c>IOException</c> if the file already exists in local storage.
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
        private async UniTask<bool> DownloadFileAsync(string id, string path)
        {
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
        private async UniTask<bool> LogInAsync()
        {
            string url = Network.ClientRestAPI + "user/signin";
            string postBody = new LoginData(Network.ServerId, Network.Instance.RoomPassword);
            UnityWebRequest.ClearCookieCache(new System.Uri(url));
            using (UnityWebRequest signinRequest = UnityWebRequest.Post(url, postBody, "application/json"))
            {
                UnityWebRequestAsyncOperation asyncOp = signinRequest.SendWebRequest();
                await asyncOp.ToUniTask();

                if (signinRequest.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError("Login to the backend was NOT successful!");
                    Debug.LogError(signinRequest.error);
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        /// <summary>
        /// Fetches the metadata for all files associated to the server ID.
        /// </summary>
        /// <param name="serverId">the server ID</param>
        /// <returns>A list of file metadata objects if the request was successful, or <c>null</c> if not.</returns>
        private async UniTask<List<FileData>> GetFilesAsync(string serverId)
        {
            string url = Network.ClientRestAPI + "server/files?id=" + serverId;
            using (UnityWebRequest fetchRequest = UnityWebRequest.Get(url))
            {
                UnityWebRequestAsyncOperation operation = fetchRequest.SendWebRequest();
                await operation.ToUniTask();

                if (fetchRequest.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning("Fetching files for server failed!");
                    Debug.Log(fetchRequest.error);
                    return null;
                }
                else
                {
                    return FileDataList.FromJson(fetchRequest.downloadHandler.text).items;
                }
            }
        }

    }
}
