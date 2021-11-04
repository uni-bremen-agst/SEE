using System.Collections;
using UnityEngine;

namespace Assets.SEE.VsIntegration
{
    public class VisualStudioIpc : MonoBehaviour
    {
        public enum IpcTypes { NamedPipe, Socket };

        public IpcTypes Type;
        private JsonRpcServer _rpc;

        // Use this for initialization
        void Start()
        {
            switch (Type)
            {
                case IpcTypes.NamedPipe:
                    _rpc = new JsonRpcNamedPipeServer();
                    break;
                case IpcTypes.Socket:
                    break;
                default:
                    break;
            }

            _rpc.Start(new VsRemoteCommands());
        }
    }
}