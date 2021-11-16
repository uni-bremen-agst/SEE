using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using StreamRpc;
using PipeOptions = System.IO.Pipes.PipeOptions;

namespace SEE.Utils
{
    /// <summary>
    /// The server for inter-process connection. This implementation is using named pipe server.
    /// Note: Can't be used due to missing <see cref="NamedPipeServerStream"/> implementation .
    /// </summary>
    public sealed class JsonRpcNamedPipeServer : JsonRpcServer
    {
        /// <summary>
        /// Creates a new JsonRpcNamedPipeServer instance.
        /// </summary>
        /// <param name="target">An object that contains function that can be called
        /// remotely.</param>   
        public JsonRpcNamedPipeServer(object target) : base(target)
        {
        }

        /// <summary>
        /// Starts the named pipe server implementation.
        /// </summary>
        /// <param name="token">Token to cancel the current Task.</param>                          
        /// <returns>Async Task.</returns>
        protected override async Task StartServerAsync(CancellationToken token)
        {
            using var stream = new NamedPipeServerStream("VsSeeNamedPipe", PipeDirection.InOut,
                1,
                PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            await stream.WaitForConnectionAsync(token);
            Connected?.Invoke();
            using var jsonRpc = JsonRpc.Attach(stream);
            var sum = await jsonRpc.InvokeAsync<int>("Add", 3, 5);
            await Rpc.Completion;
            Disconnected?.Invoke();
        }
    }
}
