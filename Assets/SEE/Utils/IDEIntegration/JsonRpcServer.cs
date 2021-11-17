using System;
using System.Threading;
using System.Threading.Tasks;
using StreamRpc;
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
        /// Represents the method that will handle the connection event.
        /// </summary>
        public delegate void ConnectionEventHandler();

        /// <summary>
        /// Will be fired when the connection is established successful.
        /// </summary>
        public ConnectionEventHandler Connected;

        /// <summary>
        /// Will be fired when the server disconnected from the client.
        /// </summary>
        public ConnectionEventHandler Disconnected;

        /// <summary>
        /// The JsonRpc instance for standardized communication over a stream.
        /// </summary>
        protected JsonRpc Rpc;

        /// <summary>
        /// Object with all methods that can be called remotely.
        /// </summary>
        protected object Target;

        /// <summary>
        /// Task to check if this class is already trying to connect.
        /// </summary>
        protected Task Server;

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
        }

        /// <summary>
        /// Starts listening to the TCP client.
        /// </summary>
        public void Start()
        {
            Dispose();

            Server ??= StartServerAsync(_sourceToken.Token);
        }

        /// <summary>
        /// Stops listening to the TCP client.
        /// </summary>
        public void Stop()
        {
            _sourceToken.Cancel();
            Dispose();

            Rpc = null;
            _sourceToken = new CancellationTokenSource();
            Disconnected?.Invoke();

        }

        /// <summary>
        /// Checks for Connection to client and starts remote call procedure.
        /// </summary>
        /// <param name="targetName">Method name.</param>
        /// <param name="arguments">Parameters of the called method.</param>
        /// <returns></returns>
        public async Task CallRemoteProcessAsync(string targetName, params object[] arguments)
        {
            if (IsConnected())
            {
                try
                {
                    await Rpc.InvokeAsync(targetName, arguments);
                }
                catch (Exception)
                {
                    // Lost connection to client.
                }
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
        /// Returns the connection status of the client.
        /// </summary>
        /// <returns>Is the Client connected?</returns>
        public bool IsConnected()
        {
            return Rpc != null;
        }

        /// <summary>
        /// Specific connection implementation of the derived class. This method
        /// should wait for client completion and call both <see cref="Connected"/>
        /// and <see cref="Disconnected"/> with the client.
        /// </summary>
        /// <param name="token">Token to cancel the current Task.</param>
        /// <returns>Async Task.</returns>
        protected abstract Task StartServerAsync(CancellationToken token);

        /// <summary>
        /// Dispose all open streams etc.
        /// </summary>
        public virtual void Dispose()
        {
            Rpc?.Dispose();
        }
    }
}
