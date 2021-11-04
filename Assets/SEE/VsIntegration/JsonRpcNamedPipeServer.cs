using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.SEE.VsIntegration
{
    public sealed class JsonRpcNamedPipeServer : JsonRpcServer
    {
        public override Task CallRemoteProcessAsync(string targetName)
        {
            throw new NotImplementedException();
        }

        protected override Task StartServerAsync()
        {
            throw new NotImplementedException();
        }
    }
}
