// Copyright © 2022 Jan-Philipp Schramm
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS
// BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR
// IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace SEE.Utils.RPC
{
    /// <summary>
    /// The base class of the inter-process communication implementation for
    /// communication between an IDE and SEE.
    /// </summary>
    internal abstract class JsonRpcServer : IDisposable
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
        /// Represents the method that will handle the server connection events.
        /// </summary>
        /// <param name="connection">The connection that fired this event.</param>
        public delegate void ConnectionEventHandler(JsonRpcConnection connection);

        /// <summary>
        /// Will be fired when a client connection is established successful.
        /// </summary>
        public ConnectionEventHandler Connected;

        /// <summary>
        /// Will be fired when a client disconnected from the server.
        /// </summary>
        public ConnectionEventHandler Disconnected;

        /// <summary>
        /// All currently to the server connected clients. Only access this set while using
        /// <see cref="Semaphore"/>.
        /// </summary>
        protected readonly HashSet<JsonRpcConnection> RpcConnections;

        /// <summary>
        /// The semaphore used for accessing <see cref="RpcConnections"/>.
        /// </summary>
        protected SemaphoreSlim Semaphore;

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
        private CancellationTokenSource sourceToken;

        /// <summary>
        /// Sets <see cref="Target"/>, which represents the remote called functions.
        /// </summary>
        /// <param name="target">An object that contains function that can be called
        /// remotely. Null means no target will be added.</param>
        protected JsonRpcServer(object target)
        {
            Target = target;

            sourceToken = new CancellationTokenSource();
            Semaphore = new SemaphoreSlim(1, 1);
            RpcConnections = new HashSet<JsonRpcConnection>();
        }

        /// <summary>
        /// Starts listening to the TCP clients.
        /// </summary>
        /// <exception cref="JsonRpcServerCreationFailedException">A server instance couldn't be initiated.</exception>
        /// <param name="maxClients">The maximal number of clients that can connect to the server.</param>
        public async UniTask Start(uint maxClients)
        {
            if (Server.Status == UniTaskStatus.Pending)
            {
                return;
            }
            Dispose();
            Server = StartServerAsync(maxClients, sourceToken.Token);
            await Server;
        }

        /// <summary>
        /// Adds a connection to <see cref="RpcConnections"/> thread safe.
        /// </summary>
        /// <param name="connection">The client connection.</param>
        private void AddConnection(JsonRpcConnection connection)
        {
            UniTask.Run(async () =>
            {
                await Semaphore.WaitAsync();
                RpcConnections.Add(connection);
                Semaphore.Release();
                Connected?.Invoke(connection);
            }).Forget();
        }

        /// <summary>
        /// Removes a connection from <see cref="RpcConnections"/> thread safe.
        /// </summary>
        /// <param name="connection">The client connection.</param>
        private void RemoveConnection(JsonRpcConnection connection)
        {
            UniTask.Run(async () =>
            {
                await Semaphore.WaitAsync();
                RpcConnections.Remove(connection);
                Semaphore.Release();
                Disconnected?.Invoke(connection);
            }).Forget();
        }

        /// <summary>
        /// Will run the connection and register all necessary events.
        /// </summary>
        /// <param name="connection">The client connection.</param>
        protected void RunConnection(JsonRpcConnection connection)
        {
            connection.Connected += AddConnection;
            connection.Disconnected += RemoveConnection;
            connection.Run();
            if (Target != null)
            {
                connection.AddTarget(Target);
            }
        }

        /// <summary>
        /// Checks for Connection to client and starts remote call procedure.
        /// </summary>
        /// <param name="targetName">Method name.</param>
        /// <param name="arguments">Parameters of the called method.</param>
        public async UniTask CallRemoteProcessAsync(string targetName, params object[] arguments)
        {
            if (IsConnected())
            {
                await Semaphore.WaitAsync();
                foreach (JsonRpcConnection connection in RpcConnections)
                {
                    await CallRemoteProcessOnConnectionAsync(connection, targetName, arguments);
                }
                Semaphore.Release();
            }
            else
            {
#if UNITY_EDITOR
                Debug.LogWarning($"JsonRpcServer not connected to client! Couldn't call '{targetName}'.\n");
#endif
            }
        }

        /// <summary>
        /// Use this method if you want to call a remote process on a specific connection.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="targetName">Method name.</param>
        /// <param name="arguments">Parameters of the called method.</param>
        public async UniTask CallRemoteProcessOnConnectionAsync(JsonRpcConnection connection,
            string targetName, params object[] arguments)
        {
            await CallRemoteProcessOnConnectionAsync<object>(connection, targetName, arguments);
        }

        /// <summary>
        /// Use this method if you want to call a remote process on a specific connection.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="targetName">Method name.</param>
        /// <param name="arguments">Parameters of the called method.</param>
        /// <returns>default UniTask</returns>
        public async UniTask<T> CallRemoteProcessOnConnectionAsync<T>(JsonRpcConnection connection,
            string targetName, params object[] arguments)
        {
            if (connection != null)
            {
                try
                {
                    if (connection.Rpc == null) return default;
                    return await connection.Rpc.InvokeAsync<T>(targetName, arguments);
                }
                catch (Exception e)
                {
#if UNITY_EDITOR
                    Debug.LogWarning($"{e.Message}\n");
#endif
                }
            }
            else
            {
#if UNITY_EDITOR
                Debug.LogWarning($"Connection is null! Couldn't call '{targetName}'.\n");
#endif
            }
            return default;
        }

        /// <summary>
        /// Check if a client is currently connected to the server.
        /// </summary>
        /// <returns>True when a client is connected to server.</returns>
        private bool IsConnected()
        {
            return RpcConnections != null && RpcConnections.Count > 0;
        }

        /// <summary>
        /// Specific connection implementation of the derived class. When a connection to a client
        /// could be established, this method should call <see cref="RunConnection"/> instead of
        /// calling <see cref="JsonRpcConnection.Run"/>.
        /// </summary>
        /// <param name="maxClients">The maximal number of clients that can connect to the server.</param>
        /// <param name="token">Token to cancel the current Task.</param>
        /// <exception cref="JsonRpcServerCreationFailedException">A server instance couldn't be initiated.</exception>
        /// <returns>Async Task.</returns>
        protected abstract UniTask StartServerAsync(uint maxClients, CancellationToken token);

        /// <summary>
        /// Disposes all open streams and stops the server.
        /// </summary>
        public virtual void Dispose()
        {
            sourceToken.Cancel();
            if (RpcConnections != null)
            {
                Semaphore.Wait();
                foreach (JsonRpcConnection connection in RpcConnections)
                {
                    connection?.Abort();
                }

                Semaphore.Release();
            }

            sourceToken = new CancellationTokenSource();
        }
    }
}
