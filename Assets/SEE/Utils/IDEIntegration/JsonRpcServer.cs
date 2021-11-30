using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace SEE.Utils
{
    /// <summary>
    /// The Base class of the inter-process communication implementation for
    /// communication between an IDE and SEE.
    /// </summary>
    public abstract class JsonRpcServer : IDisposable
    {
        /// <summary>
        /// This exception will be thrown, if creation of the server instance failed.
        /// </summary>
        public class JsonRpcServerCreationFailedException : Exception
        {
            /// <summary>
            /// Exception with custom message when creation of server instance failed.
            /// </summary>
            /// <param name="message">Custom message.</param>
            internal JsonRpcServerCreationFailedException(string message) : base(message)
            {
            }
        }

        /// <summary>
        /// Will be fired when a client connection is established successful.
        /// </summary>
        public EventHandler Connected;

        /// <summary>
        /// Will be fired when a client disconnected from the server.
        /// </summary>
        public EventHandler Disconnected;

        /// <summary>
        /// All currently to the server connected clients. Only access this set while using
        /// <see cref="Semaphore"/>.
        /// </summary>
        protected readonly HashSet<JsonRpcClientConnection> RpcConnections;

        /// <summary>
        /// The semaphore used for accessing <see cref="RpcConnections"/>.
        /// </summary>
        protected Semaphore Semaphore;

        /// <summary>
        /// Object with all methods that can be called remotely.
        /// </summary>
        protected internal object Target;

        /// <summary>
        /// Task to check if this class is already trying to connect.
        /// </summary>
        protected UniTask Server;

        /// <summary>
        /// CancellationTokenSource to stop the current server instance.
        /// </summary>
        private CancellationTokenSource _sourceToken;

        /// <summary>
        /// Sets <see cref="Target"/>, which represents the remote called functions.
        /// </summary>
        /// <param name="target">An object that contains function that can be called
        /// remotely.</param>
        protected JsonRpcServer(object target)
        {
            _ = target ?? throw new ArgumentNullException(nameof(target));
            Target = target;

            _sourceToken = new CancellationTokenSource();
            Semaphore = new Semaphore(1, 1);
            RpcConnections = new HashSet<JsonRpcClientConnection>();
        }

        /// <summary>
        /// Starts listening to the TCP client.
        /// </summary>
        /// <exception cref="JsonRpcServerCreationFailedException">A server instance couldn't be initiated.</exception>
        /// <param name="maxClients">The maximal number of clients that can connect to the server.</param>
        /// <returns>Async UniTask.</returns>
        public async UniTask Start(int maxClients)
        {
            if (Server.Status == UniTaskStatus.Pending) return;
            Dispose();
            Server = StartServerAsync(maxClients, _sourceToken.Token);
            await Server;
        }

        /// <summary>
        /// Stops listening to the TCP client.
        /// </summary>
        public void Stop()
        {
            // TODO: Should send custom event arguments.
            if (IsConnected()) Disconnected?.Invoke(this, EventArgs.Empty);
            Dispose();
        }

        /// <summary>
        /// Adds a connection to <see cref="RpcConnections"/> thread safe.
        /// </summary>
        /// <param name="connection">The client connection.</param>
        private void AddConnection(JsonRpcClientConnection connection)
        {
            Semaphore.WaitOne();
            RpcConnections.Add(connection);
            Semaphore.Release();
            Connected?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Removes a connection from <see cref="RpcConnections"/> thread safe.
        /// </summary>
        /// <param name="connection">The client connection.</param>
        private void RemoveConnection(JsonRpcClientConnection connection)
        {
            Semaphore.WaitOne();
            RpcConnections.Remove(connection);
            Semaphore.Release();
            Disconnected?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Will run the connection and register all necessary events.
        /// </summary>
        /// <param name="connection">The client connection.</param>
        protected void RunConnection(JsonRpcClientConnection connection)
        {
            connection.Connected += AddConnection;
            connection.Disconnected += RemoveConnection;
            connection.Run();
        }

        /// <summary>
        /// Checks for Connection to client and starts remote call procedure.
        /// </summary>
        /// <param name="targetName">Method name.</param>
        /// <param name="arguments">Parameters of the called method.</param>
        /// <returns></returns>
        public async UniTask CallRemoteProcessAsync(string targetName, params object[] arguments)
        {
            if (IsConnected())
            {
                Semaphore.WaitOne();
                foreach (var connection in RpcConnections)
                {
                    try
                    {
                        if (connection.Rpc == null) continue;
                        await connection.Rpc.InvokeAsync(targetName, arguments).AsUniTask();

                    }
                    catch (Exception)
                    {
                        // Lost connection to client.
                    }
                }
                Semaphore.Release();
            }
            else
            {
#if UNITY_EDITOR
                Debug.LogWarning("JsonRpcServer not connected to client!" +
                                 $" Couldn't call '{targetName}'.");
#endif
            }
        }

        /// <summary>
        /// Check if a client is currently connected to the server.
        /// </summary>
        /// <returns>True when a client is connected to server.</returns>
        public bool IsConnected()
        {
            return RpcConnections != null && RpcConnections.Count > 0;
        }

        /// <summary>
        /// Specific connection implementation of the derived class. When a connection to a client
        /// could be established, this method should call <see cref="RunConnection"/> instead of
        /// calling <see cref="JsonRpcClientConnection.Run"/>.
        /// </summary>
        /// <param name="maxClients">The maximal number of clients that can connect to the server.</param>
        /// <param name="token">Token to cancel the current Task.</param>
        /// <exception cref="JsonRpcServerCreationFailedException">A server instance couldn't be initiated.</exception>
        /// <returns>Async Task.</returns>
        protected abstract UniTask StartServerAsync(int maxClients, CancellationToken token);

        /// <summary>
        /// Dispose all open streams etc. Will not call <see cref="Disconnected"/>, call
        /// <see cref="Stop"/> instead.
        /// </summary>
        public virtual void Dispose()
        {
            _sourceToken.Cancel();
            if (RpcConnections != null)
            {
                Semaphore.WaitOne();
                foreach (var connection in RpcConnections)
                {
                    connection?.Abort();
                }

                Semaphore.Release();
            }
            
            _sourceToken = new CancellationTokenSource();
        }
    }
}
