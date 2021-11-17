using System;
using Cysharp.Threading.Tasks;
using SEE.Game.UI.Notification;
using SEE.Utils;
using UnityEngine;

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
            /// TODO: Remove! Just for testing purpose.
            /// </summary>
            /// <param name="a"></param>
            /// <param name="b"></param>
            /// <returns></returns>
            public int Add(int a, int b)
            {
                return a + b;
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
            /// deliver all methods, that can be called from the client.
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
                await _ideIntegration._rpc.CallRemoteProcessAsync("OpenFile", path);
            }
        }

        #endregion

        /// <summary>
        /// There is currently only an implementation for Visual Studio.
        /// </summary>
        public enum Ide
        {
            VisualStudio // Establish connection to Visual Studio.
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
        /// TCP Socket port for communication to Visual Studio.
        /// </summary>
        public int VisualStudioPort = 26100;

        /// <summary>
        /// The JsonRpcServer used for communication between IDE and SEE.
        /// </summary>
        private JsonRpcServer _rpc;

        /// <summary>
        /// Initializes all necessary objects for the inter-process communication
        /// between SEE and the selected IDE.
        /// </summary>
        public void Start()
        {
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
                Ide.VisualStudio => 
                    new JsonRpcSocketServer(new RemoteProcedureCalls(), VisualStudioPort),
                _ => throw new NotImplementedException($"Implementation of case {Type} not found"),
            };
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