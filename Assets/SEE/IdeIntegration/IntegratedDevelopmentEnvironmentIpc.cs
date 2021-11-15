using System;
using System.Collections;
using System.ComponentModel.Design;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Threading;
using Assets.SEE.IdeIntegration.IPC;
using StreamRpc;
using UnityEngine;

namespace Assets.SEE.IdeIntegration
{
    public class IntegratedDevelopmentEnvironmentIpc : MonoBehaviour
    {
        /// <summary>
        /// There is currently only an implementation for Visual Studio.
        /// </summary>
        public enum Ide
        {
            VisualStudio // Establish connection to Visual Studio.
        };

        /// <summary>
        /// Specifies to which IDE a connection is to be established.
        /// </summary>
        public Ide Type;

        /// <summary>
        /// The JsonRpcServer used for communication between IDE and SEE.
        /// </summary>
        private JsonRpcServer _rpc;

        /// <summary>
        /// Initializes all necessary objects for IPC.
        /// </summary>
        public void Start()
        {
            switch (Type)
            {
                case Ide.VisualStudio:
                    _rpc = new JsonRpcSocketServer(new RemoteCommands(), 26100);
                    break;
                default:
                    throw new MissingMethodException("Implementation of IDE integration not found!");
            }
            _rpc.Start();
        }
    }
}