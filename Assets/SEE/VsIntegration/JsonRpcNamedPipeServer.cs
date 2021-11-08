using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StreamRpc;
using PipeOptions = System.IO.Pipes.PipeOptions;

namespace Assets.SEE.VsIntegration
{
    public sealed class JsonRpcNamedPipeServer : JsonRpcServer
    {
        private NamedPipeServerStream _stream;

        public override Task CallRemoteProcessAsync(string targetName)
        {
            throw new NotImplementedException();
        }

        protected override async Task StartServerAsync()
        {
            try
            {
                _stream = new NamedPipeServerStream("VsSeeNamedPipe", PipeDirection.InOut, 1, PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            await _stream.WaitForConnectionAsync();
            Rpc = JsonRpc.Attach(_stream, Target);
            await Rpc.Completion;
        }

        public override void Dispose()
        {
            _stream?.Close();
            base.Dispose();
        }
    }
}
