using System;
using System.IO.Pipes;
using System.Threading.Tasks;
using StreamRpc;
using PipeOptions = System.IO.Pipes.PipeOptions;

namespace Assets.SEE.IdeIntegration.IPC
{
    public sealed class JsonRpcNamedPipeServer : JsonRpcServer
    {

        public override Task CallRemoteProcessAsync(string targetName)
        {
            throw new NotImplementedException();
        }

        protected override async Task StartServerAsync()
        {
            using var stream = new NamedPipeClientStream(".", "VsSeeNamedPipe", PipeDirection.InOut, PipeOptions.Asynchronous);
            await stream.ConnectAsync();
            using var jsonRpc = JsonRpc.Attach(stream);
            var sum = await jsonRpc.InvokeAsync<int>("Add", 3, 5);
            await Rpc.Completion;
        }
    }
}
