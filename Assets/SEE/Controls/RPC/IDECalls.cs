using Cysharp.Threading.Tasks;
using SEE.Utils;
using UnityEngine;

namespace SEE.Controls
{
    /// <summary>
    /// This script establishes the connection to an IDE of choice. There is the
    /// option to choose between all possible IDE implementations. Currently,
    /// only Visual Studio is supported, but could be easily extended in the
    /// future.
    /// Note: Only one instance of this class can be created.
    /// </summary>
    public partial class IDEIntegration : MonoBehaviour
    {
        /// <summary>
        /// Lists all methods remotely callable by the server in a convenient way.
        /// </summary>
        private class IDECalls
        {
            /// <summary>
            /// The server instance.
            /// </summary>
            private readonly JsonRpcServer server;

            /// <summary>
            /// Provides all method SEE can invoke on an IDE.
            /// </summary>
            /// <param name="server">The server instance.</param>
            public IDECalls(JsonRpcServer server)
            {
                this.server = server;
            }

            /// <summary>
            /// Opens the file in the IDE of choice.
            /// </summary>
            /// <param name="connection">A connection to an IDE.</param>
            /// <param name="path">Absolute file path.</param>
            /// <param name="line">Optional line number.</param>
            public async UniTask OpenFile(JsonRpcConnection connection, string path, int? line)
            {
                await server.CallRemoteProcessOnConnectionAsync(connection, "OpenFile", path, line);
            }

            /// <summary>
            /// Gets the absolute project path (e.g. .sln). 
            /// </summary>
            /// <param name="connection">A connection to an IDE.</param>
            /// <returns>Returns the absolute project path. Can be null.</returns>
            public async UniTask<string> GetProjectPath(JsonRpcConnection connection)
            {
                return await server.CallRemoteProcessOnConnectionAsync<string>(connection, "GetProject");
            }

            /// <summary>
            /// Gets the absolute project path (e.g. .sln). 
            /// </summary>
            /// <param name="connection">A connection to an IDE.</param>
            /// <returns>Returns the absolute project path. Can be null.</returns>
            public async UniTask<string> GetIDEVersion(JsonRpcConnection connection)
            {
                return await server.CallRemoteProcessOnConnectionAsync<string>(connection, "GetIdeVersion");
            }

            /// <summary>
            /// Was the connection started by SEE directly through a command switch.
            /// </summary>
            /// <param name="connection">A connection to an IDE.</param>
            /// <returns>True if SEE started this connection.</returns>
            public async UniTask<bool> WasStartedBySee(JsonRpcConnection connection)
            {
                return await server.CallRemoteProcessOnConnectionAsync<bool>(connection, "WasStartedBySee");
            }

            /// <summary>
            /// Will focus this IDE instance.
            /// </summary>
            /// <param name="connection">A connection to an IDE.</param>
            public async UniTask FocusIDE(JsonRpcConnection connection)
            {
                await server.CallRemoteProcessOnConnectionAsync(connection, "SetFocus");
            }

            /// <summary>
            /// Calling this method will change the loaded solution of this Connection.
            /// </summary>
            /// <param name="connection">A connection to an IDE.</param>
            /// <param name="path">The absolute solution path.</param>
            public async UniTask ChangeSolution(JsonRpcConnection connection, string path)
            {
                await server.CallRemoteProcessOnConnectionAsync(connection, "", path);
            }

            /// <summary>
            /// Declines an IDE instance.
            /// </summary>
            /// <param name="connection">A connection to an IDE.</param>
            public async UniTask Decline(JsonRpcConnection connection)
            {
                await server.CallRemoteProcessOnConnectionAsync(connection, "Decline");
            }
        }
    }
}