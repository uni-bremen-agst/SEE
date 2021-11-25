using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Cysharp.Threading.Tasks;
using SEE.Game;
using SEE.Game.City;
using SEE.Game.Evolution;
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

            public void HighlightNode(string path)
            {
                UniTask.Run(async () =>
                {
                    await UniTask.SwitchToMainThread();
                    var nodes = _ideIntegration._nodes[path];

                    if (nodes == null) return;

                    foreach (var node in nodes)
                    {
                        Outline.Create(node, Color.blue);
                    }
                    Debug.Log(path);
                });
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
        public static IDEIntegration Instance { get; private set; }

        /// <summary>
        /// All callable methods by the server.
        /// </summary>
        public ClientCalls Client { get; private set; } 

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
        private IDictionary<string, ICollection<GameObject>> _nodes;

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
            Client = new ClientCalls(this);
            _nodes = new Dictionary<string, ICollection<GameObject>>();
            
            foreach (var node in SceneQueries.AllGameNodesInScene(true, true))
            {
                var fileName = node.GetNode().Filename();
                var path = node.GetNode().Path();

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

                if (!_nodes.ContainsKey(fullPath))
                {
                    _nodes[fullPath] = new List<GameObject>();
                }
                _nodes[fullPath].Add(node);
            }

            InitializeJsonRpcServer();

            _rpc.Connected += ConnectedToClient;
            _rpc.Disconnected += DisconnectedFromClient;

            _rpc.Start();
        }

        /// <summary>
        /// Initializes the JsonRpcServer.
        /// </summary>
        private void InitializeJsonRpcServer()
        {
            _rpc = Type switch
            {
                Ide.VisualStudio2019 =>
                    new JsonRpcSocketServer(new RemoteProcedureCalls(this), VS2019Port),
                Ide.VisualStudio2022 =>
                    new JsonRpcSocketServer(new RemoteProcedureCalls(this), VS2022Port),
                _ => throw new NotImplementedException($"Implementation of case {Type} not found"),
            };
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
        private void ConnectedToClient()
        {
            //TODO: Check whether the correct IDE instance is connected
            ShowNotification.Info("Connected to IDE",
                "Connection to IDE established.", 5.0f);
        }

        /// <summary>
        /// Will be called when the client disconnected form the server.
        /// </summary>
        private void DisconnectedFromClient()
        {
            ShowNotification.Info("Disconnected from IDE",
                "The IDE was disconnected form SEE.", 5.0f);
        }
    }
}