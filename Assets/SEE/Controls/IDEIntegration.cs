using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Cysharp.Threading.Tasks;
using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.Game;
using SEE.Game.City;
using SEE.Game.UI.Notification;
using SEE.GO;
using SEE.Utils;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace SEE.Controls
{
    /// <summary>
    /// This script establishes the connection to an IDE of choice. There is the
    /// option to choose between all possible IDE implementations. Currently,
    /// only Visual Studio is supported, but could be easily extended in the
    /// future.
    /// Note: Only one instance of this class can be created.
    /// </summary>
    public partial class IDEIntegration : MonoBehaviour
    {
        /// <summary>
        /// There is currently only an implementation for Visual Studio.
        /// </summary>
        public enum Ide
        {
            VisualStudio2019, // Establish connection to Visual Studio (2019)
            VisualStudio2022 // Establish connection to Visual Studio (2022)
        };

        /// <summary>
        /// Specifies to which IDE a connection is to be established.
        /// </summary>
        [Header("General")]
        [Tooltip("Specifies to which IDE a connection is to be established.")]
        public Ide Type;

        /// <summary>
        /// Specifies the number of IDEs that can connect at the same time.
        /// </summary>
        [Tooltip("Specifies the number of IDEs that can connect at the same time.")] 
        public uint MaxNumberOfIdes = 1;

        /// <summary>
        /// TCP Socket port that will be used on local host.
        /// </summary>
        [Tooltip("TCP Socket port that will be used on local host.")] 
        public int Port = 26100;

        /// <summary>
        /// Connects to IDE regardless of the loaded project (solution).
        /// </summary>
        [Tooltip("Connects to IDE regardless of the loaded project (solution).")]
        public bool ConnectToAny = false;

        /// <summary>
        /// Will use the position of a member. Disabling may fix some problems.
        /// </summary>
        [Header("Node lookup")]
        [Tooltip("Will use the position of a member. Disabling may fix some problems.")]
        public bool UseElementPosition = false;

        /// <summary>
        /// Will use the range (start to end) of a member.
        /// </summary>
        [Tooltip("Will use the range (start to end) of a member.")] 
        public bool UseElementRange = false;

        /// <summary>
        /// Represents the singleton instance of this integration in the scene.
        /// </summary>
        public static IDEIntegration Instance { get; private set; }

        /// <summary>
        /// All callable methods by the server. Will be executed in every connected IDE.
        /// </summary>
        private IDECalls ideCalls;

        /// <summary>
        /// The JsonRpcServer used for communication between IDE and SEE.
        /// </summary>
        private JsonRpcServer server;

        /// <summary>
        /// Semaphore for accessing <see cref="cachedConnections"/>.
        /// </summary>
        private SemaphoreSlim semaphore;

        /// <summary>
        /// A mapping from the absolute path of the node to another Dictionary, which will map an
        /// element key to a list of GameObjects. Since changes in the city have no impact to the
        /// source code, this will only be initialized during start up.
        /// </summary>
        private IDictionary<string, IDictionary<string, ICollection<GameObject>>> cachedObjects;

        /// <summary>
        /// A mapping of all registered connections to the project they have opened. Only add and
        /// delete elements in this directory while using <see cref="semaphore"/>.
        /// </summary>
        private IDictionary<string, JsonRpcConnection> cachedConnections;

        /// <summary>
        /// Contains all solution path of code cities in this scene.
        /// </summary>
        private HashSet<string> cachedSolutionPaths;

        /// <summary>
        /// Contains all <see cref="InteractableObject"/> that were selected by the selected IDE.
        /// Don't make any changes on this set directly. Instead, a new assignment should be made
        /// when the set changed.
        /// </summary>
        private HashSet<InteractableObject> pendingSelections;

        #region Initialization 

        /// <summary>
        /// Initializes all necessary objects for the inter-process communication
        /// between SEE and the selected IDE.
        /// </summary>
        public void Start()
        {
            // Checks current operating system.
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
#if UNITY_EDITOR
                Debug.LogWarning("Currently only supported on Windows!");
#endif
                return;
            }

            if (Instance != null)
            {
#if UNITY_EDITOR
                Debug.LogError($"Only one instance of '{this}' can be initiated!");
#endif
                Destroy(this);
                return;
            }

            Instance = this;
            semaphore = new SemaphoreSlim(1, 1);
            pendingSelections = new HashSet<InteractableObject>();
            cachedConnections = new Dictionary<string, JsonRpcConnection>();

            InitializeSceneElementsObjects();

            server = new JsonRpcSocketServer(null, Port);
            ideCalls = new IDECalls(server);

            server.Connected += ConnectedToClient;
            server.Disconnected += DisconnectedFromClient;

            // Starting the server as a background task.
            StartServer().Forget();

            async UniTaskVoid StartServer()
            {
                try
                {
                    await server.Start(MaxNumberOfIdes);
                }
                catch (JsonRpcServer.JsonRpcServerCreationFailedException e)
                {
                    ShowNotification.Error("IDE Integration", e.Message);
                }
            }
        }

        /// <summary>
        /// Stops the currently running server and deletes the singleton instance of
        /// <see cref="IDEIntegration"/>.
        /// </summary>
        public void OnDestroy()
        {
            // To prevent show notification while destroying.
            server.Connected -= ConnectedToClient;
            server.Disconnected -= DisconnectedFromClient;

            server?.Dispose();
            semaphore?.Dispose();
            Instance = null;
        }

        /// <summary>
        /// Will get every node using <see cref="SceneQueries.AllGameNodesInScene"/> and store
        /// them in <see cref="cachedObjects"/>. Additionally will look for all solution paths.
        /// </summary>
        private void InitializeSceneElementsObjects()
        {
            // A GameObject should be unique, but the key generated by SourceLength may be used repeated
            cachedObjects = new Dictionary<string, IDictionary<string, ICollection<GameObject>>>();
            cachedSolutionPaths = new HashSet<string>();

            // Get all nodes in scene and cache them in cachedObjects
            foreach (GameObject node in SceneQueries.AllGameNodesInScene(true, true))
            {
                if (TryGenerateNodeKey(node.GetNode(), out string path, out string key))
                {
                    if (!cachedObjects.ContainsKey(path))
                    {
                        cachedObjects[path] = new Dictionary<string, ICollection<GameObject>>();
                    }

                    if (!cachedObjects[path].ContainsKey(key))
                    {
                        cachedObjects[path][key] = new List<GameObject>();
                    }
                    cachedObjects[path][key].Add(node);
                }
            }

            // Get all code cities
            foreach (GameObject obj in GameObject.FindGameObjectsWithTag(Tags.CodeCity))
            {
                if (obj.TryGetComponent(out AbstractSEECity city))
                {
                    cachedSolutionPaths.Add(city.SolutionPath.Path);
                }
            }
        }

        /// <summary>
        /// Generates an appropriate key for a given node.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <param name="path">The generated path to the given node. Can be null.</param>
        /// <param name="key">The generated key to the given node. Can be null.</param>
        /// <returns>Generating was successful.</returns>
        private bool TryGenerateNodeKey(Node node, out string path, out string key)
        {
            path = null;
            try
            {
                path = Path.GetFullPath(node.Path() + node.Filename());
            }
            catch (Exception)
            {
                // File not found
            }

            key = GenerateKey(node.SourceName, node.SourceLine().GetValueOrDefault(), 
                node.SourceColumn().GetValueOrDefault(), node.SourceLength().GetValueOrDefault());
            return path != null && key != null;
        }

        /// <summary>
        /// Will generate a key from the given parameter to be used for <see cref="cachedObjects"/>.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <param name="line">The line number of the element.</param>
        /// <param name="column">The column number of the element.</param>
        /// <param name="length">The length of the code range.</param>
        /// <returns>A key for <see cref="cachedObjects"/>.</returns>
        private string GenerateKey(string name, int line, int column, int length)
        {
            if (name == null) return "";
            string key = UseElementRange || UseElementPosition ? $"{name}:{line}" : name;
            key = UseElementRange ? $"{key}:{length}" : key;
            key = UseElementPosition ? $"{key}:{column}" : key;

            return key;
        }

        #endregion

        /// <summary>
        /// Is there any pending selection needed to be taken.
        /// </summary>
        /// <returns>True if <see cref="SEEInput.SelectionEnabled"/> and there is a selection.</returns>
        public bool PendingSelectionsAction()
        {
            return SEEInput.SelectionEnabled && pendingSelections.Count > 0;
        }

        /// <summary>
        /// Returns a set of <see cref="InteractableObject"/> that represents the elements the IDE
        /// wants to be highlighted. After calling this method, the underlying set will be cleared
        /// and thus is empty again.
        /// </summary>
        /// <returns>The set of elements to be highlighted.</returns>
        public HashSet<InteractableObject> PopPendingSelections()
        {
            HashSet<InteractableObject> elements = new HashSet<InteractableObject>();
            elements.UnionWith(pendingSelections);
            pendingSelections.Clear();
            return elements;
        }

        /// <summary>
        /// Opens the specified file in an IDE.
        /// </summary>
        /// <param name="filePath">The absolute file path.</param>
        /// <param name="solutionPath">The absolute solution path of this file.</param>
        /// <param name="line">Optional line number.</param>
        /// <returns>Async UniTask.</returns>
        public async UniTask OpenFile(string filePath, string solutionPath, int? line = null)
        {
            JsonRpcConnection connection = await LookForIDEConnection(solutionPath);
            if (connection == null) return;
            try
            {
                await ideCalls.OpenFile(connection, Path.GetFullPath(filePath), line);
            }
            catch (Exception)
            {
                // File not found!
            }
        }

        #region IDE Management

        /// <summary>
        /// Is looking for any active IDE. If no instance is found, will open a new IDE
        /// instance. Also focusing the IDE.
        /// </summary>
        /// <param name="solutionPath">The solution path.</param>
        /// <returns>Will return the <see cref="JsonRpcConnection"/> to a given solution path.
        /// Null if not found.</returns>
        private async UniTask<JsonRpcConnection> LookForIDEConnection(string solutionPath)
        {
            if (solutionPath == null) return null;

            JsonRpcConnection connection = null;
            try
            {
                connection = cachedConnections[solutionPath];
            }
            catch (Exception)
            {
                if (MaxNumberOfIdes == cachedConnections.Count && MaxNumberOfIdes != 0)
                {
                    await semaphore.WaitAsync();
                    connection = cachedConnections.First().Value;
                    semaphore.Release();
                    await ideCalls.ChangeSolution(connection, solutionPath);
                }
                else if (MaxNumberOfIdes != 0)
                {
                    connection = await OpenNewIDEInstanceAsync(solutionPath);
                }
                else
                {
                    return connection;
                }
            }

            await ideCalls.FocusIDE(connection);

            return connection;
        }

        /// <summary>
        /// Checks if the IDE is the right <see cref="Type"/> and contains a project that is represented
        /// by a graph. If <see cref="ConnectToAny"/> is true, it will skip the project check up.
        /// </summary>
        /// <param name="connection">The IDE connection.</param>
        /// <returns>True if IDE was accepted, false otherwise.</returns>
        private async UniTask<bool> CheckIDE(JsonRpcConnection connection)
        {
            // Right version of the IDE
            string version = await ideCalls.GetIDEVersion(connection);
            if (version == null || !version.Equals(Type.ToString())) return false;
            
            return await ideCalls.WasStartedBySee(connection) || ConnectToAny || 
                   cachedSolutionPaths.Contains(await ideCalls.GetProjectPath(connection));
        }

        /// <summary>
        /// Opens the IDE defined in <see cref="Type"/> and returns the new connection.
        /// </summary>
        /// <param name="solutionPath">The solution path.</param>
        /// <returns>Established connection. Can be null if connection couldn't be established</returns>
        private async UniTask<JsonRpcConnection> OpenNewIDEInstanceAsync(string solutionPath)
        {
            string arguments;
            string fileName;

            switch (Type)
            {
                case Ide.VisualStudio2019:
                    fileName = await VSPathFinder.GetVisualStudioExecutableAsync(VSPathFinder.Version.VS2019);
                    arguments = $"\"{solutionPath}\" /VsSeeExtension";
                    break;
                case Ide.VisualStudio2022:
                    fileName = await VSPathFinder.GetVisualStudioExecutableAsync(VSPathFinder.Version.VS2022);
                    arguments = $"\"{solutionPath}\" /VsSeeExtension";
                    break;
                default:
                    throw new NotImplementedException($"Implementation of case {Type} not found");
            }
            ProcessStartInfo start = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
            };

            try
            {
                using Process proc = Process.Start(start);
            }
            catch (Exception e)
            {
#if UNITY_EDITOR
                Debug.LogError(e);
#endif
                throw;
            }
            JsonRpcConnection connection = null;

            // Time out after 3 minutes without connecting.
            await UniTask.WhenAny(LookUpConnection(), UniTask.Delay(180000));

            return connection;

            async UniTask LookUpConnection()
            {
                while (true)
                {
                    try
                    {
                        connection = cachedConnections[solutionPath];
                        return;
                    }
                    catch (Exception)
                    {
                        await UniTask.Delay(200);
                    }
                }
            }
        }

        /// <summary>
        /// Will be called when connection to client is established successful. And checks whether
        /// the client contains the right project. A connection will be added to
        /// <see cref="cachedConnections"/> when everything was successful.
        /// </summary>
        /// <param name="connection">The connection.</param>
        private void ConnectedToClient(JsonRpcConnection connection)
        {
            UniTask.Run(async () =>
            {
                if (await CheckIDE(connection))
                {
                    await semaphore.WaitAsync();

                    string project = await ideCalls.GetProjectPath(connection);
                    connection.AddTarget(new RemoteProcedureCalls(this, project));

                    if (project == null) return;

                    if (!cachedConnections.ContainsKey(project))
                    {
                        cachedConnections[project] = connection;
                    }

                    semaphore.Release();

                    await UniTask.SwitchToMainThread();
                    ShowNotification.Info("Connected to IDE",
                        "Connection to IDE established.", 5.0f);
                }
                else
                {
                    await ideCalls.Decline(connection);
                }
            }).Forget();
        }

        /// <summary>
        /// Will be called when the client disconnected form the server and removes the connection
        /// from <see cref="cachedConnections"/>.
        /// </summary>
        /// <param name="connection">The connection.</param>
        private void DisconnectedFromClient(JsonRpcConnection connection)
        {
            UniTask.Run(async () =>
            {
                await semaphore.WaitAsync();

                string key = cachedConnections.FirstOrDefault(x => x.Value.Equals(connection)).Key;
                if (key != null)
                {
                    cachedConnections.Remove(key);
                    await UniTask.SwitchToMainThread();
                    ShowNotification.Info("Disconnected from IDE",
                        "The IDE was disconnected form SEE.", 5.0f);
                }
                semaphore.Release();
            }).Forget();
        }

        #endregion
    }
}