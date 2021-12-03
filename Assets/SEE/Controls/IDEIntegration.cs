using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Cysharp.Threading.Tasks;
using SEE.DataModel.DG;
using SEE.Game;
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
    public class IDEIntegration : MonoBehaviour
    {
        #region IDE Calls

        /// <summary>
        /// Lists all methods remotely callable by the server in a convenient way.
        /// </summary>
        private class IDECalls
        {
            private readonly JsonRpcServer server;

            /// <summary>
            /// Provides all method SEE can invoke on an IDE.
            /// </summary>
            /// <param name="server">The server instance.</param>
            public IDECalls(JsonRpcServer server)
            {
                this.server = server;
            }

            /// <summary>
            /// Opens the file in the IDE of choice.
            /// </summary>
            /// <param name="connection">A connection to an IDE.</param>
            /// <param name="path">Absolute file path.</param>
            public async UniTask OpenFile(JsonRpcClientConnection connection, string path, int? line)
            {
                await server.CallRemoteProcessOnConnectionAsync(connection, "OpenFile", path, line);
            }

            /// <summary>
            /// Gets the absolute project path (e.g. .sln). 
            /// </summary>
            /// <param name="connection">A connection to an IDE.</param>
            /// <returns>Returns the absolute project path. Can be null.</returns>
            public async UniTask<string> GetProjectPath(JsonRpcClientConnection connection)
            {
                return await server.CallRemoteProcessOnConnectionAsync<string>(connection, "GetProject");
            }

            /// <summary>
            /// Gets the absolute project path (e.g. .sln). 
            /// </summary>
            /// <param name="connection">A connection to an IDE.</param>
            /// <returns>Returns the absolute project path. Can be null.</returns>
            public async UniTask<string> GetIDEVersion(JsonRpcClientConnection connection)
            {
                return await server.CallRemoteProcessOnConnectionAsync<string>(connection, "GetIdeVersion");
            }

            /// <summary>
            /// Was the connection started by SEE directly through a command switch.
            /// </summary>
            /// <param name="connection">A connection to an IDE.</param>
            /// <returns>True if SEE started this connection.</returns>
            public async UniTask<bool> WasStartedBySee(JsonRpcClientConnection connection)
            {
                return await server.CallRemoteProcessOnConnectionAsync<bool>(connection, "WasStartedBySee");
            }

            /// <summary>
            /// Will focus this IDE instance.
            /// </summary>
            public async UniTask FocusIDE(JsonRpcClientConnection connection)
            {
                await server.CallRemoteProcessOnConnectionAsync(connection, "SetFocus");
            }

            /// <summary>
            /// Declines an IDE instance.
            /// </summary>
            /// <param name="connection">A connection to an IDE.</param>
            public async UniTask Decline(JsonRpcClientConnection connection)
            {
                await server.CallRemoteProcessOnConnectionAsync(connection, "Decline");
            }
        }

        #endregion

        #region Remote Procedure Calls

        /// <summary>
        /// This class contains all functions, that can be called by the client (IDE).
        /// </summary>
        private class RemoteProcedureCalls
        {
            /// <summary>
            /// instance of parent class.
            /// </summary>
            private readonly IDEIntegration ideIntegration;

            /// <summary>
            /// Nested class in <see cref="IDEIntegration"/>. Contains all methods that can be accessed
            /// by the client. Should only be initiated by the <see cref="IDEIntegration"/>.
            /// </summary>
            /// <param name="ideIntegration">instance of IDEIntegration</param>
            public RemoteProcedureCalls(IDEIntegration ideIntegration)
            {
                this.ideIntegration = ideIntegration;
            }

            /// <summary>
            /// Adds all nodes from <see cref="cachedObjects"/> with the key created by
            /// <paramref name="path"/> and <paramref name="name"/>. If <paramref name="name"/> is
            /// null, it won't be appended to the key.
            /// </summary>
            /// <param name="path">The absolute path to the source file.</param>
            /// <param name="name">Optional name of the element in a file.</param>
            public void HighlightNode(string path, string name = null)
            {
                var tmp = path;

                if (name != null)
                {
                    tmp = $"{path}:{name}";
                }

                try
                {
                    var nodes = ideIntegration.cachedObjects[tmp];
                    ideIntegration.pendingSelections = new HashSet<InteractableObject>(nodes);
                }
                catch (Exception)
                {
                    // The given key was not presented int the dictionary.
                }
            }
        }

        #endregion

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
        [Tooltip("Specifies to which IDE a connection is to be established.")]
        public Ide Type;

        /// <summary>
        /// Connects to IDE regardless of the loaded project (solution).
        /// </summary>
        [Tooltip("Connects to IDE regardless of the loaded project (solution).")]
        public bool ConnectToAny = false;

        /// <summary>
        /// Fixes potential problem with lines/columns if they do not match to those of the IDE.
        /// </summary>
        [Tooltip("Ignore the given line and column values of a node.")]
        public bool IgnorePosition = false;

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
        /// Will be raised when <see cref="CheckIDE"/> is used. Only purpose is to wait in
        /// <see cref="OpenNewIDEInstanceAsync"/> until connection happened.
        /// </summary>
        private SemaphoreSlim connectionSignal;

        /// <summary>
        /// A mapping from the absolute path of the node to a list of nodes. Since changes in the
        /// city have no impact to the source code, this will only be initialized during start up.
        /// </summary>
        private IDictionary<string, ICollection<InteractableObject>> cachedObjects;

        /// <summary>
        /// A mapping of all registered connections to the project they have opened. Only add and
        /// delete elements in this directory while using <see cref="semaphore"/>.
        /// </summary>
        private IDictionary<string, ICollection<JsonRpcClientConnection>> cachedConnections;

        /// <summary>
        /// Contains all Graphs in this scene
        /// </summary>
        private ICollection<Graph> cachedGraphs;

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
            connectionSignal = new SemaphoreSlim(0, 1);
            pendingSelections = new HashSet<InteractableObject>();
            cachedConnections = new Dictionary<string, ICollection<JsonRpcClientConnection>>();

            InitializeCachedObjects();

            server = new JsonRpcSocketServer(new RemoteProcedureCalls(this), Port);
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
            connectionSignal?.Dispose();
            Instance = null;
        }

        /// <summary>
        /// Will get every node using <see cref="SceneQueries.AllGameNodesInScene"/> and store
        /// them in <see cref="cachedObjects"/>.
        /// </summary>
        private void InitializeCachedObjects()
        {
            cachedObjects = new Dictionary<string, ICollection<InteractableObject>>();
            var allNodes = SceneQueries.AllGameNodesInScene(true, true);
            //cachedGraphs = SceneQueries.GetGraphs(allNodes);
            cachedGraphs = new List<Graph>();

            // Get all nodes in scene
            foreach (var node in allNodes)
            {
                var key = GenerateKey(node.GetNode());

                if (key != null && node.TryGetComponent(out InteractableObject obj))
                {
                    if (!cachedObjects.ContainsKey(key))
                    {
                        cachedObjects[key] = new List<InteractableObject>();
                    }
                    cachedObjects[key].Add(obj);
                }
            }
        }

        /// <summary>
        /// Generates an appropriate key for a given node.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <returns>A key.</returns>
        private string GenerateKey(Node node)
        {
            var fileName = node.Filename();
            var path = node.Path();
            var sourceName = node.SourceName;
            var line = node.SourceLine();
            var column = node.SourceColumn();
            try
            {
                if (fileName == null || path == null) return null;

                var key = Path.GetFullPath(path + fileName);
                if (sourceName != null && !sourceName.Equals(""))
                {
                    key = $"{key}:{sourceName}";
                }

                if (line.HasValue && column.HasValue && !IgnorePosition)
                {
                    key = $"{key}:{line}:{column}";
                }

                return key;
            }
            catch (Exception)
            {
                // File not found
                return null;
            }
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
            var elements = new HashSet<InteractableObject>();
            elements.UnionWith(pendingSelections);
            pendingSelections.Clear();
            return elements;
        }

        /// <summary>
        /// Opens the specified file in an IDE.
        /// </summary>
        /// <param name="filePath">The absolute file path.</param>
        /// <returns>Async UniTask.</returns>
        public async UniTask OpenFile(string filePath, int? line = null)
        {
            await LookForIDEConnection();
            // TODO: Fix me
            await ideCalls.OpenFile(cachedConnections.First().Value.First(), filePath, line);
        }

        #region IDE Management

        /// <summary>
        /// Is looking for any active IDE. If no instance is found, will open a new IDE
        /// instance.
        /// </summary>
        /// <returns>Async UniTask.</returns>
        private async UniTask LookForIDEConnection()
        {
            if (!server.IsConnected())
            {
                await OpenNewIDEInstanceAsync();
            }
            // TODO: Fix me
            await ideCalls.FocusIDE(cachedConnections.First().Value.First());
        }

        /// <summary>
        /// Checks if the IDE is the right <see cref="Type"/> and contains a project that is represented
        /// by a graph. If <see cref="ConnectToAny"/> is true, it will skip the project check up.
        /// </summary>
        /// <param name="connection">The IDE connection.</param>
        /// <returns>True if IDE was accepted, false otherwise.</returns>
        private async UniTask<bool> CheckIDE(JsonRpcClientConnection connection)
        {
            // Right version of the IDE
            var version = await ideCalls.GetIDEVersion(connection);
            if (version == null || !version.Equals(Type.ToString())) return false;

            if (await ideCalls.WasStartedBySee(connection))
            {
                connectionSignal.Release();
                return true;
            }

            if (ConnectToAny) return true;

            var project = await ideCalls.GetProjectPath(connection);
            foreach (var graph in cachedGraphs)
            {
                // TODO: Fix me!
                if (graph.Path.Equals(ideCalls))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Opens the IDE defined in <see cref="Type"/>.
        /// </summary>
        /// <returns>Async UniTask.</returns>
        private async UniTask OpenNewIDEInstanceAsync()
        {
            string arguments;
            string fileName;
            // TODO: Dynamically generate location of (.sln)
            string project = Path.GetFullPath("SEE.sln");

            switch (Type)
            {
                case Ide.VisualStudio2019:
                    fileName = await VSPathFinder.GetVisualStudioExecutableAsync(VSPathFinder.Version.VS2019);
                    arguments = $"\"{project}\" /VsSeeExtension";
                    break;
                case Ide.VisualStudio2022:
                    fileName = await VSPathFinder.GetVisualStudioExecutableAsync(VSPathFinder.Version.VS2022);
                    arguments = $"\"{project}\" /VsSeeExtension";
                    break;
                default:
                    throw new NotImplementedException($"Implementation of case {Type} not found");
            }
            var start = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
            };

            connectionSignal = new SemaphoreSlim(0, 1);

            try
            {
                using var proc = Process.Start(start);
            }
            catch (Exception e)
            {
#if UNITY_EDITOR
                Debug.LogError(e);
#endif
                throw;
            }

            await connectionSignal.WaitAsync();
        }

        /// <summary>
        /// Will be called when connection to client is established successful. And checks whether
        /// the client contains the right project. A connection will be added to
        /// <see cref="cachedConnections"/> when everything was successful.
        /// </summary>
        /// <param name="connection">The connection.</param>
        private void ConnectedToClient(JsonRpcClientConnection connection)
        {
            UniTask.Run(async () =>
            {
                if (await CheckIDE(connection))
                {
                    await semaphore.WaitAsync();

                    var project = ConnectToAny ? "" : await ideCalls.GetProjectPath(connection);

                    if (project == null) return;

                    if (!cachedConnections.ContainsKey(project))
                    {
                        cachedConnections[project] = new List<JsonRpcClientConnection>();
                    }
                    cachedConnections[project].Add(connection);

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
        private void DisconnectedFromClient(JsonRpcClientConnection connection)
        {
            UniTask.Run(async () =>
            {
                await semaphore.WaitAsync();

                var key = cachedConnections.FirstOrDefault(x => x.Value.Contains(connection)).Key;
                if (key != null)
                {
                    if (cachedConnections[key].Count > 1)
                    {
                        cachedConnections[key].Remove(connection);
                    }
                    else
                    {
                        cachedConnections.Remove(key);
                    }
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