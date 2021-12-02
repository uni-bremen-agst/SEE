using System;
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
        private readonly CancellationTokenSource tokenSource;

        /// <summary>
        /// Indicates if <see cref="Run"/> was already called.
        /// </summary>
        private bool started;

        /// <summary>
        /// Creates a new client connection.
        /// </summary>
        /// <param name="rpcServer">The server instance of this client.</param>
        protected JsonRpcClientConnection(JsonRpcServer rpcServer)
        {
            RpcServer = rpcServer;
            tokenSource = new CancellationTokenSource();
        }

        /// <summary>
        /// Gets the connection status of this connection.
        /// </summary>
        /// <returns>True if client is still connected to the server.</returns>
        public bool IsConnected()
        {
            return Rpc != null;
        }

        /// <summary>
        /// Will run the connection process. Only callable once.
        /// </summary>
        public void Run()
        {
            if (started) return;
            started = true;
            RunTask(tokenSource.Token).Forget();
        }

        /// <summary>
        /// The Task that will handle the connection. When successfully connected, this method
        /// will invoke <see cref="Connected"/>. After disconnecting will invoke
        /// <see cref="Disconnected"/>.
        /// </summary>
        /// <returns>UniTask.</returns>
        private async UniTask RunTask(CancellationToken token)
        {
            if (!InitiateJsonRpc()) return;

            Connected?.Invoke(this);

            try
            {
                await Rpc.Completion.AsUniTask().AttachExternalCancellation(token);
            }
            catch (OperationCanceledException)
            {
                Disconnected?.Invoke(this);
                throw;
            }
            catch (Exception)
            {
                // Connection was unexpectedly interrupted.
            }

            Disconnected?.Invoke(this);
            Abort();
        }

        /// <summary>
        /// Will initiate <see cref="Rpc"/> with the specific stream the derived class uses.
        /// </summary>
        protected abstract bool InitiateJsonRpc();

        /// <summary>
        /// Aborts this connection to a client and closes all open streams.
        /// </summary>
        public virtual void Abort()
        {
            tokenSource.Cancel();
            Rpc = null;
        }
    }
}
