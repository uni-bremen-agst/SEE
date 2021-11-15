using System;
using SEE.Utils;
using UnityEngine;

namespace SEE.Controls
{
    public class IDEIntegration : MonoBehaviour
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
                    _rpc = new JsonRpcSocketServer(new Utils.RemoteProcedureCalls(), 26100);
                    break;
                default:
                    throw new NotImplementedException($"Implementation of case {Type} not found");
            }
            _rpc.Start();
        }
    }
}