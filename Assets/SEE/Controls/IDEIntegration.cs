using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
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
        /// This class contains all functions, that can be called
        /// by the client (IDE).
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
            /// Is looking for any active IDE.
            /// </summary>
            /// <returns>Async UniTask.</returns>
            private async UniTask CheckForIDEInstance()
            {
                if (_ideIntegration._rpc.IsConnected()) return;
                await _ideIntegration.OpenNewIDEInstanceAsync();
            }

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
        /// All callable methods by the server.
        /// </summary>
        public static ClientCalls Client { get; private set; }

        /// <summary>
        /// Specifies to which IDE a connection is to be established.
        /// </summary>
        public Ide Type;

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
        /// A mapping from the absolute path of the node to a list of nodes. Since changes in the
        /// city have no impact to the source code, this will only be initialized during start up.
        /// </summary>
        private IDictionary<string, ICollection<InteractableObject>> _cachedObjects;

        /// <summary>
        /// Contains all <see cref="InteractableObject"/> that were selected by the selected IDE.
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
            _pendingSelections = new HashSet<InteractableObject>();

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
                    await _rpc.Start(1);
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
            _rpc.Stop();
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
        /// Opens the IDE defined in <see cref="Type"/>. Will wait until client is connected.
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

            // TODO: wait until connected
        }

        /// <summary>
        /// Will be called when connection to client is established successful.
        /// </summary>
        /// <param name="sender">The sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void ConnectedToClient(object sender, EventArgs e)
        {
            //TODO: Check whether the correct IDE instance is connected
            ShowNotification.Info("Connected to IDE",
                "Connection to IDE established.", 5.0f);
        }

        /// <summary>
        /// Will be called when the client disconnected form the server.
        /// </summary>
        /// <param name="sender">The sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void DisconnectedFromClient(object sender, EventArgs e)
        {
            ShowNotification.Info("Disconnected from IDE",
                "The IDE was disconnected form SEE.", 5.0f);
        }
    }
}