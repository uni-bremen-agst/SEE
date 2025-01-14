using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol;
using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages;
using SEE.Controls;
using SEE.DataModel.DG;
using SEE.DataModel.DG.GraphIndex;
using SEE.Game.City;
using SEE.UI.Window.CodeWindow;
using SEE.UI.Window.ConsoleWindow;
using SEE.UI.Window.VariablesWindow;
using SEE.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Encoding = System.Text.Encoding;
using StackFrame = Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages.StackFrame;

namespace SEE.UI.DebugAdapterProtocol
{
    /// <summary>
    /// Manages a debug session.
    ///
    /// <para>
    /// Takes care of the following things:
    /// <list type="bullet">
    /// <item>
    /// <term>Actions</term>
    /// <description>Creates the button bar for start-, stop- and step-actions.</description>
    /// </item>
    /// <item>
    /// <term>Variables</term>
    /// <description>Shows variable values in the variable window and on hover.</description>
    /// </item>
    /// <item>
    /// <term>Sending/Receiving Events</term>
    /// <description>Handles sending and receiving information to and from the debug adapter.</description>
    /// </item>
    /// <item>
    /// <term>Console</term>
    /// <description>Displays program outputs in the console and evaluates user inputs.</description>
    /// </item>
    /// <item>
    /// <term>Code Position</term>
    /// <description>Shows the code position in the code window and the city.</description>
    /// </item>
    /// </list>
    /// </para>
    /// </summary>
    public partial class DebugAdapterProtocolSession : PlatformDependentComponent
    {
        /// <summary>
        /// Duration (seconds) of highlighting the code position in the city.
        /// </summary>
        private const float highlightDurationInitial = 3f;

        /// <summary>
        /// Duration (seconds) of repeated highlighting the code position in the city.
        /// Occurs when code stays in the same method.
        /// </summary>
        private const float highlightDurationRepeated = 2f;

        /// <summary>
        /// The prefab for the debug controls.
        /// </summary>
        private const string debugControlsPrefab = UIPrefabFolder + "DebugAdapterProtocolControls";

        /// <summary>
        /// The debug adapter.
        /// </summary>
        public DebugAdapter.DebugAdapter Adapter;

        /// <summary>
        /// Used for highlighting code position.
        /// <see cref="HighlightInCity(string, int)"/>
        /// </summary>
        public AbstractSEECity City;

        /// <summary>
        /// The stepping granularity.
        /// </summary>
        private SteppingGranularity? steppingGranularity = SteppingGranularity.Line;

        /// <summary>
        /// The debug controls.
        /// </summary>
        [ManagedUI]
        private GameObject controls;

        /// <summary>
        /// The process for the debug adapter host.
        /// </summary>
        private Process adapterProcess;

        /// <summary>
        /// The debug adapter host.
        /// </summary>
        private DebugProtocolHost adapterHost;

        /// <summary>
        /// The capabilities of the debug adapter.
        /// </summary>
        private InitializeResponse capabilities;

        /// <summary>
        /// The variables window.
        /// </summary>
        private VariablesWindow variablesWindow;

        /// <summary>
        /// The path of the last code position.
        /// </summary>
        private string lastCodePath;

        /// <summary>
        /// The line of the last code position.
        /// </summary>
        private int lastCodeLine;

        /// <summary>
        /// Last highlighted node.
        /// </summary>
        private Node lastHighlighted;

        /// <summary>
        /// The code window where the last code position was marked.
        /// Used for clearing previous marked line.
        /// </summary>
        private CodeWindow lastCodeWindow;

        /// <summary>
        /// Queued actions that are executed on the main thread.
        ///
        /// <para>
        /// Ensures that the actions are executed on the main thread and after the debug adapter is initialized.
        /// </para>
        /// <seealso cref="Update"/>
        /// </summary>
        private readonly Queue<Action> actions = new();

        /// <summary>
        /// Buffers the hovered word if the debuggee is currently running.
        /// </summary>
        private TMP_WordInfo? hoveredWord;

        /// <summary>
        /// The source range index to find graph element corresponding to code position.
        /// </summary>
        private SourceRangeIndex sourceRangeIndex;

        /// <summary>
        /// Whether the debug adapter is currently executing the program.
        /// </summary>
        private bool isRunning;

        /// <summary>
        /// Whether the debug adapter is currently executing the program.
        /// </summary>
        private bool IsRunning
        {
            get => isRunning;
            set
            {
                if (value == isRunning)
                {
                    return;
                }
                isRunning = value;
                if (value)
                {
                    actions.Enqueue(ClearLastCodePosition);
                }
                else
                {
                    actions.Enqueue(UpdateThreads);
                    actions.Enqueue(UpdateStackFrames);
                    actions.Enqueue(UpdateCodePosition);
                    actions.Enqueue(UpdateVariables);
                    actions.Enqueue(UpdateHoverTooltip);
                }
            }
        }

        /// <summary>
        /// The currently active threads.
        /// </summary>
        private List<Thread> threads = new();

        /// <summary>
        /// Returns the first active thread.
        /// </summary>
        private Thread MainThread => threads.FirstOrDefault();

        /// <summary>
        /// The currently active stack frames.
        /// </summary>
        private List<StackFrame> stackFrames = new();

        /// <summary>
        /// The current stack frame.
        /// </summary>
        private StackFrame StackFrame
        {
            get
            {
                if (IsRunning)
                {
                    Debug.LogError("StackFrame should only be retrieved while execution is paused.");
                }
                return stackFrames.FirstOrDefault();
            }
        }

        /// <summary>
        /// The variables.
        /// </summary>
        private Dictionary<Thread, Dictionary<StackFrame, Dictionary<Scope, List<Variable>>>> variables = new();

        /// <summary>
        /// Sets up the debug session.
        /// </summary>
        protected override void StartDesktop()
        {
            // creates UI elements
            OpenConsole(true);
            ConsoleWindow.OnInputSubmit += OnConsoleInput;
            SetupControls();
            CodeWindow.OnWordHoverBegin += OnWordHoverBegin;
            CodeWindow.OnWordHoverEnd += OnWordHoverEnd;
            if (City && City.LoadedGraph != null)
            {
                sourceRangeIndex = new SourceRangeIndex(City.LoadedGraph, node => node.Path());
            }

            if (Adapter == null)
            {
                OnInitializationFailed("Debug adapter not set.");
                return;
            }
            ConsoleWindow.AddMessage($"Start debugging session: {Adapter.Name} - {Adapter.AdapterFileName} - {Adapter.AdapterArguments} - {Adapter.AdapterWorkingDirectory}\n");

            // starts the debug adapter process
            if (!CreateAdapterProcess())
            {
                OnInitializationFailed("Couldn't create the debug adapter process.");
                return;
            }
            else
            {
                ConsoleWindow.AddMessage("Created the debug adapter process.\n", "Adapter", "Log");
            }
            // starts the debug adapter host
            if (!CreateAdapterHost())
            {
                OnInitializationFailed("Couldn't create the debug adapter host.");
                return;
            }
            else
            {
                ConsoleWindow.AddMessage("Created the debug adapter host.\n", "Adapter", "Log");
            }

            // adds breakpoint events
            DebugBreakpointManager.OnBreakpointAdded += OnBreakpointsChanged;
            DebugBreakpointManager.OnBreakpointRemoved += OnBreakpointsChanged;

            // sends the initialize request
            try
            {
                capabilities = adapterHost.SendRequestSync(new InitializeRequest()
                {
                    PathFormat = InitializeArguments.PathFormatValue.Path,
                    ClientID = "vscode",
                    ClientName = "Visual Studio Code",
                    AdapterID = Adapter.Name,
                    Locale = "en",
                    LinesStartAt1 = true,
                    ColumnsStartAt1 = true,
                });
                if (capabilities.SupportsSteppingGranularity != true)
                {
                    steppingGranularity = null;
                }
            }
            catch (Exception e)
            {
                OnInitializationFailed(e.Message);
            }
        }

        /// <summary>
        /// Helper method for when something went wrong during the initialization.
        /// </summary>
        /// <param name="message">The error message.</param>
        private void OnInitializationFailed(string message)
        {
            ConsoleWindow.AddMessage(message, "Adapter", "Error");
            Debug.LogError(message);
            Destroyer.Destroy(this);
        }

        /// <summary>
        /// Executes the queued actions on the main thread.
        /// Waits until the capabilities are known.
        /// </summary>
        protected override void Update()
        {
            if (capabilities != null)
            {
                while (actions.Count > 0)
                {
                    actions.Dequeue()();
                }
            }
        }

        /// <summary>
        /// Cleans up debug session.
        /// </summary>
        protected override void OnDestroy()
        {
            actions.Clear();
            threads.Clear();
            stackFrames.Clear();
            ClearLastCodePosition();

            ConsoleWindow.AddMessage("Debug session finished.\n");
            DebugBreakpointManager.OnBreakpointAdded -= OnBreakpointsChanged;
            DebugBreakpointManager.OnBreakpointRemoved -= OnBreakpointsChanged;
            ConsoleWindow.OnInputSubmit -= OnConsoleInput;
            CodeWindow.OnWordHoverBegin -= OnWordHoverBegin;
            CodeWindow.OnWordHoverEnd -= OnWordHoverEnd;
            if (adapterProcess is { HasExited: false })
            {
                adapterProcess.Kill();
            }
            if (adapterHost is { IsRunning: true })
            {
                adapterHost.Stop();
            }
            base.OnDestroy();
        }

        /// <summary>
        /// Creates the process for the debug adapter.
        /// </summary>
        /// <returns>Whether the creation was successful.</returns>
        private bool CreateAdapterProcess()
        {
            adapterProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = Adapter.AdapterFileName,
                    Arguments = Adapter.AdapterArguments,
                    WorkingDirectory = Adapter.AdapterWorkingDirectory,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    // message headers are ASCII, message bodies are UTF-8
                    StandardInputEncoding = Encoding.ASCII,
                    StandardOutputEncoding = Encoding.ASCII,
                    StandardErrorEncoding = Encoding.ASCII,
                },
                EnableRaisingEvents = true
            };
            adapterProcess.Exited += (_, _) => ConsoleWindow.AddMessage($"Process: Exited! ({(!adapterProcess.HasExited ? adapterProcess.ProcessName : null)})");
            adapterProcess.Disposed += (_, _) => ConsoleWindow.AddMessage($"Process: Exited! ({(!adapterProcess.HasExited ? adapterProcess.ProcessName : null)})");
            adapterProcess.OutputDataReceived += (_, args) => ConsoleWindow.AddMessage($"Process: OutputDataReceived! ({adapterProcess.ProcessName})\n\t{args.Data}");
            adapterProcess.ErrorDataReceived += (_, args) =>
            {
                string message = $"Process: ErrorDataReceived! ({adapterProcess.ProcessName})\n\t{args.Data}";
                ConsoleWindow.AddMessage(message + "\n", "Adapter", "Error");
                Debug.LogWarning(message);
            };

            string currentDirectory = Directory.GetCurrentDirectory();
            try
            {
                // working directory needs to be set manually so that executables can be found at relative paths
                Directory.SetCurrentDirectory(Adapter.AdapterWorkingDirectory);
                if (!adapterProcess.Start())
                {
                    adapterProcess = null;
                }
            }
            catch (Exception e)
            {
                ConsoleWindow.AddMessage(e.Message + "\n", "Adapter", "Error");
                Debug.LogWarning(e);
                adapterProcess = null;
            }
            // working directory needs to be reset (otherwise unity crashes)
            Directory.SetCurrentDirectory(currentDirectory);

            return adapterProcess != null && !adapterProcess.HasExited;
        }

        /// <summary>
        /// Creates the debug adapter host.
        /// </summary>
        /// <returns>Whether the creation was successful.</returns>
        private bool CreateAdapterHost()
        {
            adapterHost = new DebugProtocolHost(adapterProcess.StandardInput.BaseStream, adapterProcess.StandardOutput.BaseStream);
            adapterHost.DispatcherError += (_, args) =>
            {
                string message = $"DispatcherError - {args.Exception}";
                ConsoleWindow.AddMessage(message + "\n", "Adapter", "Error");
                Debug.LogWarning(message);
            };
            adapterHost.ResponseTimeThresholdExceeded += (_, args) =>
            {
                ConsoleWindow.AddMessage($"ResponseTimeThresholdExceeded - \t{args.Command}\t{args.SequenceId}\t{args.Threshold}\n", "Adapter", "Warning");
            };
            adapterHost.EventReceived += OnEventReceived;
            adapterHost.Run();

            return adapterHost.IsRunning;
        }

        /// <summary>
        /// Updates <see cref="threads"/>.
        /// Must be executed on the main thread.
        /// </summary>
        private void UpdateThreads()
        {
            if (IsRunning)
            {
                return;
            }

            threads = adapterHost.SendRequestSync(new ThreadsRequest()).Threads;
        }

        /// <summary>
        /// Updates the stack frames.
        /// Must be executed on the main thread.
        /// </summary>
        private void UpdateStackFrames()
        {
            if (IsRunning)
            {
                return;
            }

            stackFrames = adapterHost.SendRequestSync(new StackTraceRequest { ThreadId = MainThread.Id }).StackFrames;
        }


        /// <summary>
        /// Updates <see cref="variables"/>.
        /// Must be executed on the main thread.
        /// </summary>
        private void UpdateVariables()
        {
            if (IsRunning)
            {
                return;
            }
            variables = new();

            foreach (Thread thread in threads)
            {
                Dictionary<StackFrame, Dictionary<Scope, List<Variable>>> threadVariables = new();
                variables.Add(thread, threadVariables);

                foreach (StackFrame stackFrame in stackFrames)
                {
                    List<Scope> stackScopes = adapterHost.SendRequestSync(new ScopesRequest { FrameId = stackFrame.Id }).Scopes;
                    Dictionary<Scope, List<Variable>> stackVariables = stackScopes.ToDictionary(scope => scope, scope => RetrieveNestedVariables(scope.VariablesReference));
                    threadVariables.Add(stackFrame, stackVariables);
                }
            }
            variablesWindow ??= WindowSpaceManager.ManagerInstance[WindowSpaceManager.LocalPlayer].Windows.OfType<VariablesWindow>().FirstOrDefault();
            if (variablesWindow != null)
            {
                variablesWindow.RetrieveNestedVariables = RetrieveNestedVariables;
                variablesWindow.Variables = variables;
            }
        }

        /// <summary>
        /// Retrieves nested variables.
        /// Must be executed on the main thread.
        /// </summary>
        /// <param name="variablesReference">The variable reference.</param>
        /// <returns>The nested variables.</returns>
        private List<Variable> RetrieveNestedVariables(int variablesReference)
        {
            if (variablesReference <= 0 || IsRunning)
            {
                return new();
            }
            return adapterHost.SendRequestSync(new VariablesRequest { VariablesReference = variablesReference }).Variables;
        }

        /// <summary>
        /// Retrieves the value of a variable.
        /// Must be executed on the main thread.
        /// </summary>
        /// <param name="variable">The variable.</param>
        /// <returns>The variable value.</returns>
        private string RetrieveVariableValue(Variable variable)
        {
            if (IsRunning && variable.EvaluateName != null)
            {
                EvaluateResponse value = adapterHost.SendRequestSync(new EvaluateRequest
                {
                    Expression = variable.EvaluateName,
                    FrameId = IsRunning ? null : StackFrame.Id
                });
                return value.Result;
            }

            return variable.Value;
        }
    }
}
