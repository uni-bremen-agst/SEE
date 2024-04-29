using Michsky.UI.ModernUIPack;
using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol;
using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages;
using SEE.Controls;
using SEE.DataModel.DG;
using SEE.DataModel.DG.GraphIndex;
using SEE.Game;
using SEE.Game.City;
using SEE.GO;
using SEE.UI.Window;
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
    public class DebugAdapterProtocolSession : PlatformDependentComponent
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
        /// The debug adapter.
        /// </summary>
        public DebugAdapter.DebugAdapter Adapter;

        /// <summary>
        /// Used for highlighing code position.
        /// <see cref="HighlightInCity(string, int)"/>
        /// </summary>
        public AbstractSEECity City;

        /// <summary>
        /// The prefab for the debug controls.
        /// </summary>
        private const string DebugControlsPrefab = UIPrefabFolder + "DebugAdapterProtocolControls";

        /// <summary>
        /// The stepping granularity.
        /// </summary>
        private SteppingGranularity? steppingGranularity = SteppingGranularity.Line;

        /// <summary>
        /// The debug controls.
        /// </summary>
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
        /// The tooltip.
        /// </summary>
        private Tooltip.Tooltip tooltip;

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
        private Queue<Action> actions = new();

        /// <summary>
        /// Buffers the hovered word if the debuggee is currently running.
        /// </summary>
        private TMP_WordInfo? hoveredWord;

        /// <summary>
        /// Whether the debug adapter is currently executing the program.
        /// </summary>
        private bool isRunning;

        /// <summary>
        /// The source range index to find graph element corresponding to code position.
        /// </summary>
        private SourceRangeIndex sourceRangeIndex;

        /// <summary>
        /// Whether the debug adapter is currently executing the program.
        /// </summary>
        private bool IsRunning
        {
            get => isRunning;
            set
            {
                if (value == isRunning) return;
                isRunning = value;
                if (value)
                {
                    actions.Enqueue(ClearLastCodePosition);
                } else
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
        private Thread mainThread => threads.FirstOrDefault();

        /// <summary>
        /// The currently active stack frames.
        /// </summary>
        private List<StackFrame> stackFrames = new();

        /// <summary>
        /// The current stack frame.
        /// </summary>
        private StackFrame stackFrame{
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

        #region General
        /// <summary>
        /// Sets up the debug session.
        /// </summary>
        protected override void StartDesktop()
        {
            // creates ui elements
            tooltip = gameObject.AddComponent<Tooltip.Tooltip>();
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
                string message = "Debug adapter not set.";
                ConsoleWindow.AddMessage(message, "Adapter", "Error");
                Debug.LogWarning(message);
                Destroyer.Destroy(this);
                return;
            }
            ConsoleWindow.AddMessage($"Start debugging session: {Adapter.Name} - {Adapter.AdapterFileName} - {Adapter.AdapterArguments} - {Adapter.AdapterWorkingDirectory}\n");

            // starts the debug adapter process
            if (!CreateAdapterProcess())
            {
                string message = "Couldn't create the debug adapter process.";
                ConsoleWindow.AddMessage(message, "Adapter", "Error");
                Debug.LogWarning(message);
                Destroyer.Destroy(this);
                return;
            }
            else
            {
                ConsoleWindow.AddMessage("Created the debug adapter process.\n", "Adapter", "Log");
            }
            // starts the debug adapter host
            if (!CreateAdapterHost())
            {
                string message = "Couldn't create the debug adapter host.";
                ConsoleWindow.AddMessage(message, "Adapter", "Error");
                Debug.LogWarning(message);
                Destroyer.Destroy(this);
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
                ConsoleWindow.AddMessage(e.Message + "\n", "Adapter", "Error");
                Debug.LogWarning(e);
                Destroyer.Destroy(this);
            }
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
        private void OnDestroy()
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
            if (controls)
            {
                Destroyer.Destroy(controls);
            }
            if (tooltip)
            {
                Destroyer.Destroy(tooltip);
            }
            if (adapterProcess != null && !adapterProcess.HasExited)
            {
                adapterProcess.Kill();
            }
            if (adapterHost != null && adapterHost.IsRunning)
            {
                adapterHost.Stop();
            }
        }
        #endregion

        #region Setup
        /// <summary>
        /// Sets up the debug controls.
        /// Hides controls which can't be used.
        /// </summary>
        private void SetupControls()
        {
            controls = PrefabInstantiator.InstantiatePrefab(DebugControlsPrefab, transform, false);
            controls.transform.position = new Vector3(Screen.width * 0.5f, Screen.height * 0.9f, 0);

            actions.Enqueue(() =>
            {
                Dictionary<string, (Action, string)> listeners = new()
                {
                    {"Continue", (OnContinue, "Continue")},
                    {"Pause", (OnPause, "Pause")},
                    {"Reverse", (OnReverseContinue, "Reverse")},
                    {"StepOver", (OnNext, "Step Over")},
                    {"StepBack", (OnStepBack, "Step Back")},
                    {"StepIn", (OnStepIn, "Step In")},
                    {"StepOut", (OnStepOut, "Step Out")},
                    {"Restart", (OnRestart, "Restart")},
                    {"Stop", (OnStop , "Stop")},
                    {"Console", (() => OpenConsole(), "Console")},
                    {"Variables",  (OpenVariables, "Variables")},
                    {"CodePosition", (() => ShowCodePosition(true, true, highlightDurationInitial), "Code Position") }
                };
                foreach (var (name, (action, description)) in listeners)
                {
                    GameObject button = controls.transform.Find(name).gameObject;
                    button.MustGetComponent<ButtonManagerBasic>().clickEvent.AddListener(() => action());
                    if (button.TryGetComponentOrLog(out PointerHelper pointerHelper))
                    {
                        pointerHelper.EnterEvent.AddListener(_ => tooltip.Show(description));
                        pointerHelper.ExitEvent.AddListener(_ => tooltip.Hide());
                    }
                }
                UpdateVisibility();
                adapterHost.EventReceived += (object sender, EventReceivedEventArgs e) =>
                {
                    if (e.Body is CapabilitiesEvent)
                    {
                        UpdateVisibility();
                    }
                };

                void UpdateVisibility()
                {
                    Dictionary<string, bool?> activeButtons = new()
                    {
                        {"Reverse", capabilities.SupportsStepBack },
                        {"StepBack", capabilities.SupportsStepBack },
                        {"Restart", capabilities.SupportsRestartRequest },
                    };
                    foreach (var (name, active) in activeButtons)
                    {
                        controls.transform.Find(name).gameObject.SetActive(active == true);
                    }
                }
            });
        }

        /// <summary>
        /// Opens up the console.
        /// Can be called again to reopen or focus the console.
        /// </summary>
        private void OpenConsole(bool start = false)
        {
            WindowSpace manager = WindowSpaceManager.ManagerInstance[WindowSpaceManager.LocalPlayer];
            ConsoleWindow console = manager.Windows.OfType<ConsoleWindow>().FirstOrDefault();
            if (console == null)
            {
                console = Canvas.AddComponent<ConsoleWindow>();
                ConsoleWindow.DefaultChannel = "Adapter";
                ConsoleWindow.DefaultChannelLevel = "Log";
                manager.AddWindow(console);
                if (start)
                {
                    foreach ((string channel, char icon) in new[] { ("Program", '\uf135'), ("Adapter", '\uf188') })
                    {
                        ConsoleWindow.AddChannel(channel, icon);
                        foreach ((string level, Color color) in new[] { ("Log", Color.gray), ("Warning", Color.yellow.Darker()), ("Error", Color.red.Darker()) })
                        {
                            ConsoleWindow.AddChannelLevel(channel, level, color);
                        }
                    }
                    ConsoleWindow.SetChannelLevelEnabled("Adapter", "Log", false);
                }
            }
            manager.ActiveWindow = console;
        }

        /// <summary>
        /// Opens the variables window.
        /// Can be recalled to reopen or focus the console.
        /// </summary>
        private void OpenVariables()
        {
            WindowSpace manager = WindowSpaceManager.ManagerInstance[WindowSpaceManager.LocalPlayer];
            if (variablesWindow == null)
            {
                variablesWindow = manager.Windows.OfType<VariablesWindow>().FirstOrDefault() ?? Canvas.AddComponent<VariablesWindow>();
                variablesWindow.Variables = variables;
                variablesWindow.RetrieveNestedVariables = RetrieveNestedVariables;
                variablesWindow.RetrieveVariableValue = RetrieveVariableValue;
                manager.AddWindow(variablesWindow);
            }

            manager.ActiveWindow = variablesWindow;
        }


        /// <summary>
        /// Creates the process for the debug adapter.
        /// </summary>
        /// <returns>Whether the creation was sucessful.</returns>
        private bool CreateAdapterProcess()
        {
            adapterProcess = new Process()
            {
                StartInfo = new ProcessStartInfo()
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
                }
            };
            adapterProcess.EnableRaisingEvents = true;
            adapterProcess.Exited += (_, args) => ConsoleWindow.AddMessage($"Process: Exited! ({(!adapterProcess.HasExited ? adapterProcess.ProcessName : null)})");
            adapterProcess.Disposed += (_, args) => ConsoleWindow.AddMessage($"Process: Exited! ({(!adapterProcess.HasExited ? adapterProcess.ProcessName : null)})");
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
            adapterHost.DispatcherError += (sender, args) =>
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
        #endregion

        #region Events
        /// <summary>
        /// Updates the breakpoints.
        /// </summary>
        /// <param name="path">The source code path.</param>
        /// <param name="line">The code line.</param>
        private void OnBreakpointsChanged(string path, int line)
        {
            actions.Enqueue(() =>
            {
                adapterHost.SendRequest(new SetBreakpointsRequest()
                {
                    Source = new Source() { Path = path, Name = path },
                    Breakpoints = DebugBreakpointManager.Breakpoints[path].Values.ToList(),
                }, _ => {});
            });
        }

        /// <summary>
        /// Handles the begin of hovering a word.
        /// </summary>
        /// <param name="codeWindow"></param>
        /// <param name="wordInfo"></param>
        private void OnWordHoverBegin(CodeWindow codeWindow, TMP_WordInfo wordInfo)
        {
            hoveredWord = wordInfo;
            actions.Enqueue(UpdateHoverTooltip);
        }

        /// <summary>
        /// Handles the end of hovering a word.
        /// </summary>
        /// <param name="codeWindow"></param>
        /// <param name="wordInfo"></param>
        private void OnWordHoverEnd(CodeWindow codeWindow, TMP_WordInfo wordInfo)
        {
            hoveredWord = null;
            tooltip.Hide();
        }

        /// <summary>
        /// Evaluates the hovered word.
        /// Only allowed on the main thread.
        /// </summary>
        private void UpdateHoverTooltip()
        {
            if (hoveredWord is null || IsRunning) return;

            string expression = ((TMP_WordInfo)hoveredWord).GetWord();

            try
            {
                EvaluateResponse result = adapterHost.SendRequestSync(new EvaluateRequest()
                {
                    Expression = expression,
                    Context = capabilities.SupportsEvaluateForHovers == true ? EvaluateArguments.ContextValue.Hover : null,
                    FrameId = stackFrame.Id
                });
                tooltip.Show(result.Result, 0.25f);
            } catch (ProtocolException)
            {
                // Ignore exceptions on hover
            }

        }

        /// <summary>
        /// Handles events of the debug adapter.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event.</param>
        private void OnEventReceived(object sender, EventReceivedEventArgs e)
        {
            switch (e.Body)
            {
                case InitializedEvent initializedEvent:
                    OnInitializedEvent(initializedEvent);
                    break;
                case OutputEvent outputEvent:
                    OnOutputEvent(outputEvent);
                    break;
                case TerminatedEvent terminatedEvent:
                    OnTerminatedEvent(terminatedEvent);
                    break;
                case ExitedEvent exitedEvent:
                    OnExitedEvent(exitedEvent);
                    break;
                case StoppedEvent stoppedEvent:
                    OnStoppedEvent(stoppedEvent);
                    break;
                case ThreadEvent threadEvent:
                    OnThreadEvent(threadEvent);
                    break;
                case ContinuedEvent continuedEvent:
                    OnContinuedEvent(continuedEvent);
                    break;
                case CapabilitiesEvent capabilitiesEvent:
                    OnCapabilitiesEvent(capabilitiesEvent);
                    break;
            }
        }

        /// <summary>
        /// Handles the initialized event.
        /// </summary>
        /// <param name="initializedEvent">The event.</param>
        private void OnInitializedEvent(InitializedEvent initializedEvent)
        {
            actions.Enqueue(() =>
            {
                adapterHost.SendRequest(Adapter.GetLaunchRequest(), _ => IsRunning = true);
                foreach ((string path, Dictionary<int, SourceBreakpoint> breakpoints) in DebugBreakpointManager.Breakpoints)
                {
                    adapterHost.SendRequest(new SetBreakpointsRequest()
                    {
                        Source = new Source() { Path = path, Name = path },
                        Breakpoints = breakpoints.Values.ToList(),
                    }, _ => {});
                }
                adapterHost.SendRequest(new SetFunctionBreakpointsRequest() { Breakpoints = new() }, _ => { });
                adapterHost.SendRequest(new SetExceptionBreakpointsRequest() { Filters = new() }, _ => { });
                if (capabilities.SupportsConfigurationDoneRequest == true)
                {
                    adapterHost.SendRequest(new ConfigurationDoneRequest(), _ => { });
                }
            });
        }

        /// <summary>
        /// Handles output events.
        /// </summary>
        /// <param name="outputEvent">The event.</param>
        private void OnOutputEvent(OutputEvent outputEvent)
        {
            string channel = outputEvent.Category switch
            {
                OutputEvent.CategoryValue.Console => "Adapter",
                OutputEvent.CategoryValue.Stdout => "Program",
                OutputEvent.CategoryValue.Stderr => "Program",
                OutputEvent.CategoryValue.Telemetry => null,
                OutputEvent.CategoryValue.MessageBox => "Adapter",
                OutputEvent.CategoryValue.Exception => "Adapter",
                OutputEvent.CategoryValue.Important => "Adapter",
                OutputEvent.CategoryValue.Unknown => "Adapter",
                null => "Adapter",
                _ => "Adapter",
            };
            string level = outputEvent.Category switch
            {
                OutputEvent.CategoryValue.Console => "Log",
                OutputEvent.CategoryValue.Stdout => "Log",
                OutputEvent.CategoryValue.Stderr => "Error",
                OutputEvent.CategoryValue.Telemetry => null,
                OutputEvent.CategoryValue.MessageBox => "Warning",
                OutputEvent.CategoryValue.Exception => "Error",
                OutputEvent.CategoryValue.Important => "Warning",
                OutputEvent.CategoryValue.Unknown => "Log",
                null => "Log",
                _ => "Log",
            };
            if (channel is not null && level is not null)
            {
                if (level == "Error")
                {
                    Debug.LogWarning(outputEvent.Output);
                }
                ConsoleWindow.AddMessage(outputEvent.Output, channel, level);
            }
        }

        /// <summary>
        /// Handles the terminated event.
        /// </summary>
        /// <param name="terminatedEvent">The event.</param>
        private void OnTerminatedEvent(TerminatedEvent terminatedEvent)
        {
            ConsoleWindow.AddMessage("Terminated\n");
            actions.Enqueue(() => Destroyer.Destroy(this));
        }

        /// <summary>
        /// Handles the exited event.
        /// </summary>
        /// <param name="exitEvent">The event.</param>
        private void OnExitedEvent(ExitedEvent exitedEvent) {
            OpenConsole();
            ConsoleWindow.AddMessage($"Exited with exit code {exitedEvent.ExitCode}\n", "Program", "Log");
            actions.Enqueue(() => Destroyer.Destroy(this));
        }

        /// <summary>
        /// Handles stopped events.
        /// </summary>
        /// <param name="stoppedEvent">The event.</param>
        private void OnStoppedEvent(StoppedEvent stoppedEvent)
        {
            IsRunning = false;
            if (stoppedEvent.Reason == StoppedEvent.ReasonValue.Exception)
            {
                actions.Enqueue(() =>
                {
                    if (capabilities.SupportsExceptionInfoRequest != true) return;

                    ExceptionInfoResponse exceptionInfo = adapterHost.SendRequestSync(new ExceptionInfoRequest()
                    {
                        ThreadId = mainThread.Id,
                    });
                    string description = $"{exceptionInfo.ExceptionId}" + (exceptionInfo.Description != null ? $"\n{exceptionInfo.Description}" : "");
                    OpenConsole();
                    ConsoleWindow.AddMessage(description + "\n", "Program", "Error");
                });
            }
        }

        /// <summary>
        /// Handles thread events.
        /// </summary>
        /// <param name="threadEvent">The event.</param>
        private void OnThreadEvent(ThreadEvent threadEvent)
        {
            if (threadEvent.Reason == ThreadEvent.ReasonValue.Started)
            {
                threads.Add(new(threadEvent.ThreadId, threadEvent.ThreadId.ToString()));
            }
            else if (threadEvent.Reason == ThreadEvent.ReasonValue.Exited)
            {
                threads.RemoveAll(t => t.Id == threadEvent.ThreadId);
            }
        }

        /// <summary>
        /// Handles continued events.
        /// </summary>
        /// <param name="continuedEvent">The event.</param>
        private void OnContinuedEvent(ContinuedEvent continuedEvent)
        {
            IsRunning = true;
        }

        /// <summary>
        /// Updates the capabilities.
        /// </summary>
        /// <param name="capabilitiesEvent">The changed capabilities.</param>
        private void OnCapabilitiesEvent(CapabilitiesEvent capabilitiesEvent)
        {
            if (capabilities == null)
            {
                actions.Enqueue(UpdateCapabilities);
            } else
            {
                UpdateCapabilities();
            }

            void UpdateCapabilities()
            {
                capabilities.SupportsConfigurationDoneRequest = capabilitiesEvent.Capabilities.SupportsConfigurationDoneRequest ?? capabilities.SupportsConfigurationDoneRequest;
                capabilities.SupportsFunctionBreakpoints = capabilitiesEvent.Capabilities.SupportsFunctionBreakpoints ?? capabilities.SupportsFunctionBreakpoints;
                capabilities.SupportsConditionalBreakpoints = capabilitiesEvent.Capabilities.SupportsConditionalBreakpoints ?? capabilities.SupportsConditionalBreakpoints;
                capabilities.SupportsHitConditionalBreakpoints = capabilitiesEvent.Capabilities.SupportsHitConditionalBreakpoints ?? capabilities.SupportsHitConditionalBreakpoints;
                capabilities.SupportsEvaluateForHovers = capabilitiesEvent.Capabilities.SupportsEvaluateForHovers ?? capabilities.SupportsEvaluateForHovers;
                capabilities.ExceptionBreakpointFilters = capabilitiesEvent.Capabilities.ExceptionBreakpointFilters ?? capabilities.ExceptionBreakpointFilters;
                capabilities.SupportsStepBack = capabilitiesEvent.Capabilities.SupportsStepBack ?? capabilities.SupportsStepBack;
                capabilities.SupportsSetVariable = capabilitiesEvent.Capabilities.SupportsSetVariable ?? capabilities.SupportsSetVariable;
                capabilities.SupportsRestartFrame = capabilitiesEvent.Capabilities.SupportsRestartFrame ?? capabilities.SupportsRestartFrame;
                capabilities.SupportsGotoTargetsRequest = capabilitiesEvent.Capabilities.SupportsGotoTargetsRequest ?? capabilities.SupportsGotoTargetsRequest;
                capabilities.SupportsStepInTargetsRequest = capabilitiesEvent.Capabilities.SupportsStepInTargetsRequest ?? capabilities.SupportsStepInTargetsRequest;
                capabilities.SupportsCompletionsRequest = capabilitiesEvent.Capabilities.SupportsCompletionsRequest ?? capabilities.SupportsCompletionsRequest;
                capabilities.CompletionTriggerCharacters = capabilitiesEvent.Capabilities.CompletionTriggerCharacters ?? capabilities.CompletionTriggerCharacters;
                capabilities.SupportsModulesRequest = capabilitiesEvent.Capabilities.SupportsModulesRequest ?? capabilities.SupportsModulesRequest;
                capabilities.AdditionalModuleColumns = capabilitiesEvent.Capabilities.AdditionalModuleColumns ?? capabilities.AdditionalModuleColumns;
                capabilities.SupportedChecksumAlgorithms = capabilitiesEvent.Capabilities.SupportedChecksumAlgorithms ?? capabilities.SupportedChecksumAlgorithms;
                capabilities.SupportsRestartRequest = capabilitiesEvent.Capabilities.SupportsRestartRequest ?? capabilities.SupportsRestartRequest;
                capabilities.SupportsExceptionOptions = capabilitiesEvent.Capabilities.SupportsExceptionOptions ?? capabilities.SupportsExceptionOptions;
                capabilities.SupportsValueFormattingOptions = capabilitiesEvent.Capabilities.SupportsValueFormattingOptions ?? capabilities.SupportsValueFormattingOptions;
                capabilities.SupportsExceptionInfoRequest = capabilitiesEvent.Capabilities.SupportsExceptionInfoRequest ?? capabilities.SupportsExceptionInfoRequest;
                capabilities.SupportTerminateDebuggee = capabilitiesEvent.Capabilities.SupportTerminateDebuggee ?? capabilities.SupportTerminateDebuggee;
                capabilities.SupportSuspendDebuggee = capabilitiesEvent.Capabilities.SupportSuspendDebuggee ?? capabilities.SupportSuspendDebuggee;
                capabilities.SupportsDelayedStackTraceLoading = capabilitiesEvent.Capabilities.SupportsDelayedStackTraceLoading ?? capabilities.SupportsDelayedStackTraceLoading;
                capabilities.SupportsLoadedSourcesRequest = capabilitiesEvent.Capabilities.SupportsLoadedSourcesRequest ?? capabilities.SupportsLoadedSourcesRequest;
                capabilities.SupportsLogPoints = capabilitiesEvent.Capabilities.SupportsLogPoints ?? capabilities.SupportsLogPoints;
                capabilities.SupportsTerminateThreadsRequest = capabilitiesEvent.Capabilities.SupportsTerminateThreadsRequest ?? capabilities.SupportsTerminateThreadsRequest;
                capabilities.SupportsSetExpression = capabilitiesEvent.Capabilities.SupportsSetExpression ?? capabilities.SupportsSetExpression;
                capabilities.SupportsTerminateRequest = capabilitiesEvent.Capabilities.SupportsTerminateRequest ?? capabilities.SupportsTerminateRequest;
                capabilities.SupportsDataBreakpoints = capabilitiesEvent.Capabilities.SupportsDataBreakpoints ?? capabilities.SupportsDataBreakpoints;
                capabilities.SupportsReadMemoryRequest = capabilitiesEvent.Capabilities.SupportsReadMemoryRequest ?? capabilities.SupportsReadMemoryRequest;
                capabilities.SupportsWriteMemoryRequest = capabilitiesEvent.Capabilities.SupportsWriteMemoryRequest ?? capabilities.SupportsWriteMemoryRequest;
                capabilities.SupportsDisassembleRequest = capabilitiesEvent.Capabilities.SupportsDisassembleRequest ?? capabilities.SupportsDisassembleRequest;
                capabilities.SupportsCancelRequest = capabilitiesEvent.Capabilities.SupportsCancelRequest ?? capabilities.SupportsCancelRequest;
                capabilities.SupportsBreakpointLocationsRequest = capabilitiesEvent.Capabilities.SupportsBreakpointLocationsRequest ?? capabilities.SupportsBreakpointLocationsRequest;
                capabilities.SupportsClipboardContext = capabilitiesEvent.Capabilities.SupportsClipboardContext ?? capabilities.SupportsClipboardContext;
                capabilities.SupportsSteppingGranularity = capabilitiesEvent.Capabilities.SupportsSteppingGranularity ?? capabilities.SupportsSteppingGranularity;
                capabilities.SupportsInstructionBreakpoints = capabilitiesEvent.Capabilities.SupportsInstructionBreakpoints ?? capabilities.SupportsInstructionBreakpoints;
                capabilities.SupportsExceptionFilterOptions = capabilitiesEvent.Capabilities.SupportsExceptionFilterOptions ?? capabilities.SupportsExceptionFilterOptions;
                capabilities.SupportsSingleThreadExecutionRequests = capabilitiesEvent.Capabilities.SupportsSingleThreadExecutionRequests ?? capabilities.SupportsSingleThreadExecutionRequests;
                capabilities.SupportsResumableDisconnect = capabilitiesEvent.Capabilities.SupportsResumableDisconnect ?? capabilities.SupportsResumableDisconnect;
                capabilities.SupportsExceptionConditions = capabilitiesEvent.Capabilities.SupportsExceptionConditions ?? capabilities.SupportsExceptionConditions;
                capabilities.SupportsLoadSymbolsRequest = capabilitiesEvent.Capabilities.SupportsLoadSymbolsRequest ?? capabilities.SupportsLoadSymbolsRequest;
                capabilities.SupportsModuleSymbolSearchLog = capabilitiesEvent.Capabilities.SupportsModuleSymbolSearchLog ?? capabilities.SupportsModuleSymbolSearchLog;
                capabilities.SupportsDebuggerProperties = capabilitiesEvent.Capabilities.SupportsDebuggerProperties ?? capabilities.SupportsDebuggerProperties;
                capabilities.SupportsSetSymbolOptions = capabilitiesEvent.Capabilities.SupportsSetSymbolOptions ?? capabilities.SupportsSetSymbolOptions;
                capabilities.SupportsAuthenticatedSymbolServers = capabilitiesEvent.Capabilities.SupportsAuthenticatedSymbolServers ?? capabilities.SupportsAuthenticatedSymbolServers;
            }
        }

        /// <summary>
        /// Handles user input of the console.
        /// </summary>
        /// <param name="text">The text.</param>
        private void OnConsoleInput(string text)
        {
            actions.Enqueue(() =>
            {
                try
                {
                    EvaluateResponse result = adapterHost.SendRequestSync(new EvaluateRequest()
                    {
                        Expression = text,
                        Context = EvaluateArguments.ContextValue.Repl,
                        FrameId = IsRunning ? null : stackFrame.Id
                    });
                    ConsoleWindow.AddMessage(result.Result + "\n", "Program", "Log");
                } catch (Exception e)
                {
                    ConsoleWindow.AddMessage(e.Message, "Program", "Error");
                }

            });
        }

        /// <summary>
        /// Queues a continue request.
        /// </summary>
        void OnContinue()
        {
            actions.Enqueue(() =>
            {
                if (IsRunning) return;
                adapterHost.SendRequest(new ContinueRequest { ThreadId = mainThread.Id }, _ => { });
            });
        }

        /// <summary>
        /// Queues a pause request.
        /// </summary>
        void OnPause()
        {
            actions.Enqueue(() =>
            {
                if (!IsRunning) return;
                adapterHost.SendRequest(new PauseRequest { ThreadId = mainThread.Id }, _ => { });
            });
        }

        /// <summary>
        /// Queues a reverse continue request.
        /// </summary>
        void OnReverseContinue()
        {
            actions.Enqueue(() =>
            {
                if (IsRunning) return;
                adapterHost.SendRequest(new ReverseContinueRequest { ThreadId = mainThread.Id }, _ => { });
            });
        }

        /// <summary>
        /// Queues a next request.
        /// </summary>
        void OnNext()
        {
            actions.Enqueue(() =>
            {
                if (IsRunning) return;
                adapterHost.SendRequest(new NextRequest { ThreadId = mainThread.Id, Granularity = steppingGranularity }, _ => { });
            });
        }

        /// <summary>
        /// Queues a step back request.
        /// </summary>
        void OnStepBack()
        {
            actions.Enqueue(() =>
            {
                if (IsRunning) return;
                adapterHost.SendRequest(new StepBackRequest { ThreadId = mainThread.Id, Granularity = steppingGranularity }, _ => { });
            });
        }

        /// <summary>
        /// Queues a step in request.
        /// </summary>
        void OnStepIn()
        {
            actions.Enqueue(() =>
            {
                if (IsRunning) return;
                adapterHost.SendRequest(new StepInRequest { ThreadId = mainThread.Id, Granularity = steppingGranularity }, _ => { });
            });
        }

        /// <summary>
        /// Queues a step out request.
        /// </summary>
        void OnStepOut()
        {
            actions.Enqueue(() =>
            {
                if (IsRunning) return;
                adapterHost.SendRequest(new StepOutRequest { ThreadId = mainThread.Id, Granularity = steppingGranularity }, _ => { });
            });
        }

        /// <summary>
        /// Queues a restart request.
        /// </summary>
        void OnRestart()
        {
            actions.Enqueue(() =>
            {
                adapterHost.SendRequest(new RestartRequest { Arguments = Adapter.GetLaunchRequest() }, _ => IsRunning = true);
            });
        }

        /// <summary>
        /// Queues a terminate request.
        /// </summary>
        void OnStop()
        {
            actions.Enqueue(() =>
            {
                if (capabilities.SupportsTerminateRequest == true)
                {
                    Terminate();
                } else
                {
                    Disconnect();
                }
            });

            // Tries to stop the debuggee gracefully.
            void Terminate()
            {
                adapterHost.SendRequest(new TerminateRequest(),
                    _ => QueueDestroy(),
                    (_, _) => actions.Enqueue(Disconnect));
            }
            // Forcefully shuts down the debuggee.
            void Disconnect()
            {
                adapterHost.SendRequest(new DisconnectRequest(),
                    _ => QueueDestroy(),
                    (_, _) => QueueDestroy()
                );
            }
            void QueueDestroy()
            {
                actions.Enqueue(() => Destroyer.Destroy(this));
            }
        }
        #endregion

        #region Utilities
        /// <summary>
        /// Updates the code position.
        /// Marks it in the code window and highlights it in the city.
        /// Must be executed on the main thread.
        /// </summary>
        private void UpdateCodePosition()
        {
            if (IsRunning) return;

            StackFrame stackFrameWithSource = stackFrames.FirstOrDefault(frame => frame.Source != null);
            if (stackFrameWithSource == null)
            {
                return;
            }

            lastCodePath = stackFrameWithSource.Source.Path;
            lastCodeLine = stackFrameWithSource.Line;

            if (sourceRangeIndex != null)
            {
                // TODO: Does SourceRangeIndex always have slashes?
                string sourceRangeIndexPath = lastCodePath.Replace("\\", "/");
                if (sourceRangeIndexPath.StartsWith(City.SourceCodeDirectory.AbsolutePath))
                {
                    sourceRangeIndexPath = sourceRangeIndexPath.Substring(City.SourceCodeDirectory.AbsolutePath.Length);
                    if (sourceRangeIndexPath.StartsWith("/"))
                    {
                        sourceRangeIndexPath = sourceRangeIndexPath.Substring(1);
                    }
                }
                Node node;
                if (sourceRangeIndex.TryGetValue(sourceRangeIndexPath, lastCodeLine, out node))
                {
                    if (lastHighlighted != null && lastHighlighted.ID != node.ID)
                    {
                        Edge edge = lastHighlighted.Outgoings.FirstOrDefault(e => e.Target.ID == node.ID);
                        if (edge)
                        {
                            edge.Operator().Highlight(highlightDurationInitial, false);
                        }
                    }
                    float duration = lastHighlighted == null || lastHighlighted.ID != node.ID ? highlightDurationInitial : highlightDurationRepeated;
                    lastHighlighted = node;
                    ShowCodePosition(true, true, highlightDurationInitial);
                } else
                {
                    ShowCodePosition(true, true, -1);
                }
            } else
            {
                ShowCodePosition(true, true, -1);
            }
        }

        /// <summary>
        /// Shows the code position.
        /// </summary>
        /// <param name="makeActive">Whether to make the code window active.</param>
        /// <param name="scroll">Whether to scroll the code window.</param>
        /// <param name="highlightDuration">The highlight duration (seconds) in the city.</param>
        private void ShowCodePosition(bool makeActive = false, bool scroll = false, float highlightDuration = highlightDurationInitial)
        {
            WindowSpace manager = WindowSpaceManager.ManagerInstance[WindowSpaceManager.LocalPlayer];
            CodeWindow codeWindow = manager.Windows.OfType<CodeWindow>().FirstOrDefault(window => Filenames.OnCurrentPlatform(window.FilePath) == Filenames.OnCurrentPlatform(lastCodePath));
            if (codeWindow == null)
            {
                codeWindow = Canvas.AddComponent<CodeWindow>();
                codeWindow.Title = Path.GetFileName(lastCodePath);
                codeWindow.EnterFromFile(lastCodePath);
                manager.AddWindow(codeWindow);
                codeWindow.OnComponentInitialized += Mark;
                codeWindow.OnComponentInitialized += MakeActive;
            } else
            {
                Mark();
                MakeActive();
            }
            if (lastHighlighted && highlightDuration > 0)
            {
                lastHighlighted.Operator().Highlight(highlightDuration, false);
            }

            lastCodeWindow = codeWindow;


            void MakeActive()
            {
                if (makeActive)
                {
                    manager.ActiveWindow = codeWindow;
                }
            }
            void Mark()
            {
                if (scroll)
                {
                    codeWindow.ScrolledVisibleLine = lastCodeLine;
                } else
                {
                    codeWindow.MarkLine(lastCodeLine);
                }
            }
        }

        /// <summary>
        /// Clears the last code position.
        /// </summary>
        private void ClearLastCodePosition()
        {
            if (lastCodeWindow)
            {
                lastCodeWindow.MarkLine(0);
            }
        }

        /// <summary>
        /// Updates <see cref="threads"/>.
        /// Must be executed on the main thread.
        /// </summary>
        private void UpdateThreads()
        {
            if (IsRunning) return;

            threads = adapterHost.SendRequestSync(new ThreadsRequest()).Threads;
        }

        /// <summary>
        /// Updates the stack frames.
        /// Must be executed on the main thread.
        /// </summary>
        private void UpdateStackFrames()
        {
            if (IsRunning) return;

            stackFrames = adapterHost.SendRequestSync(new StackTraceRequest() { ThreadId = mainThread.Id }).StackFrames;
        }


        /// <summary>
        /// Updates <see cref="variables"/>.
        /// Must be executed on the main thread.
        /// </summary>
        private void UpdateVariables()
        {
            if (IsRunning) return;
            variables = new();

            foreach (Thread thread in threads)
            {
                Dictionary<StackFrame, Dictionary<Scope, List<Variable>>> threadVariables = new();
                variables.Add(thread, threadVariables);

                foreach (StackFrame stackFrame in stackFrames)
                {
                    Dictionary<Scope, List<Variable>> stackVariables = new();
                    threadVariables.Add(stackFrame, stackVariables);
                    List<Scope> stackScopes = adapterHost.SendRequestSync(new ScopesRequest() { FrameId = stackFrame.Id }).Scopes;
                    foreach (Scope scope in stackScopes)
                    {
                        stackVariables.Add(scope, RetrieveNestedVariables(scope.VariablesReference));
                    }
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
            if (variablesReference <= 0 || IsRunning) return new();
            return adapterHost.SendRequestSync(new VariablesRequest() { VariablesReference = variablesReference }).Variables;
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
                EvaluateResponse value = adapterHost.SendRequestSync(new EvaluateRequest()
                {
                    Expression = variable.EvaluateName,
                    FrameId = IsRunning ? null : stackFrame.Id
                });
                return value.Result;
            }

            return variable.Value;
        }
        #endregion
    }
}
