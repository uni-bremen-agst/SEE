using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol;
using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages;
using SEE.Controls;
using SEE.GO;
using SEE.UI.Window;
using SEE.UI.Window.CodeWindow;
using SEE.UI.Window.ConsoleWindow;
using SEE.Utils;
using Sirenix.Utilities;
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
        private const string DebugControlsPrefab = "Prefabs/UI/DebugAdapterProtocolControls";

        public DebugAdapter.DebugAdapter Adapter;

        private ConsoleWindow console;
        private GameObject controls;
        private Process adapterProcess;
        private DebugProtocolHost adapterHost;
        private InitializeResponse capabilities;
        private Tooltip.Tooltip tooltip;
        private CodeWindow lastCodeWindow;

        private Queue<Action> actions = new();

        private bool isRunning;
        private List<Thread> threads = new();
        private Thread? thread => threads.FirstOrDefault();

        #region General
        protected void Start()
        {
            tooltip = gameObject.AddComponent<Tooltip.Tooltip>();
            SetupConsole();
            SetupControls();

            if (Adapter == null)
            {
                LogError(new("Debug adapter not set."));
                Destroyer.Destroy(this);
                return;
            }
            console.AddMessage($"Start debugging session: {Adapter.Name} - {Adapter.AdapterFileName} - {Adapter.AdapterArguments} - {Adapter.AdapterWorkingDirectory}\n");

            if (!CreateAdapterProcess())
            {
                LogError(new("Couldn't create the debug adapter process."));
                Destroyer.Destroy(this);
                return;
            }
            else
            {
                console.AddMessage("Created the debug adapter process.\n", "Adapter", "Log");
            }
            if (!CreateAdapterHost())
            {
                LogError(new("Couldn't create the debug adapter host."));
                Destroyer.Destroy(this);
                return;
            }
            else
            {
                console.AddMessage("Created the debug adapter host.\n", "Adapter", "Log");
            }

            DebugBreakpointManager.OnBreakpointAdded += OnBreakpointsChanged;
            DebugBreakpointManager.OnBreakpointRemoved += OnBreakpointsChanged;

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
            } catch (Exception e)
            {
                LogError(e);
                Destroyer.Destroy(this);
            }
        }
        private void Update()
        {
            if (actions.Count > 0 && capabilities != null)
            {
                try
                {
                    actions.Dequeue()();
                }
                catch (Exception e)
                {
                    LogError(e);
                }
            }
        }
        private void OnDestroy()
        {
            console.AddMessage("Debug session finished.\n");
            actions.Clear();
            ClearLastCodePosition();
            DebugBreakpointManager.OnBreakpointAdded -= OnBreakpointsChanged;
            DebugBreakpointManager.OnBreakpointRemoved -= OnBreakpointsChanged;
            if (console)
            {
                console.OnInputSubmit -= OnConsoleInput;
            }
            if (controls)
            {
                Destroyer.Destroy(controls);
            }
            if (tooltip)
            {
                Destroyer.Destroy(tooltip);
            }
            if (adapterHost != null && adapterHost.IsRunning)
            {
                adapterHost.Stop();
            }
            if (adapterProcess != null && !adapterProcess.HasExited)
            {
                adapterProcess.Close();
            }
        }
        #endregion

        #region Setup
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
                    {"Terminal", (true, SetupConsole, "Open the Terminal")},
                };
                foreach (var (name, (active,action, description)) in listeners)
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
            return;
        }
        private void SetupConsole()
        {
            WindowSpace manager = WindowSpaceManager.ManagerInstance[WindowSpaceManager.LocalPlayer];
            if (console == null)
            {
                console = manager.Windows.OfType<ConsoleWindow>().FirstOrDefault();
                if (console == null)
                {
                    console = gameObject.AddComponent<ConsoleWindow>();
                    console.DefaultChannel = "Adapter";
                    console.DefaultChannelLevel = "Log";
                    manager.AddWindow(console);
                }
                foreach ((string channel, char icon) in new[] { ("Adapter", '\uf188'), ("Debugee", '\uf135') })
                {
                    console.AddChannel(channel, icon);
                    foreach ((string level, Color color) in new[] { ("Log", Color.gray), ("Warning", Color.yellow.Darker()), ("Error", Color.red) })
                    {
                        console.AddChannelLevel(channel, level, color);
                    }
                }
                console.SetChannelLevelEnabled("Adapter", "Log", false);
                console.OnInputSubmit += OnConsoleInput;
            }
            manager.ActiveWindow = console;
        }
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
            adapterProcess.Exited += (_, args) => console.AddMessage($"Process: Exited! ({(!adapterProcess.HasExited ? adapterProcess.ProcessName : null)})");
            adapterProcess.Disposed += (_, args) => console.AddMessage($"Process: Exited! ({(!adapterProcess.HasExited ? adapterProcess.ProcessName : null)})");
            adapterProcess.OutputDataReceived += (_, args) => console.AddMessage($"Process: OutputDataReceived! ({adapterProcess.ProcessName})\n\t{args.Data}");
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
        private bool CreateAdapterHost()
        {
            adapterHost = new DebugProtocolHost(adapterProcess.StandardInput.BaseStream, adapterProcess.StandardOutput.BaseStream);
            adapterHost.DispatcherError += (sender, args) => LogError(new($"DispatcherError - {args.Exception}"));
            adapterHost.ResponseTimeThresholdExceeded += (_, args) => console.AddMessage($"ResponseTimeThresholdExceeded - \t{args.Command}\t{args.SequenceId}\t{args.Threshold}\n", "Adapter", "Warning");
            adapterHost.EventReceived += OnEventReceived;
            adapterHost.Run();

            return adapterHost.IsRunning;
        }
        #endregion

        #region Events
        private void OnBreakpointsChanged(string path, int line)
        {
            actions.Enqueue(() =>
            {
                adapterHost.SendRequestSync(new SetBreakpointsRequest()
                {
                    Source = new Source() { Path = path, Name = Path.GetFileName(path)},
                    Breakpoints = DebugBreakpointManager.Breakpoints[path].Values.ToList(),
                });
                Debug.Log("Done");
            });
        }
        private void OnEventReceived(object sender, EventReceivedEventArgs e)
        {
            if (e.Body is InitializedEvent)
            {
                actions.Enqueue(() =>
                {
                    foreach ((string path, Dictionary<int, SourceBreakpoint> breakpoints) in DebugBreakpointManager.Breakpoints)
                    {
                        adapterHost.SendRequestSync(new SetBreakpointsRequest()
                        {
                            Source = new Source() { Path = path, Name = Path.GetFileName(path) },
                            Breakpoints = breakpoints.Values.ToList(),
                        });
                    }
                    adapterHost.SendRequestSync(new SetFunctionBreakpointsRequest() { Breakpoints = new() });
                    adapterHost.SendRequestSync(new SetExceptionBreakpointsRequest() { Filters = new()});
                    Adapter.Launch(adapterHost, capabilities);
                    isRunning = true;
                });
            }
            else if (e.Body is OutputEvent outputEvent)
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
                    // FIXME: Why does it require a cast?
                    console.AddMessage(outputEvent.Output, channel, level);
                }
            }
            else if (e.Body is TerminatedEvent terminatedEvent)
            {
                // TODO: Let user restart the program.
                console.AddMessage("Terminated\n");
                actions.Enqueue(() => Destroyer.Destroy(this));
            }
            else if (e.Body is ExitedEvent exitedEvent)
            {
                isRunning = false;

                console.AddMessage($"Exited with exit code {exitedEvent.ExitCode}\n", "Debugee", "Log");
                actions.Enqueue(() => Destroyer.Destroy(this));
            }
            else if (e.Body is StoppedEvent stoppedEvent)
            {
                actions.Enqueue(() =>
                {
                    threads = adapterHost.SendRequestSync(new ThreadsRequest()).Threads;
                    isRunning = false;
                    UpdateCodePosition();
                });
            }
            else if (e.Body is ThreadEvent threadEvent)
            {
                if (threadEvent.Reason == ThreadEvent.ReasonValue.Started)
                {
                    threads.Add(new(threadEvent.ThreadId, threadEvent.ThreadId.ToString()));
                } else if (threadEvent.Reason == ThreadEvent.ReasonValue.Exited)
                {
                    threads.RemoveAll(t => t.Id == threadEvent.ThreadId);
                }
            }
            else if (e.Body is ContinuedEvent)
            {
                isRunning = true;
            }
        }
        private void OnConsoleInput(string text)
        {
            actions.Enqueue(() =>
            {
                if (thread == null) return;
                StackFrame stackFrame = adapterHost.SendRequestSync(new StackTraceRequest() { ThreadId = thread.Id }).StackFrames[0];
                EvaluateResponse result = adapterHost.SendRequestSync(new EvaluateRequest()
                {
                    Expression = text,
                    Context = EvaluateArguments.ContextValue.Repl,
                    FrameId = stackFrame.Id
                });
                console.AddMessage(result.Result, "Debugee", "Log");
            });
        }

        void OnContinue()
        {
            actions.Enqueue(() =>
            {
                if (thread is null || isRunning) return;
                adapterHost.SendRequest(new ContinueRequest { ThreadId = thread.Id }, _ => isRunning = true);
            });
        }
        void OnPause()
        {
            actions.Enqueue(() =>
            {
                if (thread is null || !isRunning) return;
                adapterHost.SendRequest(new ContinueRequest { ThreadId = thread.Id }, _ => isRunning = false);
            });
        }
        void OnReverseContinue()
        {
            actions.Enqueue(() =>
            {
                if (thread is null || isRunning) return;
                adapterHost.SendRequest(new ReverseContinueRequest { ThreadId = thread.Id }, _ => isRunning = true);
            });
        }
        void OnNext()
        {
            actions.Enqueue(() =>
            {
                if (thread is null || isRunning) return;
                adapterHost.SendRequest(new NextRequest { ThreadId = thread.Id }, _ => isRunning = true);
            });
        }
        void OnStepBack()
        {
            actions.Enqueue(() =>
            {
                if (thread is null || isRunning) return;
                adapterHost.SendRequest(new StepBackRequest { ThreadId = thread.Id }, _ => isRunning = true);
            });
        }
        void OnStepIn()
        {
            actions.Enqueue(() =>
            {
                if (thread is null || isRunning) return;
                adapterHost.SendRequest(new StepInRequest { ThreadId = thread.Id }, _ => isRunning = true);
            });
        }
        void OnStepOut()
        {
            actions.Enqueue(() =>
            {
                if (thread is null || isRunning) return;
                adapterHost.SendRequest(new StepOutRequest { ThreadId = thread.Id }, _ => isRunning = true);
            });
        }
        void OnRestart()
        {
            actions.Enqueue(() =>
            {
                adapterHost.SendRequest(new RestartRequest { Arguments = Adapter.GetLaunchRequest(capabilities) }, _ => isRunning = true);
            });
        }
        void OnStop()
        {
            actions.Enqueue(() =>
            {
                adapterHost.SendRequest(new TerminateRequest(), _ => { });
            });
        }
        #endregion

        #region Utilities
        private void LogError(Exception e)
        {
            console.AddMessage(e.ToString() + "\n", "Adapter", "Error");
            Debug.LogWarning(e);
        }
        private void UpdateCodePosition()
        {
            if (thread == null) return;
            ClearLastCodePosition();

            StackFrame stackFrame = adapterHost.SendRequestSync(new StackTraceRequest(){ ThreadId = thread.Id}).StackFrames[0];

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
                    codeWindow.ScrolledVisibleLine = stackFrame.Line;
                };
            }
            else
            {
                codeWindow.MarkLine(stackFrame.Line);
            }
            manager.ActiveWindow = codeWindow;

            lastCodeWindow = codeWindow;
        }

        private void ClearLastCodePosition()
        {
            if (lastCodeWindow)
            {
                lastCodeWindow.MarkLine(0);
            }
        }
        #endregion
    }


}