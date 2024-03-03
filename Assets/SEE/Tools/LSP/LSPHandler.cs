using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using OmniSharp.Extensions.JsonRpc.Server;
using OmniSharp.Extensions.LanguageServer.Client;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.General;
using SEE.UI;
using SEE.Utils;
using UnityEngine;

namespace SEE.Tools.LSP
{
    /// <summary>
    /// Handles the language server process.
    ///
    /// This class is responsible for starting and stopping the language server, and is intended
    /// to be the primary interface for other classes to communicate with the language server.
    /// </summary>
    public class LSPHandler : MonoBehaviour
    {
        /// <summary>
        /// The language server to be used.
        /// </summary>
        private LSPServer server;

        /// <summary>
        /// The language server to be used.
        /// This property has to be set—and can only be set—before the language server is started.
        /// </summary>
        public LSPServer Server
        {
            get => server;
            set
            {
                if (IsReady)
                {
                    throw new InvalidOperationException("Cannot change the LSP server while it is still running.\n");
                }
                server = value;
            }
        }

        /// <summary>
        /// The path to the project to be analyzed.
        /// </summary>
        private string projectPath;

        /// <summary>
        /// The path to the project to be analyzed.
        /// </summary>
        public string ProjectPath
        {
            get => projectPath;
            set
            {
                if (IsReady)
                {
                    // TODO: Is this true or do we just have to call some LSP method?
                    throw new InvalidOperationException("Cannot change the project path while the LSP server is still running.\n");
                }
                projectPath = value;
            }
        }

        /// <summary>
        /// Whether to log the communication between the language server and SEE to a temporary file.
        /// </summary>
        public bool LogLSP { get; set; }

        /// <summary>
        /// The language client that is used to communicate with the language server.
        /// </summary>
        public LanguageClient Client { get; private set; }

        /// <summary>
        /// The process that runs the language server.
        /// </summary>
        private Process lspProcess;

        /// <summary>
        /// The cancellation token source used for asynchronous operations.
        /// </summary>
        private CancellationTokenSource cancellationTokenSource = new();

        /// <summary>
        /// The cancellation token used for asynchronous operations.
        /// </summary>
        private CancellationToken cancellationToken => cancellationTokenSource.Token;

        /// <summary>
        /// A semaphore to ensure that nothing interferes with the language server while it is starting or stopping.
        /// </summary>
        private readonly SemaphoreSlim semaphore = new(1, 1);

        /// <summary>
        /// Whether the language server is ready to process requests.
        /// </summary>
        public bool IsReady { get; private set; }

        private void OnEnable()
        {
            InitializeAsync().Forget();
        }

        private void OnDisable()
        {
            ShutdownAsync().Forget();
        }

        /// <summary>
        /// Waits asynchronously until the language server is ready to process requests.
        /// </summary>
        public async UniTask WaitUntilReadyAsync()
        {
            await UniTask.WaitUntil(() => IsReady, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Initializes the language server such that it is ready to process requests.
        /// </summary>
        public async UniTask InitializeAsync()
        {
            if (Server == null)
            {
                throw new InvalidOperationException("LSP server must be set before initializing the handler.\n");
            }
            await semaphore.WaitAsync(cancellationToken);
            if (IsReady)
            {
                // LSP server is already running
                semaphore.Release();
                return;
            }

            IDisposable spinner = LoadingSpinner.Show("Initializing language server...");
            try
            {
                // TODO: Check for executable (at relevant locations?) first, and if not there, direct users
                //       for info on how to install it.
                ProcessStartInfo startInfo = new(fileName: Server.ServerExecutable, arguments: Server.Parameters)
                {
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                };
                lspProcess = Process.Start(startInfo);
                if (lspProcess == null)
                {
                    throw new InvalidOperationException("Failed to start the language server.\n");
                }

                Stream outputLog = Stream.Null;
                Stream inputLog = Stream.Null;
                if (LogLSP)
                {
                    string tempDir = Path.GetTempPath();
                    outputLog = new FileStream(Path.Combine(tempDir, "outputLogLsp.txt"), FileMode.Create, FileAccess.Write, FileShare.Read);
                    inputLog = new FileStream(Path.Combine(tempDir, "inputLogLsp.txt"), FileMode.Create, FileAccess.Write, FileShare.Read);
                }

                TeeStream teedInputStream = new(lspProcess.StandardOutput.BaseStream, outputLog);
                TeeStream teedOutputStream = new(lspProcess.StandardInput.BaseStream, inputLog);

                // TODO: Add other capabilities here
                DocumentSymbolCapability symbolCapabilities = new()
                {
                    HierarchicalDocumentSymbolSupport = true
                };
                Client = LanguageClient.Create(options => options.WithInput(teedInputStream)
                                                                 .WithOutput(teedOutputStream)
                                                                 // TODO: Path
                                                                 .WithRootPath(ProjectPath)
                                                                 // Log output
                                                                 .WithCapability(symbolCapabilities));
                await Client.Initialize(cancellationToken);
                // FIXME: We need to wait a certain amount until the files are indexed.
                //        Use progress notifications for this instead of this hack.
                await UniTask.Delay(TimeSpan.FromSeconds(5), cancellationToken: cancellationToken);
                IsReady = true;
            }
            finally
            {
                semaphore.Release();
                spinner.Dispose();
            }
        }

        /// <summary>
        /// Shuts down the language server and exits its process.
        ///
        /// After this method is called, the language server is no longer
        /// ready to process requests until it is initialized again.
        /// </summary>
        public async UniTask ShutdownAsync()
        {
            cancellationTokenSource.Cancel();
            cancellationTokenSource.Dispose();
            cancellationTokenSource = new CancellationTokenSource();

            await semaphore.WaitAsync(cancellationToken);
            if (!IsReady)
            {
                // LSP server is not running.
                return;
            }

            IDisposable spinner = LoadingSpinner.Show("Shutting down language server...");
            try
            {
                await Client.Shutdown();
            }
            catch (InvalidParametersException)
            {
                // Some language servers (e.g., rust-analyzer) have trouble with OmniSharp's empty map.
                // They throw an InvalidParameterException, which we can ignore for now.
            }
            finally
            {
                // In case Client.SendExit() fails, we release the semaphore and resources first to avoid a deadlock.
                IsReady = false;
                semaphore.Release();
                spinner.Dispose();

                Client.SendExit();
            }
        }
    }
}
