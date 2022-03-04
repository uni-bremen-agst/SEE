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
using System.Threading;
using Cysharp.Threading.Tasks;
using StreamRpc;
using UnityEngine;

namespace SEE.Utils.RPC
{
    /// <summary>
    /// Represents a unique client connection. Contains the underlying <see cref="JsonRpc"/>
    /// instance to invoke methods in this specific client.
    /// </summary>
    public abstract class JsonRpcConnection
    {
        /// <summary>
        /// Represents the method that will handle the client connection events.
        /// </summary>
        /// <param name="connection">Connection that fired this event.</param>
        public delegate void ConnectionEventHandler(JsonRpcConnection connection);

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
        protected JsonRpcConnection()
        {
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
        /// Returns the task that will handle the connection. When successfully connected, this method
        /// will invoke <see cref="Connected"/>. After disconnecting will invoke
        /// <see cref="Disconnected"/>.
        /// </summary>
        /// <returns>UniTask.</returns>
        private async UniTask RunTask(CancellationToken token)
        {
            if (!InitiateJsonRpc())
            {
                return;
            }

            // To allow adding targets after starting
            Rpc.AllowModificationWhileListening = true;

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
            catch (InvalidOperationException e)
            {
                // listening hasn't been started
#if UNITY_EDITOR
                Debug.LogError($"{e.Message}\n");
#endif
            }

            Disconnected?.Invoke(this);
            Abort();
        }

        /// <summary>
        /// Adds a target, that will be called by the client. You can add multiple targets.
        /// </summary>
        /// <param name="target">An object that contains function that can be called
        /// remotely.</param>
        public void AddTarget(object target)
        {
            Rpc.AddLocalRpcTarget(target);
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
