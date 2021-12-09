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
            /// <param name="line">Optional line number.</param>
            public async UniTask OpenFile(JsonRpcConnection connection, string path, int? line)
            {
                await server.CallRemoteProcessOnConnectionAsync(connection, "OpenFile", path, line);
            }

            /// <summary>
            /// Gets the absolute project path (e.g. .sln). 
            /// </summary>
            /// <param name="connection">A connection to an IDE.</param>
            /// <returns>Returns the absolute project path. Can be null.</returns>
            public async UniTask<string> GetProjectPath(JsonRpcConnection connection)
            {
                return await server.CallRemoteProcessOnConnectionAsync<string>(connection, "GetProject");
            }

            /// <summary>
            /// Gets the absolute project path (e.g. .sln). 
            /// </summary>
            /// <param name="connection">A connection to an IDE.</param>
            /// <returns>Returns the absolute project path. Can be null.</returns>
            public async UniTask<string> GetIDEVersion(JsonRpcConnection connection)
            {
                return await server.CallRemoteProcessOnConnectionAsync<string>(connection, "GetIdeVersion");
            }

            /// <summary>
            /// Was the connection started by SEE directly through a command switch.
            /// </summary>
            /// <param name="connection">A connection to an IDE.</param>
            /// <returns>True if SEE started this connection.</returns>
            public async UniTask<bool> WasStartedBySee(JsonRpcConnection connection)
            {
                return await server.CallRemoteProcessOnConnectionAsync<bool>(connection, "WasStartedBySee");
            }

            /// <summary>
            /// Will focus this IDE instance.
            /// </summary>
            /// <param name="connection">A connection to an IDE.</param>
            public async UniTask FocusIDE(JsonRpcConnection connection)
            {
                await server.CallRemoteProcessOnConnectionAsync(connection, "SetFocus");
            }

            /// <summary>
            /// Calling this method will change the loaded solution of this Connection.
            /// </summary>
            /// <param name="connection">A connection to an IDE.</param>
            /// <param name="path">The absolute solution path.</param>
            public async UniTask ChangeSolution(JsonRpcConnection connection, string path)
            {
                await server.CallRemoteProcessOnConnectionAsync(connection, "", path);
            }

            /// <summary>
            /// Declines an IDE instance.
            /// </summary>
            /// <param name="connection">A connection to an IDE.</param>
            public async UniTask Decline(JsonRpcConnection connection)
            {
                await server.CallRemoteProcessOnConnectionAsync(connection, "Decline");
            }
        }

        #endregion

        #region Remote Procedure Calls

        /// <summary>
        /// This class contains all functions, that can be called by the client (IDE). It will be
        /// given to each <see cref="JsonRpcConnection"/> individually.
        /// </summary>
        private class RemoteProcedureCalls
        {
            /// <summary>
            /// instance of parent class.
            /// </summary>
            private readonly IDEIntegration ideIntegration;

            /// <summary>
            /// The current solution path of the connected IDE.
            /// </summary>
            private string solutionPath;

            /// <summary>
            /// Nested class in <see cref="IDEIntegration"/>. Contains all methods that can be accessed
            /// by the client. Should only be initiated by the <see cref="IDEIntegration"/>.
            /// </summary>
            /// <param name="ideIntegration">instance of IDEIntegration</param>
            /// <param name="solutionPath">The solution path of the connected IDE.</param>
            public RemoteProcedureCalls(IDEIntegration ideIntegration, string solutionPath)
            {
                this.ideIntegration = ideIntegration;
                this.solutionPath = solutionPath;
            }

            /// <summary>
            /// Adds all nodes from <see cref="cachedObjects"/> with the key created by
            /// <paramref name="path"/> and <paramref name="name"/>. If <paramref name="name"/> is
            /// null, it won't be appended to the key.
            /// </summary>
            /// <param name="path">The absolute path to the source file.</param>
            /// <param name="name">Name of the element in a file.</param>
            /// <param name="line">Line of the element.</param>
            /// <param name="column">Column of the element.</param>
            public void HighlightNode(string path, string name, int line, int column)
            {
                try
                {
                    var key = GenerateKey(path, name, line, column);
                    var objects = ideIntegration.cachedObjects[key];
                    SetInteractableObjects(objects);
                }
                catch (Exception)
                {
                    // The given key was not presented int the dictionary.
                }
            }


            /// <summary>
            /// Adds all edges from <see cref="cachedObjects"/> with the key created by
            /// <paramref name="path"/> and <paramref name="name"/>. If <paramref name="name"/> is
            /// null, it won't be appended to the key.
            /// </summary>
            /// <param name="path">The absolute path to the source file.</param>
            /// <param name="name">Name of the element in a file.</param>
            /// <param name="line">Line of the element.</param>
            /// <param name="column">Column of the element.</param>
            public void HighlightNodeReferences(string path, string name, int line, int column)
            {
                var objects = new HashSet<GameObject>();
                try
                {
                    var nodes = ideIntegration.cachedObjects[
                        GenerateKey(path, name, line, column)];
                    var ids = new HashSet<string>();
                    foreach (var node in nodes)
                    {
                        ids.Add(node.GetNode().ID);
                    }
                    objects.UnionWith(SceneQueries.Find(ids));
                }
                catch (Exception)
                {
                    // The given key was not presented int the dictionary.
                }

                SetInteractableObjects(objects);
            }

            /// <summary>
            /// This method will highlight all given elements of a specific file in SEE.
            /// </summary>
            /// <param name="path">The absolute path to the source file.</param>
            /// <param name="nodes">A list of tuples representing the nodes. Order: (name/line/column)</param>
            /// <returns></returns>
            public void HighlightNodes(string path, ICollection<Tuple<string, int, int>> nodes)
            {
                var objects = new HashSet<GameObject>();
                foreach (var (name, line, column) in nodes)
                {
                    try
                    {
                        objects.UnionWith(ideIntegration.cachedObjects[GenerateKey(path, name, line, column)]);
                    }
                    catch (Exception)
                    {
                        // The given key was not presented int the dictionary.
                    }
                }
                SetInteractableObjects(objects);
            }

            /// <summary>
            /// Solution path changed.
            /// </summary>
            /// <returns>Async Task.</returns>
            public void SolutionChanged(string path)
            {
                ideIntegration.semaphore.Wait();
                var connection = ideIntegration.cachedConnections[solutionPath];

                if (ideIntegration.cachedSolutionPaths.Contains(path) || ideIntegration.ConnectToAny)
                {
                    if (ideIntegration.cachedConnections.Remove(solutionPath))
                    {
                        ideIntegration.cachedConnections.Add(path, connection);
                        solutionPath = path;
                    }
                }
                else
                {
                    ideIntegration.ideCalls.Decline(connection).Forget();
                }

                ideIntegration.semaphore.Release();

            }

            /// <summary>
            /// Will generate a key from the given parameter to be used for <see cref="cachedObjects"/>.
            /// </summary>
            /// <param name="path">The path of the file.</param>
            /// <param name="name">The name of the element.</param>
            /// <param name="line">The line number of the element.</param>
            /// <param name="column">The column number of the element.</param>
            /// <returns></returns>
            private string GenerateKey(string path, string name, int line, int column)
            {
                var tmp = "";

                if (name != null)
                {
                    tmp = $"{path}:{name}";

                    if (!ideIntegration.IgnorePosition)
                    {
                        tmp += $":{line}:{column}";
                    }
                }

                return tmp;
            }

            /// <summary>
            /// Will transform the given collection to a set of <see cref="InteractableObject"/> and
            /// add them to <see cref="pendingSelections"/>.
            /// </summary>
            /// <param name="objects">The collection of GameObjects representing nodes.</param>
            private void SetInteractableObjects(IEnumerable<GameObject> objects)
            {
                UniTask.Run(async () =>
                {
                    await UniTask.SwitchToMainThread();
                    var tmp = new HashSet<InteractableObject>();

                    foreach (var node in objects)
                    {
                        if (node.TryGetComponent(out InteractableObject obj))
                        {
                            tmp.Add(obj);
                        }
                    }

                    await UniTask.SwitchToThreadPool();

                    ideIntegration.pendingSelections = tmp;
                });
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
        /// A mapping from the absolute path of the node to a list of nodes. Since changes in the
        /// city have no impact to the source code, this will only be initialized during start up.
        /// </summary>
        private IDictionary<string, ICollection<GameObject>> cachedObjects;

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
            cachedObjects = new Dictionary<string, ICollection<GameObject>>();
            cachedSolutionPaths = new HashSet<string>();

            // Get all nodes in scene
            foreach (var node in SceneQueries.AllGameNodesInScene(true, true))
            {
                var key = GenerateKey(node.GetNode());

                if (key == null) continue;
                if (!cachedObjects.ContainsKey(key))
                {
                    cachedObjects[key] = new List<GameObject>();
                }
                cachedObjects[key].Add(node);
            }

            foreach (var obj in GameObject.FindGameObjectsWithTag(Tags.CodeCity))
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
        /// <returns>A key. Can be null</returns>
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
        /// <param name="solutionPath">The absolute solution path of this file.</param>
        /// <param name="line">Optional line number.</param>
        /// <returns>Async UniTask.</returns>
        public async UniTask OpenFile(string filePath, string solutionPath, int? line = null)
        {
            var connection = await LookForIDEConnection(solutionPath);
            if (connection == null) return;
            await ideCalls.OpenFile(connection, filePath, line);
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
            var version = await ideCalls.GetIDEVersion(connection);
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
            var start = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
            };

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

                    var project = await ideCalls.GetProjectPath(connection);

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

                var key = cachedConnections.FirstOrDefault(x => x.Value.Equals(connection)).Key;
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