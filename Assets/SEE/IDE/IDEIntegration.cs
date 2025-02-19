﻿// Copyright © 2022 Jan-Philipp Schramm
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS
// BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR
// IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Cysharp.Threading.Tasks;
using SEE.Controls;
using SEE.DataModel.DG;
using SEE.Game;
using SEE.Game.City;
using SEE.UI.Notification;
using SEE.GO;
using SEE.Utils;
using UnityEngine;
using Debug = UnityEngine.Debug;
using SEE.Utils.IdeRPC;

namespace SEE.IDE
{
    /// <summary>
    /// This class establishes the connection to an IDE of choice. There is the
    /// option to choose between all possible IDE implementations. Currently,
    /// only Visual Studio is supported, but this class could be easily extended in the
    /// future.
    /// Note: Only one instance of this class can be created.
    /// </summary>
    public partial class IDEIntegration
    {
        /// <summary>
        /// Whether the integration with the IDE should be enabled.
        ///
        /// TODO: We need to turn this into a property to be able to enable/disable
        /// IDE integration at run-time. But that needs a bit more thinking.
        /// For the time being, it remains a regular field that will cause the
        /// <see cref="Instance"/> to be destroyed in <see cref="Awake"/> in
        /// case this field is false.
        /// </summary>
        [Tooltip("Whether the integration with the IDE should be enabled.")]
        public bool EnableIDEIntegration = false;

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
        /// A mapping from an absolute file path of a given node to another Dictionary, which
        /// will map an element key generated by <see cref="GenerateKey"/> to a list of
        /// GameObjects (representing the Nodes in this scene). Since changes in the city have
        /// no impact on the source code, this will only be initialized during start up.
        /// </summary>
        private IDictionary<string, IDictionary<string, ICollection<GameObject>>> cachedObjects;

        /// <summary>
        /// A mapping of all registered connections to the project they have opened.
        /// </summary>
        private ConcurrentDictionary<string, JsonRpcConnection> cachedConnections;

        /// <summary>
        /// Contains all solution paths of code cities in this scene.
        /// </summary>
        private HashSet<string> cachedSolutionPaths;

        /// <summary>
        /// Contains all <see cref="InteractableObject"/>s that were selected by the selected IDE.
        /// Don't make any changes on this set directly. Instead, a new assignment should be made
        /// when the set changed.
        /// </summary>
        private HashSet<InteractableObject> pendingSelections;

        #region Initialization

        /// <summary>
        /// If <see cref="EnableIDEIntegration"/> is false, we will disable this component.
        /// </summary>
        public void Awake()
        {
            if (!EnableIDEIntegration)
            {
                enabled = false;
                Destroyer.Destroy(this);
            }
        }

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
                Destroyer.Destroy(this);
                return;
            }

            Instance = this;
            pendingSelections = new HashSet<InteractableObject>();
            cachedConnections = new ConcurrentDictionary<string, JsonRpcConnection>();

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
                    await server.StartAsync(MaxNumberOfIdes);
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
            if (server != null)
            {
                server.Connected -= ConnectedToClient;
                server.Disconnected -= DisconnectedFromClient;

                server.Dispose();
            }
            Instance = null;
        }

        /// <summary>
        /// Will get every node using <see cref="SceneQueries.AllGameNodesInScene"/> and store
        /// them in <see cref="cachedObjects"/>. Additionally will look for all solution paths.
        /// </summary>
        private void InitializeSceneElementsObjects()
        {
            // A GameObject should be unique, but the key generated by SourceLength may be used repeatedly.
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
        /// <param name="path">The retrieved absolute platform-dependent path of the given node. Can be null.</param>
        /// <param name="key">The key generated for the given node. Can be null.</param>
        /// <returns>True in case of success.</returns>
        private bool TryGenerateNodeKey(Node node, out string path, out string key)
        {
            path = null;
            try
            {
                path = Path.GetFullPath(node.AbsolutePlatformPath());
            }
            catch (Exception)
            {
                // File not found
            }

            int? length = node.SourceRange?.Lines;
            key = GenerateKey(node.SourceName, node.SourceLine.GetValueOrDefault(),
                              node.SourceColumn.GetValueOrDefault(), length.GetValueOrDefault());
            return path != null && key != null;
        }

        /// <summary>
        /// Will generate a key from the given parameter to be used for <see cref="cachedObjects"/>.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <param name="line">The line number of the element.</param>
        /// <param name="column">The column number of the element.</param>
        /// <param name="length">The length of the code range (the number of lines from the start of its declaration until its end).</param>
        /// <returns>A key for <see cref="cachedObjects"/>.</returns>
        private string GenerateKey(string name, int line, int column, int length)
        {
            if (name == null)
            {
                return "";
            }
            else
            {
                string key = UseElementRange || UseElementPosition ? $"{name}:{line}" : name;
                key = UseElementRange ? $"{key}:{length}" : key;
                key = UseElementPosition ? $"{key}:{column}" : key;
                return key;
            }
        }

        #endregion

        /// <summary>
        /// True if there is any pending selection needed to be taken.
        /// </summary>
        /// <returns>True if <see cref="SEEInput.SelectionEnabled"/> and there is a selection.</returns>
        public bool PendingSelectionsAction()
        {
            return SEEInput.SelectionEnabled && pendingSelections.Count > 0;
        }

        /// <summary>
        /// Returns a set of <see cref="InteractableObject"/>s that represents the elements the IDE
        /// wants to be highlighted. After calling this method, the underlying set will be cleared
        /// and thus is empty again.
        /// </summary>
        /// <returns>The set of elements to be highlighted.</returns>
        public HashSet<InteractableObject> PopPendingSelections()
        {
            HashSet<InteractableObject> elements = new HashSet<InteractableObject>(pendingSelections);
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
        public async UniTask OpenFileAsync(string filePath, string solutionPath, int? line = null)
        {
            JsonRpcConnection connection = await LookForIDEConnectionAsync(solutionPath);
            if (connection != null)
            {
                //string fullPath = Path.GetFullPath(filePath);
                try
                {
                    await ideCalls.OpenFileAsync(connection, filePath, line);
                }
                catch (Exception)
                {
                    ShowNotification.Error("File not found", $"File path '{filePath}' of node doesn't exist.");
                }
            }
        }

        #region IDE Management

        /// <summary>
        /// Looks for any active IDE. If no instance is found, opens a new IDE
        /// instance. Also focusing the IDE.
        /// </summary>
        /// <param name="solutionPath">The solution path.</param>
        /// <returns>Will return the <see cref="JsonRpcConnection"/> to a given solution path.
        /// Null if not found.</returns>
        private async UniTask<JsonRpcConnection> LookForIDEConnectionAsync(string solutionPath)
        {
            if (solutionPath == null)
            {
                return null;
            }

            JsonRpcConnection connection = null;
            try
            {
                connection = cachedConnections[solutionPath];
            }
            catch (Exception)
            {
                if (MaxNumberOfIdes == cachedConnections.Count && MaxNumberOfIdes != 0
                                                               && solutionPath != "")
                {
                    connection = cachedConnections.First().Value;
                    await ideCalls.ChangeSolutionAsync(connection, solutionPath);
                }
                else if (MaxNumberOfIdes != 0 && solutionPath != "")
                {
                    connection = await OpenNewIDEInstanceAsync(solutionPath);
                }
                else
                {
                    ShowNotification.Error("Solution file not defined",
                                           "SEE City is missing the solution file.");
                    return connection;
                }
            }

            await ideCalls.FocusIDEAsync(connection);

            return connection;
        }

        /// <summary>
        /// Checks if the IDE is the right <see cref="Type"/> and contains a project that is represented
        /// by a graph. If <see cref="ConnectToAny"/> is true, it will skip the project check up.
        /// </summary>
        /// <param name="connection">The IDE connection.</param>
        /// <returns>True if IDE was accepted, false otherwise.</returns>
        private async UniTask<bool> CheckIDEAsync(JsonRpcConnection connection)
        {
            // Right version of the IDE
            string version = await ideCalls.GetIDEVersionAsync(connection);
            if (version == null || !version.Equals(Type.ToString()))
            {
                return false;
            }
            else
            {
                return await ideCalls.WasStartedBySeeAsync(connection)
                       || ConnectToAny
                       || cachedSolutionPaths.Contains(await ideCalls.GetProjectPathAsync(connection));
            }
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
                    throw new NotImplementedException($"Handling of case {Type} not implemented.");
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
            CancellationTokenSource cts = new CancellationTokenSource(180000);

            try
            {
                await LookUpConnection(cts.Token);
            }
            catch (OperationCanceledException)
            {
                ShowNotification.Error("Connection timed out",
                    "Couldn't establish connection to IDE.");
            }
            finally
            {
                cts.Dispose();
            }

            return connection;

            async UniTask LookUpConnection(CancellationToken token)
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
                        await UniTask.Delay(200, cancellationToken: token);
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
                if (await CheckIDEAsync(connection))
                {

                    string project = await ideCalls.GetProjectPathAsync(connection);
                    connection.AddTarget(new RemoteProcedureCalls(this, project));

                    if (project == null)
                    {
                        return;
                    }

                    if (!cachedConnections.ContainsKey(project))
                    {
                        cachedConnections[project] = connection;
                    }

                    await UniTask.SwitchToMainThread();
                    ShowNotification.Info("Connected to IDE", "Connection to IDE established.", 5.0f);
                }
                else
                {
                    await ideCalls.DeclineAsync(connection);
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
                string key = cachedConnections.FirstOrDefault(x => x.Value.Equals(connection)).Key;
                if (key != null)
                {
                    if (!cachedConnections.TryRemove(key, out _))
                    {
#if UNITY_EDITOR
                        Debug.LogError($"Tried to remove a nonexistent connection.");
#endif
                    }

                    await UniTask.SwitchToMainThread();
                    ShowNotification.Info("Disconnected from IDE",
                        "The IDE was disconnected form SEE.", 5.0f);
                }
            }).Forget();
        }

        #endregion
    }
}
