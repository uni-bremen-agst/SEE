using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol;
using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages;
using SEE.Controls;
using SEE.GO;
using SEE.UI.Window;
using SEE.UI.Window.CodeWindow;
using SEE.UI.Window.ConsoleWindow;
using SEE.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;
using Encoding = System.Text.Encoding;
using StackFrame = Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages.StackFrame;

namespace SEE.UI.DebugAdapterProtocol
{
    public class DebugAdapterProtocolSession : MonoBehaviour
    {
        /// <summary>
        /// The debug adapter.
        /// </summary>
        public DebugAdapter.DebugAdapter Adapter;

        /// <summary>
        /// The prefab for the debug controls.
        /// </summary>
        private const string DebugControlsPrefab = "Prefabs/UI/DebugAdapterProtocolControls";
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
        /// The code window where the last code position was marked.
        /// </summary>
        private CodeWindow lastCodeWindow;

        /// <summary>
        /// Queued actions that are executed on the main thread.
        /// <seealso cref="Update"/>
        /// </summary>
        private Queue<Action> actions = new();

        /// <summary>
        /// Whether the debugger is current executing the debugee.
        /// </summary>
        private bool isRunning;

        /// <summary>
        /// Whether the debugger is current executing the debugee.
        /// Automatically updates the code position on change.
        /// </summary>
        private bool IsRunning
        {
            get => isRunning;
            set
            {
                if (value == isRunning) return;
                isRunning = value;
                if (!value)
                {
                    UpdateCodePosition();
                }
                else
                {
                    ClearLastCodePosition();
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
        private Thread? thread => threads.FirstOrDefault();

        #region General
        /// <summary>
        /// Sets up the debug session.
        /// </summary>
        protected void Start()
        {
            // creates ui elements
            tooltip = gameObject.AddComponent<Tooltip.Tooltip>();
            SetupConsole(true);
            SetupControls();

            if (Adapter == null)
            {
                LogError(new("Debug adapter not set."));
                Destroyer.Destroy(this);
                return;
            }
            ConsoleWindow.AddMessage($"Start debugging session: {Adapter.Name} - {Adapter.AdapterFileName} - {Adapter.AdapterArguments} - {Adapter.AdapterWorkingDirectory}\n");

            // starts the debug adapter process
            if (!CreateAdapterProcess())
            {
                LogError(new("Couldn't create the debug adapter process."));
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
                LogError(new("Couldn't create the debug adapter host."));
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
                LogError(e);
                Destroyer.Destroy(this);
            }
        }

        /// <summary>
        /// Executes the queued actions on the main thread.
        /// Waits until the capabilities are known.
        /// </summary>
        private void Update()
        {
            if (actions.Count > 0 && capabilities != null)
            {
                // TODO: Try catch -> LogError
                actions.Dequeue()();
            }
        }

        /// <summary>
        /// Cleans up debug session.
        /// </summary>
        private void OnDestroy()
        {
            actions.Clear();
            threads.Clear();

            ConsoleWindow.AddMessage("Debug session finished.\n");
            DebugBreakpointManager.OnBreakpointAdded -= OnBreakpointsChanged;
            DebugBreakpointManager.OnBreakpointRemoved -= OnBreakpointsChanged;
            ConsoleWindow.OnInputSubmit -= OnConsoleInput;
            if (lastCodeWindow)
            {
                lastCodeWindow.MarkLine(0);
            }
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
                Dictionary<string, (bool, Action, string)> listeners = new Dictionary<string, (bool, Action, string)>
                {
                    {"Continue", (true, OnContinue, "Continue")},
                    {"Pause", (true, OnPause, "Pause")},
                    {"Reverse", (capabilities.SupportsStepBack == true, OnReverseContinue, "Reverse")},
                    {"Next", (true, OnNext, "Next")},
                    {"StepBack", (capabilities.SupportsStepBack == true, OnStepBack, "Step Back")},
                    {"StepIn", (true, OnStepIn, "Step In")},
                    {"StepOut", (true, OnStepOut, "Step Out")},
                    {"Restart", (capabilities.SupportsRestartRequest == true, OnRestart, "Restart")},
                    {"Stop", (true, OnStop , "Stop")},
                    {"Terminal", (true, () => SetupConsole(), "Open the Terminal")},
                };
                foreach (var (name, (active, action, description)) in listeners)
                {
                    GameObject button = controls.transform.Find(name).gameObject;
                    button.SetActive(active);
                    button.MustGetComponent<Button>().onClick.AddListener(() => action());
                    if (button.TryGetComponentOrLog(out PointerHelper pointerHelper))
                    {
                        pointerHelper.EnterEvent.AddListener(_ => tooltip.Show(description));
                        pointerHelper.ExitEvent.AddListener(_ => tooltip.Hide());
                    }
                }
            });
        }

        /// <summary>
        /// Sets up the console.
        /// Can be recalled to reopen and/or focus the console.
        /// </summary>
        private void SetupConsole(bool start = false)
        {
            WindowSpace manager = WindowSpaceManager.ManagerInstance[WindowSpaceManager.LocalPlayer];
            ConsoleWindow console = manager.Windows.OfType<ConsoleWindow>().FirstOrDefault();
            if (console == null)
            {
                console = gameObject.AddComponent<ConsoleWindow>();
                ConsoleWindow.DefaultChannel = "Adapter";
                ConsoleWindow.DefaultChannelLevel = "Log";
                manager.AddWindow(console);
                if (start)
                {
                    foreach ((string channel, char icon) in new[] { ("Adapter", '\uf188'), ("Debugee", '\uf135') })
                    {
                        ConsoleWindow.AddChannel(channel, icon);
                        foreach ((string level, Color color) in new[] { ("Log", Color.gray), ("Warning", Color.yellow.Darker()), ("Error", Color.red.Darker()) })
                        {
                            ConsoleWindow.AddChannelLevel(channel, level, color);
                        }
                    }
                    ConsoleWindow.SetChannelLevelEnabled("Adapter", "Log", false);
                    ConsoleWindow.OnInputSubmit += OnConsoleInput;
                }
            }
            manager.ActiveWindow = console;
        }
        /// <summary>
        /// Creates the process for the debug adapter.
        /// </summary>
        /// <returns></returns>
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
            adapterProcess.ErrorDataReceived += (_, args) => LogError(new($"Process: ErrorDataReceived! ({adapterProcess.ProcessName})\n\t{args.Data}"));

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
                LogError(e);
                adapterProcess = null;
            }
            // working directory needs to be reset (otherwise unity crashes)
            Directory.SetCurrentDirectory(currentDirectory);

            return adapterProcess != null && !adapterProcess.HasExited;
        }

        /// <summary>
        /// Creates the debug adapter host.
        /// </summary>
        /// <returns></returns>
        private bool CreateAdapterHost()
        {
            adapterHost = new DebugProtocolHost(adapterProcess.StandardInput.BaseStream, adapterProcess.StandardOutput.BaseStream);
            adapterHost.DispatcherError += (sender, args) => LogError(new($"DispatcherError - {args.Exception}"));
            adapterHost.ResponseTimeThresholdExceeded += (_, args) => ConsoleWindow.AddMessage($"ResponseTimeThresholdExceeded - \t{args.Command}\t{args.SequenceId}\t{args.Threshold}\n", "Adapter", "Warning");
            adapterHost.EventReceived += OnEventReceived;
            adapterHost.Run();

            return adapterHost.IsRunning;
        }
        #endregion

        #region Events
        /// <summary>
        /// Updates the breakpoints.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="line"></param>
        private void OnBreakpointsChanged(string path, int line)
        {
            actions.Enqueue(() =>
            {
                SetBreakpointsResponse response = adapterHost.SendRequestSync(new SetBreakpointsRequest()
                {
                    Source = new Source() { Path = path, Name = Path.GetFileName(path) },
                    Breakpoints = DebugBreakpointManager.Breakpoints[path].Values.ToList(),
                });
                Debug.Log(String.Join(" ", DebugBreakpointManager.Breakpoints[path].Keys));
                foreach (Breakpoint breakpoint in response.Breakpoints)
                {
                    Debug.Log($"Breakpoint\t{breakpoint.Id}\t{breakpoint.Source.Name}\t{breakpoint.Line}\t{breakpoint.Verified}");
                }
                Debug.Log("Done");
            });
        }

        /// <summary>
        /// Handles events of the debug adapter.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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
            }
        }

        /// <summary>
        /// Handles the initialized event.
        /// </summary>
        private void OnInitializedEvent(InitializedEvent initializedEvent)
        {
            actions.Enqueue(() =>
            {
                adapterHost.SendRequest(Adapter.GetLaunchRequest(), _ => { });
                IsRunning = true;
                foreach ((string path, Dictionary<int, SourceBreakpoint> breakpoints) in DebugBreakpointManager.Breakpoints)
                {
                    adapterHost.SendRequest(new SetBreakpointsRequest()
                    {
                        Source = new Source() { Path = path, Name = Path.GetFileName(path) },
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
                OutputEvent.CategoryValue.Stdout => "Debugee",
                OutputEvent.CategoryValue.Stderr => "Debugee",
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
            ConsoleWindow.AddMessage($"Exited with exit code {exitedEvent.ExitCode}\n", "Debugee", "Log");
            actions.Enqueue(() => Destroyer.Destroy(this));
        }

        /// <summary>
        /// Handles stopped events.
        /// </summary>
        /// <param name="stoppedEvent">The event.</param>
        private void OnStoppedEvent(StoppedEvent stoppedEvent)
        {
            IsRunning = false;
            actions.Enqueue(() => threads = adapterHost.SendRequestSync(new ThreadsRequest()).Threads);
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

        private void OnContinuedEvent(ContinuedEvent continuedEvent)
        {
            IsRunning = true;
        }

        /// <summary>
        /// Handles user input of the console.
        /// </summary>
        /// <param name="text"></param>
        private void OnConsoleInput(string text)
        {
            actions.Enqueue(() =>
            {
                if (thread == null) return;
                StackFrame stackFrame = !IsRunning ? 
                    adapterHost.SendRequestSync(new StackTraceRequest() { ThreadId = thread.Id }).StackFrames[0] :
                    null;
                try
                {
                    EvaluateResponse result = adapterHost.SendRequestSync(new EvaluateRequest()
                    {
                        Expression = text,
                        Context = EvaluateArguments.ContextValue.Repl,
                        FrameId = stackFrame?.Id
                    });
                    ConsoleWindow.AddMessage(result.Result + "\n", "Debugee", "Log");
                } catch (ProtocolException e)
                {
                    ConsoleWindow.AddMessage(e.Message, "Debugee", "Error");
                }

            });
        }

        /// <summary>
        /// Sends a continue request.
        /// </summary>
        void OnContinue()
        {
            actions.Enqueue(() =>
            {
                if (thread is null || IsRunning) return;
                adapterHost.SendRequest(new ContinueRequest { ThreadId = thread.Id }, _ => { });
            });
        }

        /// <summary>
        /// Sends a pause request.
        /// </summary>
        void OnPause()
        {
            actions.Enqueue(() =>
            {
                if (thread is null || !IsRunning) return;
                adapterHost.SendRequest(new PauseRequest { ThreadId = thread.Id }, _ => { });
            });
        }

        /// <summary>
        /// Sends a reverse continue request.
        /// </summary>
        void OnReverseContinue()
        {
            actions.Enqueue(() =>
            {
                if (thread is null || IsRunning) return;
                adapterHost.SendRequest(new ReverseContinueRequest { ThreadId = thread.Id }, _ => { });
            });
        }

        /// <summary>
        /// Sends a next request.
        /// </summary>
        void OnNext()
        {
            actions.Enqueue(() =>
            {
                if (thread is null || IsRunning) return;
                adapterHost.SendRequest(new NextRequest { ThreadId = thread.Id, Granularity = steppingGranularity }, _ => { });
            });
        }

        /// <summary>
        /// Sends a step back request.
        /// </summary>
        void OnStepBack()
        {
            actions.Enqueue(() =>
            {
                if (thread is null || IsRunning) return;
                adapterHost.SendRequest(new StepBackRequest { ThreadId = thread.Id, Granularity = steppingGranularity }, _ => { });
            });
        }

        /// <summary>
        /// Sends a step in request.
        /// </summary>
        void OnStepIn()
        {
            actions.Enqueue(() =>
            {
                if (thread is null || IsRunning) return;
                adapterHost.SendRequest(new StepInRequest { ThreadId = thread.Id, Granularity = steppingGranularity }, _ => { });
            });
        }

        /// <summary>
        /// Sends a step out request.
        /// </summary>
        void OnStepOut()
        {
            actions.Enqueue(() =>
            {
                if (thread is null || IsRunning) return;
                adapterHost.SendRequest(new StepOutRequest { ThreadId = thread.Id, Granularity = steppingGranularity }, _ => { });
            });
        }

        /// <summary>
        /// Sends a restart request.
        /// </summary>
        void OnRestart()
        {
            actions.Enqueue(() =>
            {
                adapterHost.SendRequest(new RestartRequest { Arguments = Adapter.GetLaunchRequest() }, _ => { });
                IsRunning = true;
            });
        }

        /// <summary>
        /// Sends a terminate request.
        /// </summary>
        void OnStop()
        {
            actions.Enqueue(() =>
            {
                adapterHost.SendRequest(new TerminateRequest(), _ => { });
            });
        }
        #endregion

        #region Utilities
        /// <summary>
        /// Logs an error in the console window and in the unity console.
        /// </summary>
        /// <param name="e"></param>
        private void LogError(Exception e)
        {
            ConsoleWindow.AddMessage(e.ToString() + "\n", "Adapter", "Error");
            Debug.LogWarning(e);
        }

        /// <summary>
        /// Updates the code position.
        /// </summary>
        private void UpdateCodePosition()
        {
            actions.Enqueue(() =>
            {
                if (thread == null) return;

                StackFrame stackFrame = adapterHost.SendRequestSync(new StackTraceRequest() { ThreadId = thread.Id }).StackFrames[0];

                string path = stackFrame.Source.Path;
                string title = Path.GetFileName(path);

                WindowSpace manager = WindowSpaceManager.ManagerInstance[WindowSpaceManager.LocalPlayer];
                CodeWindow codeWindow = manager.Windows.OfType<CodeWindow>().FirstOrDefault(window => window.Title == title);
                if (codeWindow == null)
                {
                    codeWindow = gameObject.AddComponent<CodeWindow>();
                    codeWindow.Title = title;
                    codeWindow.EnterFromFile(path, false);
                    manager.AddWindow(codeWindow);
                    codeWindow.OnComponentInitialized += () =>
                    {
                        codeWindow.MarkLine(stackFrame.Line);
                    };
                }
                else
                {
                    codeWindow.EnterFromFile(path, false);
                    codeWindow.MarkLine(stackFrame.Line);
                }
                manager.ActiveWindow = codeWindow;

                lastCodeWindow = codeWindow;
            });


        }

        private void ClearLastCodePosition()
        {
            actions.Enqueue(() =>
            {
                if (lastCodeWindow)
                {
                    lastCodeWindow.MarkLine(0);
                }
            });

        }
        #endregion
    }


}