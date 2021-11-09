using System;
using System.Collections;
using System.ComponentModel.Design;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Threading;
using StreamRpc;
using UnityEngine;

namespace Assets.SEE.IdeIntegration
{
    public class IntegratedDevelopmentEnvironmentIpc : MonoBehaviour
    {
        public enum Ide { VisualStudio };

        public Ide Type;
        private JsonRpcServer _rpc;

        // Use this for initialization
        public void Start()
        {
            switch (Type)
            {
                case Ide.VisualStudio:
                    StartVisualStudio();
                    break;
                default:
                    throw new MissingMethodException();
            }
            _rpc.Start(new RemoteCommands());
        }

        private void StartVisualStudio()
        {
            _rpc = new JsonRpcSocketServer(26100);
        }
    }
}