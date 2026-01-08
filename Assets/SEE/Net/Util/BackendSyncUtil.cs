using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using SEE.Game.City;
using SEE.User;
using SEE.Utils;
using SEE.Utils.Paths;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Networking;

namespace SEE.Net.Util
{
    /// <summary>
    /// Utility to synchronize files from the SEE backend.
    /// </summary>
    internal class BackendSyncUtil
    {
        /// <summary>
        /// Associates the Code City types with the paths to find the matching game objects.
        /// </summary>
        private static readonly Dictionary<string, string> cities = new()
        {
            {"SEECity",          "ImplementationTable/CodeCity"},
            {"DiffCity",         "DiffTable/DiffCity"},
            {"SEECityEvolution", "EvolutionTable/EvolutionCity"},
            {"SEEJlgCity",       "DynamicTable/DynamicCity"},
            {"SEEReflexionCity", "ArchitectureTable/ReflexionCity"},
        };

        /// <summary>
        /// Where multiplayer files are stored locally relative to the streaming assets.
        /// This is a dedicated directory where only files are stored that are downloaded from the backend.
        /// </summary>
        private const string serverContentDirectory = "Multiplayer/";

        /// <summary>
        /// The data structure for logging into the backend.
        /// </summary>
        [System.Serializable]
        private struct LoginData
        {
            /// <summary>
            /// Name of the user.
            /// </summary>
            [JsonProperty(PropertyName = "username", Required = Required.Always)]
            public string Username;
            /// <summary>
            /// Password of the user
            /// </summary>
            [JsonProperty(PropertyName = "password", Required = Required.Always)]
            public string Password;

            public LoginData(string username, string password)
            {
                Username = username;
                Password = password;
            }

            public readonly override string ToString()
            {
                return JsonConvert.SerializeObject(this);
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
            [JsonProperty(PropertyName = "id", Required = Required.Always)]
            public string Id;
            [JsonProperty(PropertyName = "name", Required = Required.Always)]
            public string Name;
            [JsonProperty(PropertyName = "contentType", Required = Required.Always)]
            public string ContentType;
            [JsonProperty(PropertyName = "projectType", Required = Required.Always)]
            public string ProjectType;
            [JsonProperty(PropertyName = "size")]
            public long Size;
            [JsonProperty(PropertyName = "creationTime")]
            public DateTime CreationTime;

            public static FileData FromJson(string json)
            {
                return JsonConvert.DeserializeObject<FileData>(json);
            }

            public readonly override string ToString()
            {
                return JsonConvert.SerializeObject(this);
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
        /// Initializes the client by downloading files, synchronizing with the server and instantiating Code Cities.
        /// </summary>
        internal static async UniTask InitializeClientAsync()
        {
            await InitializeCitiesAsync();
            Network.ActionNetworkInst.Value?.SyncClientServerRpc(NetworkManager.Singleton.LocalClientId);
        }

        /// <summary>
        /// Builds a zip file, which contains all snapshot files from <paramref name="snapshot"/>.
        /// </summary>
        /// <param name="snapshot">The snapshot to create the zip file from.</param>
        /// <returns>The path to the newly created zip file.</returns>
        private static string BuildSnapshotZip(SEECitySnapshot snapshot)
        {
            string snapshotsDir = Path.Combine(Path.GetTempPath(), "see-snapshot-" + Path.GetRandomFileName());
            Directory.CreateDirectory(snapshotsDir);

            Debug.Log($"Snapshot City name: {snapshot.CityName}, ConfigPath: {snapshot.ConfigPath}, GraphPath: {snapshot.GraphPath}, LayoutPath: {snapshot.LayoutPath}");

            string cfgPath = CopyToDir(snapshot.ConfigPath, snapshotsDir);
            string graphPath = CopyToDir(snapshot.GraphPath, snapshotsDir);
            string layoutPath = CopyToDir(snapshot.LayoutPath, snapshotsDir);

            RenameFile(cfgPath, "Configuration");
            RenameFile(graphPath, "Graph");
            RenameFile(layoutPath, "Layout");

            if (snapshot is SEEReflexionCitySnapshot reflexionCitySnapshot)
            {
                // Copy additional files if snapshot is a SEEReflexionCitySnapshot
                string archPath = CopyToDir(reflexionCitySnapshot.ArchitectureGraphPath, snapshotsDir);
                string mappingPath = CopyToDir(reflexionCitySnapshot.MappingGraphPath, snapshotsDir);

                RenameFile(archPath, "Architecture");
                RenameFile(mappingPath, "Mapping");
                RenameFile(layoutPath, "Layout");
            }

            string snapshotZipPath = snapshotsDir + ".zip";
            Archiver.CreateArchive(snapshotsDir, snapshotZipPath);
            // Clear up zip directory
            Directory.Delete(snapshotsDir, true);

            return snapshotZipPath;

            string CopyToDir(string file, string targetDir)
            {
                string newFilePath = Path.Combine(targetDir, Path.GetFileName(file));
                File.Copy(file, newFilePath);
                return newFilePath;
            }

            void RenameFile(string filePath, string newFileName)
            {
                string extensions = String.Join("", Path.GetFileName(filePath)
                .Split('.')
                .Skip(1)
                .Select(x => "." + x));

                File.Move(filePath, Path.Combine(Path.GetDirectoryName(filePath), newFileName) + extensions);
            }

        }

        /// <summary>
        /// Compresses and saves a <see cref="SEECitySnapshot"/> to the backend.
        /// </summary>
        /// <param name="snapshot">The snapshot to save.</param>
        /// <returns>An empty task.</returns>
        public static async UniTask SaveSnapshotsAsync(SEECitySnapshot snapshot)
        {
            Logger.Log("Try saving snapshot to backend");

            string snapshotZipPath = BuildSnapshotZip(snapshot);

            if (!await LogInAsync())
            {
                Debug.LogError("Unable to save snapshot");
                return;
            }

            string url = UserSettings.BackendServerAPI + "server/snapshots?serverId=" + Network.ServerId + "&project_type=" + snapshot.CityName;
            var bytes = File.ReadAllBytes(snapshotZipPath);

            using UnityWebRequest request = new UnityWebRequest(url, "POST");

            request.uploadHandler = new UploadHandlerRaw(bytes);
            request.uploadHandler.contentType = "application/octet-stream";
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/octet-stream");
            request.SetRequestHeader("X-Filename", Path.GetFileName(snapshotZipPath));
            await request.SendWebRequest().ToUniTask();
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Failed to upload snapshot: {request.error}");
            }
            else
            {
                Debug.Log("Snapshot uploaded successfully.");
                // Clean up old zip file
                File.Delete(snapshotZipPath);
            }


        }

        /// <summary>
        /// Downloads the multiplayer files and instantiates Code Cities.
        /// </summary>
        internal static async UniTask InitializeCitiesAsync()
        {
            if (!string.IsNullOrWhiteSpace(Network.ServerId) && !string.IsNullOrWhiteSpace(UserSettings.BackendDomain))
            {
                ClearMultiplayerData();
                await DownloadAllFilesAsync();
            }
            Debug.Log("Initializing Multiplayer Cities...\n");
            await LoadCitiesAsync();
        }

        /// <summary>
        /// Deletes the multiplayer files and recreates the directory to prepare for downlaod.
        /// </summary>
        private static void ClearMultiplayerData()
        {
            // This should be safe to clear as files are downloaded from the backend each time SEE starts.
            string multiplayerDataPath = Path.Combine(Application.streamingAssetsPath, serverContentDirectory);
            if (Directory.Exists(multiplayerDataPath))
            {
                Directory.Delete(multiplayerDataPath, true);
            }
            Directory.CreateDirectory(multiplayerDataPath);
        }

        /// <summary>
        /// Retrieves all multiplayer files from the backend server.
        /// </summary>
        private static async UniTask DownloadAllFilesAsync()
        {
            Debug.Log($"Backend API URL is: {UserSettings.BackendServerAPI}.\n");

            if (!await LogInAsync())
            {
                Debug.LogError("Unable to download files!\n");
                return;
            }

            List<FileData> files = await GetFilesAsync(Network.ServerId);
            Debug.Log($"Downloading {files.Count} files to: {Path.Combine(Application.streamingAssetsPath, serverContentDirectory)}.\n");
            foreach (FileData file in files)
            {
                try
                {
                    Debug.Log($"Downloading file: {file.Name}");
                    string localFileName = Path.Combine(serverContentDirectory, file.Name);
                    bool success = await DownloadFileAsync(file.Id, localFileName);
                    if (success && file.ContentType.ToLower() == "application/zip")
                    {
                        Debug.Log($"Extracting ZIP file: {file.Name}.\n");
                        try
                        {
                            Unzip(localFileName, Path.Combine(serverContentDirectory, file.ProjectType), true);
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"Error unzipping file: {file.Name}");
                            Debug.LogError(e + "\n");
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error downloading file: {file.Name}.\n");
                    Debug.LogError(e + "\n");
                }
            }
            Debug.Log("Done downloading!\n");
        }

        /// <summary>
        /// Unzips a file if it exists.
        /// Throws an <c>IOException</c> if the file does not exist in local storage.
        /// Additionally, the exceptions raised by <c>ZipFile.ExtractToDirectory()</c> are not caught.
        /// </summary>
        /// <param name="relativeZipPath">The path of the ZIP file to be extracted relative to the streaming assets.</param>
        /// <param name="relativeTtargetPath">The path of the target directory in which the file should be extracted relative to the streaming assets.</param>
        /// <param name="stripSingleRootDir">Determines if a single root directory is stripped during extraction (if present).</param>
        private static void Unzip(string relativeZipPath, string relativeTtargetPath, bool stripSingleRootDir = false)
        {
            string zipPath = Path.Combine(Application.streamingAssetsPath, relativeZipPath);
            string targetPath = Path.Combine(Application.streamingAssetsPath, relativeTtargetPath);
            if (!File.Exists(zipPath))
            {
                throw new IOException($"The file does not exist: '{zipPath}'");
            }

            string now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
            string tempTargetPath = Path.Combine(Application.streamingAssetsPath, serverContentDirectory, "tmp_" + now);
            ZipFile.ExtractToDirectory(zipPath, tempTargetPath);

            string[] dirs = Directory.GetDirectories(tempTargetPath);
            bool doStripSingleRootDir = stripSingleRootDir && Directory.GetFiles(tempTargetPath).Length == 0 && dirs.Length == 1;
            if (doStripSingleRootDir)
            {
                Directory.Move(dirs[0], targetPath);
                Directory.Delete(tempTargetPath, true);
            }
            else
            {
                Directory.Move(tempTargetPath, targetPath);
            }
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
        private static async UniTask<bool> DownloadFileAsync(string id, string path)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(path))
            {
                Debug.LogWarning("Parameters must not be empty!\n");
                return false;
            }
            string targetPath = Path.Combine(Application.streamingAssetsPath, path);
            if (File.Exists(targetPath))
            {
                throw new IOException($"The file already exists: '{targetPath}'");
            }

            string url = UserSettings.BackendServerAPI + "file/download?id=" + id;
            using UnityWebRequest getRequest = UnityWebRequest.Get(url);
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

        /// <summary>
        /// Asynchronously logs into the backend by sending a POST request to the user/signin endpoint.
        /// If the login is successful, the server responds with a cookie containing a JWT (JSON Web Token)
        /// that is stored in Unity's cookie cache and used for subsequent API calls.
        /// </summary>
        /// <returns>
        /// A <see cref="UniTask{bool}"/> indicating whether the login was successful.
        /// Returns <c>true</c> if the login is successful; otherwise, <c>false</c>.
        /// </returns>
        private static async UniTask<bool> LogInAsync()
        {
            string url = UserSettings.BackendServerAPI + "user/signin";
            string postBody = new LoginData(Network.ServerId, User.UserSettings.Instance.Network.RoomPassword);
            UnityWebRequest.ClearCookieCache(new Uri(url));
            using UnityWebRequest signinRequest = UnityWebRequest.Post(url, postBody, "application/json");
            UnityWebRequestAsyncOperation asyncOp = signinRequest.SendWebRequest();
            await asyncOp.ToUniTask();

            if (signinRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Login to the backend was NOT successful!\n");
                Debug.LogError(signinRequest.error);
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Fetches the metadata for all files associated to the server ID.
        /// </summary>
        /// <param name="serverId">The server ID.</param>
        /// <returns>A list of file metadata objects if the request was successful, or <c>null</c> if not.</returns>
        private static async UniTask<List<FileData>> GetFilesAsync(string serverId)
        {
            string url = UserSettings.BackendServerAPI + "server/files?id=" + serverId;
            using UnityWebRequest fetchRequest = UnityWebRequest.Get(url);
            UnityWebRequestAsyncOperation operation = fetchRequest.SendWebRequest();
            await operation.ToUniTask();

            if (fetchRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning("Fetching files for server failed!\n");
                Debug.Log(fetchRequest.error);
                return null;
            }
            else
            {
                return JsonConvert.DeserializeObject<List<FileData>>(fetchRequest.downloadHandler.text);
            }
        }

        /// <summary>
        /// Loads all available Code Cities in the scene.
        /// </summary>
        private static async UniTask LoadCitiesAsync()
        {
            foreach (string city in cities.Keys)
            {
                string path = Path.Combine(Application.streamingAssetsPath, serverContentDirectory, city);
                if (Directory.Exists(path))
                {
                    Debug.Log($"Found {city}...\n");
                    await LoadCityAsync(path, cities[city]);
                }
            }
        }

        /// <summary>
        /// Loads a Code City in the scene.
        /// </summary>
        private static async UniTask LoadCityAsync(string dirPath, string gameObjectPath)
        {
            GameObject codeCity = GameObject.Find(gameObjectPath);
            SEECity seeCity = codeCity.GetComponent<SEECity>() as SEECity;
            seeCity.Reset();

            string configPath = GetCfg(dirPath);
            if (string.IsNullOrWhiteSpace(configPath))
            {
                Debug.Log($"No SEECity configuration found in: {dirPath}\n");
                return;
            }

            Debug.Log($"Loading SEECity configuration from: {configPath}\n");
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
        private static string GetCfg(string dir)
        {
            string dirPath = Path.Combine(Application.streamingAssetsPath, dir);
            foreach (string filePath in Directory.EnumerateFiles(dirPath, "*.cfg", SearchOption.TopDirectoryOnly))
            {
                return filePath;
            }
            return null;
        }
    }
}
