using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Cysharp.Threading.Tasks;
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
        #region Remote Procedure Calls

        /// <summary>
        /// This class contains all functions, that can be called by the client (IDE).
        /// </summary>
        private class RemoteProcedureCalls
        {
            /// <summary>
            /// Instance of parent class.
            /// </summary>
            private readonly IDEIntegration _ideIntegration;

            /// <summary>
            /// Nested class in <see cref="IDEIntegration"/>. Contains all methods that can be accessed
            /// by the client. Should only be initiated by the <see cref="IDEIntegration"/>.
            /// </summary>
            /// <param name="ideIntegration">Instance of IDEIntegration</param>
            public RemoteProcedureCalls(IDEIntegration ideIntegration)
            {
                _ideIntegration = ideIntegration;
            }

            /// <summary>
            /// Adds all nodes from <see cref="_cachedObjects"/> with the key created by
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
                    path += $":{name}";
                }

                try
                {
                    var nodes = _ideIntegration._cachedObjects[path];
                    _pendingSelections = new HashSet<InteractableObject>(nodes);
                }
                catch (Exception)
                {
                    // The given key was not presented int the dictionary.
                }
            }
        }

        #endregion

        #region Client Calls

        /// <summary>
        /// Lists all methods remotely callable by the server in a convenient way.
        /// </summary>
        public class ClientCalls
        {
            /// <summary>
            /// Instance of parent class.
            /// </summary>
            private readonly IDEIntegration _ideIntegration;

            /// <summary>
            /// Nested class in <see cref="IDEIntegration"/>.  The purpose of this class is to
            /// deliver all methods, that can be called from the client. Should only be initiated
            /// by the <see cref="IDEIntegration"/>.
            /// </summary>
            /// <param name="ideIntegration">Instance of IDEIntegration</param>
            public ClientCalls(IDEIntegration ideIntegration)
            {
                _ideIntegration = ideIntegration;
            }

            /// <summary>
            /// Does the connected IDE contain the project the graph represents. This method will
            /// autocratically close the connection if the IDE has the wrong project open. 
            /// </summary>
            /// <returns>True if IDE contains this project, false otherwise.</returns>
            public async UniTask<bool> CheckProject(JsonRpcClientConnection connection)
            {
                // TODO: Implement this!
                return true;
            }

            /// <summary>
            /// Is looking for any active IDE. If no instance is found, will open a new IDE
            /// instance and wait until project is loaded
            /// </summary>
            /// <returns>Async UniTask.</returns>
            private async UniTask CheckForIDEInstance()
            {
                // TODO: FIX ME
                if (_ideIntegration._rpc.IsConnected()) return;
                await _ideIntegration.OpenNewIDEInstanceAsync();
            }

            /// <summary>
            /// Opens the file in the IDE of choice.
            /// </summary>
            /// <param name="path">Absolute file path.</param>
            /// <returns></returns>
            public async UniTask OpenFileAsync(string path)
            {
                await CheckForIDEInstance();
                await _ideIntegration._rpc.CallRemoteProcessAsync("OpenFile", path);
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
        /// The singleton instance of this class.
        /// </summary>
        private static IDEIntegration Instance;

        /// <summary>
        /// All callable methods by the server. Will be executed in every connected IDE.
        /// </summary>
        public static ClientCalls Client { get; private set; }

        /// <summary>
        /// Specifies to which IDE a connection is to be established.
        /// </summary>
        public Ide Type;

        /// <summary>
        /// Specifies the number of IDEs that can connect at the same time.
        /// </summary>
        public int MaxNumberOfClients = 1;

        /// <summary>
        /// TCP Socket port for communication to Visual Studio (2019).
        /// </summary>
        public int VS2019Port = 26100;

        /// <summary>
        /// TCP Socket port for communication to Visual Studio (2022).
        /// </summary>
        public int VS2022Port = 26101;

        /// <summary>
        /// The JsonRpcServer used for communication between IDE and SEE.
        /// </summary>
        private JsonRpcServer _rpc;

        /// <summary>
        /// Semaphore for accessing <see cref="_cachedConnections"/>.
        /// </summary>
        private SemaphoreSlim _semaphore;

        /// <summary>
        /// A mapping from the absolute path of the node to a list of nodes. Since changes in the
        /// city have no impact to the source code, this will only be initialized during start up.
        /// </summary>
        private IDictionary<string, ICollection<InteractableObject>> _cachedObjects;

        /// <summary>
        /// A mapping of all registered connections to the project they have opened. Only access
        /// this dictionary while using <see cref="_semaphore"/>.
        /// </summary>
        private IDictionary<string, JsonRpcClientConnection> _cachedConnections;

        /// <summary>
        /// Contains all <see cref="InteractableObject"/> that were selected by the selected IDE.
        /// Don't make any changes on this set directly. Instead, a new assignment should be made
        /// when the set changed.
        /// </summary>
        private static HashSet<InteractableObject> _pendingSelections;

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
                Debug.LogError("Currently only supported on Windows!");
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
            _semaphore = new SemaphoreSlim(1, 1);
            _pendingSelections = new HashSet<InteractableObject>();
            _cachedConnections = new Dictionary<string, JsonRpcClientConnection>();

            InitializeCachedObject();
            InitializeJsonRpcServer();

            _rpc.Connected += ConnectedToClient;
            _rpc.Disconnected += DisconnectedFromClient;

            // Starting the server as a background task.
            StartServer().Forget();

            async UniTaskVoid StartServer()
            {
                try
                {
                    await _rpc.Start(MaxNumberOfClients);
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
            _rpc.Connected -= ConnectedToClient;
            _rpc.Disconnected -= DisconnectedFromClient;

            _rpc?.Dispose();
            _semaphore?.Dispose();
            Instance = null;
        }

        /// <summary>
        /// Is there any pending selection needed to be taken.
        /// </summary>
        /// <returns>True if <see cref="SEEInput.SelectionEnabled"/> and </returns>
        public static bool PendingSelectionsAction()
        {
            return SEEInput.SelectionEnabled && _pendingSelections.Count > 0;
        }

        /// <summary>
        /// Returns a set of <see cref="InteractableObject"/> that represents the elements the IDE
        /// wants to be highlighted. After calling this method, the underlying set will be cleared
        /// and thus is empty again.
        /// </summary>
        /// <returns>The set of elements to be highlighted.</returns>
        public static HashSet<InteractableObject> PopPendingSelections()
        {
            HashSet<InteractableObject> elements = new HashSet<InteractableObject>();
            elements.UnionWith(_pendingSelections);
            _pendingSelections.Clear();
            return elements;
        }

        /// <summary>
        /// Initializes the JsonRpcServer with the right port number.
        /// </summary>
        private void InitializeJsonRpcServer()
        {
            Client = new ClientCalls(this);

            _rpc = Type switch
            {
                Ide.VisualStudio2019 =>
                    new JsonRpcSocketServer(new RemoteProcedureCalls(this), VS2019Port),
                Ide.VisualStudio2022 =>
                    new JsonRpcSocketServer(new RemoteProcedureCalls(this), VS2022Port),
                _ => throw new NotImplementedException($"Implementation of case {Type} not found"),
            };
        }

        private void InitializeCachedObject()
        {
            _cachedObjects = new Dictionary<string, ICollection<InteractableObject>>();

            // Get all nodes in scene
            foreach (var node in SceneQueries.AllGameNodesInScene(true, true))
            {
                var fileName = node.GetNode().Filename();
                var path = node.GetNode().Path();
                var name = node.GetNode().SourceName;

                if (fileName == null || path == null) continue;

                string fullPath;
                try
                {
                    fullPath = Path.GetFullPath(path + fileName);
                }
                catch (Exception)
                {
                    // File not found
                    continue;
                }

                if (name != null && (!name.Equals("?") || !name.Equals("")))
                {
                    fullPath += $":{name}";
                }

                if (node.TryGetComponent(out InteractableObject obj))
                {
                    if (!_cachedObjects.ContainsKey(fullPath))
                    {
                        _cachedObjects[fullPath] = new List<InteractableObject>();
                    }
                    _cachedObjects[fullPath].Add(obj);
                }
            }
        }

        /// <summary>
        /// Opens the IDE defined in <see cref="Type"/>.
        /// </summary>
        /// <returns>Async UniTask.</returns>
        private async UniTask OpenNewIDEInstanceAsync()
        {
            string arguments;
            string fileName;
            switch (Type)
            {
                case Ide.VisualStudio2019:
                    fileName = await VSPathFinder.GetVisualStudioExecutableAsync(VSPathFinder.Version.VS2019);
                    arguments = "";
                    break;
                case Ide.VisualStudio2022:
                    fileName = await VSPathFinder.GetVisualStudioExecutableAsync(VSPathFinder.Version.VS2022);
                    arguments = "";
                    break;
                default:
                    throw new NotImplementedException($"Implementation of case {Type} not found");
            }
            var start = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true
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
        }

        /// <summary>
        /// Will be called when connection to client is established successful. And checks whether
        /// the client contains the right project. A connection will be added to
        /// <see cref="_cachedConnections"/> when everything was successful.
        /// </summary>
        /// <param name="connection">The connection.</param>
        private void ConnectedToClient(JsonRpcClientConnection connection)
        {
            UniTask.Run(async () =>
            {
                if (await Client.CheckProject(connection))
                {
                    await _semaphore.WaitAsync();

                    // TODO: Fix this!
                    _cachedConnections["Test"] = connection;

                    _semaphore.Release();
                    
                    await UniTask.SwitchToMainThread();
                    ShowNotification.Info("Connected to IDE",
                        "Connection to IDE established.", 5.0f);
                }
            }).Forget();
        }

        /// <summary>
        /// Will be called when the client disconnected form the server and removes the connection
        /// from <see cref="_cachedConnections"/>.
        /// </summary>
        /// <param name="connection">The connection.</param>
        private void DisconnectedFromClient(JsonRpcClientConnection connection)
        {
            UniTask.Run(async () =>
            {
                await _semaphore.WaitAsync();

                var key = _cachedConnections.FirstOrDefault(x => x.Value == connection).Key;
                if (key != null)
                {
                    _cachedConnections.Remove(key);
                }

                _semaphore.Release();

                await UniTask.SwitchToMainThread();
                ShowNotification.Info("Disconnected from IDE",
                    "The IDE was disconnected form SEE.", 5.0f);
            }).Forget();
            
        }
    }
}