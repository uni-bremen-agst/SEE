using System.Threading;
using Cysharp.Threading.Tasks;
using StreamRpc;

namespace SEE.Utils
{
    /// <summary>
    /// Represents a unique client connection. Contains the underlying <see cref="JsonRpc"/>
    /// instance to invoke methods in this specific client.
    /// </summary>
    public abstract class JsonRpcClientConnection
    {
        /// <summary>
        /// Represents the method that will handle the client connection events.
        /// </summary>
        /// <param name="connection">Connection that fired this event.</param>
        public delegate void ConnectionEventHandler(JsonRpcClientConnection connection);

        /// <summary>
        /// Will be fired when the client is fully connected.
        /// </summary>
        public ConnectionEventHandler Connected;

        /// <summary>
        /// Will be fired when the client gets disconnected.
        /// </summary>
        public ConnectionEventHandler Disconnected;

        /// <summary>
        /// The underlying JsonRpc connection. Will be null if no connection is established.
        /// </summary>
        public JsonRpc Rpc { get; protected set; }

        /// <summary>
        /// The server instance related to this client connection.
        /// </summary>
        protected JsonRpcServer RpcServer;

        /// <summary>
        /// Cancellation token to stop any running background tasks in this client connection.
        /// </summary>
        private readonly CancellationTokenSource _tokenSource;

        /// <summary>
        /// Creates a new client connection.
        /// </summary>
        /// <param name="rpcServer">The server instance of this client.</param>
        protected JsonRpcClientConnection(JsonRpcServer rpcServer)
        {
            RpcServer = rpcServer;
            _tokenSource = new CancellationTokenSource();
        }

        /// <summary>
        /// Will run the connection process. Only execute this method once.
        /// </summary>
        public void Run()
        {
            RunTask(_tokenSource.Token).Forget();
        }

        /// <summary>
        /// The Task that will handle the connection. When successfully connected, this method
        /// will invoke <see cref="Connected"/>. After disconnecting will invoke
        /// <see cref="Disconnected"/>.
        /// </summary>
        /// <returns>UniTask.</returns>
        protected abstract UniTask RunTask(CancellationToken token);

        /// <summary>
        /// Abort this connection to a client and closes all open streams.
        /// </summary>
        public virtual void Abort()
        {
            _tokenSource.Cancel();
        }
    }
}
