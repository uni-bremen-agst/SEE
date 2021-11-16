using System;
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
        /// <summary>
        /// There is currently only an implementation for Visual Studio.
        /// </summary>
        public enum Ide
        {
            VisualStudio // Establish connection to Visual Studio.
        };

        public static IDEIntegration Instance { get; private set; }

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
            InitializeJsonRpcServer();

            _rpc.Connected += ConnectedToClient;
            _rpc.Disconnected += DisconnectedFromClient;

            _rpc.Start();
        }

        /// <summary>
        /// 
        /// </summary>
        private void InitializeJsonRpcServer()
        {
            _rpc = Type switch
            {
                Ide.VisualStudio => 
                    new JsonRpcSocketServer(new Utils.RemoteProcedureCalls(), VisualStudioPort),
                _ => throw new NotImplementedException($"Implementation of case {Type} not found"),
            };
        }

        /// <summary>
        /// Will be called when connection to client is established successful.
        /// </summary>
        private void ConnectedToClient()
        {
#if UNITY_EDITOR
            Debug.Log("Socket connection to IDE established.\n");
#endif
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