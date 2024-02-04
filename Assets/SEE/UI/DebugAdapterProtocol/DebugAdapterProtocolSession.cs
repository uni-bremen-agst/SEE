using Michsky.UI.ModernUIPack;
using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol;
using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages;
using RootMotion;
using SEE.Controls;
using SEE.Controls.Actions;
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
        private const string DebugControlsPrefab = "Prefabs/UI/DebugAdapterProtocolControls";

        public DebugAdapter.DebugAdapter Adapter;

        private ConsoleWindow console;
        private GameObject controls;
        private Process adapterProcess;
        private DebugProtocolHost adapterHost;
        private InitializeResponse capabilities;

        private Queue<Action> actions = new();

        private bool isRunning;
        private List<int> threads = new();
        private int? threadId => threads.Count > 0 ? threads.First() : null;
        private object? restartData;

        protected void Start()
        {
            OpenConsole();
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
                console.AddMessage("Created the debug adapter process.\n", ConsoleWindow.MessageSource.Adapter, ConsoleWindow.MessageLevel.Log);
            }
            if (!CreateAdapterHost())
            {
                LogError(new("Couldn't create the debug adapter host."));
                Destroyer.Destroy(this);
                return;
            }
            else
            {
                console.AddMessage("Created the debug adapter host.\n", ConsoleWindow.MessageSource.Adapter, ConsoleWindow.MessageLevel.Log);
            }

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

        private void SetupControls()
        {
            controls = PrefabInstantiator.InstantiatePrefab(DebugControlsPrefab, transform, false);
            controls.transform.position = new Vector3(Screen.width * 0.5f, Screen.height * 0.9f, 0);

            Dictionary<string, Action> listeners = new Dictionary<string, Action>
            {
                {"Continue", Continue}, 
                {"Pause", Pause}, 
                {"Reverse", Reverse},
                {"Next", Next},
                {"StepBack", StepBack},
                {"StepIn", StepIn},
                {"StepOut", StepOut},
                {"Restart", Restart},
                {"Stop", Stop },
            };
            foreach (var kv in listeners)
            {
                controls.transform.Find(kv.Key).gameObject.MustGetComponent<Button>().onClick.AddListener(() => actions.Enqueue(kv.Value));
            }
            controls.transform.Find("Terminal").gameObject.MustGetComponent<Button>().onClick.AddListener(OpenConsole);

            return;

            void Continue()
            {
                if (threadId is null || isRunning) return;
                adapterHost.SendRequest(new ContinueRequest { ThreadId = (int)threadId }, _ => isRunning = true);
            }
            void Pause()
            {
                if (threadId is null || !isRunning) return;
                adapterHost.SendRequest(new ContinueRequest { ThreadId = (int)threadId }, _ => isRunning = false);
            }
            void Reverse()
            {
                if (threadId is null || isRunning || capabilities.SupportsStepBack != true) return;
                adapterHost.SendRequest(new ReverseContinueRequest { ThreadId = (int)threadId }, _ => isRunning = true);
            }
            void Next()
            {
                if (threadId is null || isRunning) return;
                adapterHost.SendRequest(new NextRequest { ThreadId = (int)threadId }, _ => isRunning = true);
            }
            void StepBack()
            {
                if (threadId is null || isRunning || capabilities?.SupportsStepBack != true) return;
                adapterHost.SendRequest(new StepBackRequest { ThreadId = (int)threadId }, _ => isRunning = true);
            }
            void StepIn()
            {
                if (threadId is null || isRunning) return;
                adapterHost.SendRequest(new StepInRequest { ThreadId = (int)threadId }, _ => isRunning = true);
            }
            void StepOut()
            {
                if (threadId is null || isRunning) return;
                adapterHost.SendRequest(new StepOutRequest { ThreadId = (int)threadId }, _ => isRunning = true);
            }
            void Restart()
            {
                if (capabilities?.SupportsRestartRequest != true) return;
                adapterHost.SendRequest(new RestartRequest { Arguments = Adapter.GetLaunchRequest(capabilities) }, _ => isRunning = true);
            }
            void Stop()
            {
                adapterHost.SendRequest(new DisconnectRequest(), _ => { });
            }
        }

        private void OpenConsole()
        {
            WindowSpace manager = WindowSpaceManager.ManagerInstance[WindowSpaceManager.LocalPlayer];
            if (console == null)
            {
                console = gameObject.AddComponent<ConsoleWindow>();
                manager.AddWindow(console);
            }
            if (manager.ActiveWindow != console)
            {
                manager.ActiveWindow = console;
            }
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
            adapterProcess.Exited += (_, args) => console.AddMessage($"Process: Exited! ({adapterProcess.ProcessName})");
            adapterProcess.Disposed += (_, args) => console.AddMessage($"Process: Exited! ({adapterProcess.ProcessName})");
            adapterProcess.OutputDataReceived += (_, args) => console.AddMessage($"Process: OutputDataReceived! ({adapterProcess.ProcessName})\n\t{args.Data}");
            adapterProcess.ErrorDataReceived += (_, args) => LogError(new($"Process: ErrorDataReceived! ({adapterProcess.ProcessName})\n\t{args.Data}"));

            string currentDirectory = Directory.GetCurrentDirectory();
            // working directory needs to be set manually so that executables can be found
            try
            {
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

        private void OnEventReceived(object sender, EventReceivedEventArgs e)
        {
            if (e.Body is InitializedEvent)
            {
                actions.Enqueue(() =>
                {
                    List<Action> launchActions = Adapter.GetLaunchActions(adapterHost, capabilities);
                    Action last = launchActions.Last();
                    launchActions[launchActions.Count - 1] = () => { last(); isRunning = true; };
                    launchActions.ForEach(actions.Enqueue);
                });
            }
            else if (e.Body is OutputEvent outputEvent)
            {
                ConsoleWindow.MessageSource source = outputEvent.Category switch
                {
                    OutputEvent.CategoryValue.Console => ConsoleWindow.MessageSource.Adapter,
                    OutputEvent.CategoryValue.Stdout => ConsoleWindow.MessageSource.Debugee,
                    OutputEvent.CategoryValue.Stderr => ConsoleWindow.MessageSource.Debugee,
                    OutputEvent.CategoryValue.Telemetry => ConsoleWindow.MessageSource.Adapter,
                    OutputEvent.CategoryValue.MessageBox => ConsoleWindow.MessageSource.Adapter,
                    OutputEvent.CategoryValue.Exception => ConsoleWindow.MessageSource.Adapter,
                    OutputEvent.CategoryValue.Important => ConsoleWindow.MessageSource.Adapter,
                    OutputEvent.CategoryValue.Unknown => ConsoleWindow.MessageSource.Adapter,
                    null => ConsoleWindow.MessageSource.Adapter,
                    _ => ConsoleWindow.MessageSource.Adapter,
                };
                ConsoleWindow.MessageLevel? level = outputEvent.Category switch
                {
                    OutputEvent.CategoryValue.Console => ConsoleWindow.MessageLevel.Log,
                    OutputEvent.CategoryValue.Stdout => ConsoleWindow.MessageLevel.Log,
                    OutputEvent.CategoryValue.Stderr => ConsoleWindow.MessageLevel.Error,
                    OutputEvent.CategoryValue.Telemetry => null,
                    OutputEvent.CategoryValue.MessageBox => ConsoleWindow.MessageLevel.Warning,
                    OutputEvent.CategoryValue.Exception => ConsoleWindow.MessageLevel.Error,
                    OutputEvent.CategoryValue.Important => ConsoleWindow.MessageLevel.Warning,
                    OutputEvent.CategoryValue.Unknown => ConsoleWindow.MessageLevel.Log,
                    null => ConsoleWindow.MessageLevel.Log,
                    _ => ConsoleWindow.MessageLevel.Log,
                };
                if (level is not null)
                {
                    if (level == ConsoleWindow.MessageLevel.Error)
                    {
                        Debug.LogWarning(outputEvent.Output);
                    }
                    // FIXME: Why does it require a cast?
                    console.AddMessage(outputEvent.Output, source, (ConsoleWindow.MessageLevel)level);
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

                console.AddMessage($"Exited with exit code {exitedEvent.ExitCode}\n", ConsoleWindow.MessageSource.Debugee, ConsoleWindow.MessageLevel.Log);
                actions.Enqueue(() => Destroyer.Destroy(this));
            }
            else if (e.Body is StoppedEvent stoppedEvent)
            {
                isRunning = false;
                switch (stoppedEvent.Reason)
                {
                    case StoppedEvent.ReasonValue.Step:
                        break;
                    case StoppedEvent.ReasonValue.Breakpoint:
                        break;
                    case StoppedEvent.ReasonValue.Exception:
                        break;
                    case StoppedEvent.ReasonValue.Pause:
                        break;
                    case StoppedEvent.ReasonValue.Entry:
                        break;
                    case StoppedEvent.ReasonValue.InstructionBreakpoint:
                        break;
                    case StoppedEvent.ReasonValue.Restart:
                        break;
                    case StoppedEvent.ReasonValue.FunctionBreakpoint:
                    case StoppedEvent.ReasonValue.DataBreakpoint:
                    case StoppedEvent.ReasonValue.Goto:
                    case StoppedEvent.ReasonValue.Unknown:
                        break;
                }
                console.AddMessage($"Stopped - {stoppedEvent.Reason} - {stoppedEvent.PreserveFocusHint} - [{String.Join(", ", stoppedEvent.HitBreakpointIds)}]" +
                    (stoppedEvent.Description != null ? $"\n\tDescription: {stoppedEvent.Description}" : "") +
                    (stoppedEvent.Text != null ? $"\n\tDescription: {stoppedEvent.Text}" : "") +
                    $"\n", ConsoleWindow.MessageSource.Debugee, ConsoleWindow.MessageLevel.Log);
                actions.Enqueue(() =>
                {
                    if (threadId == null) return;
                    StackTraceResponse response = adapterHost.SendRequestSync(new StackTraceRequest()
                    {
                        ThreadId = (int) threadId,
                        Levels = 1
                    });
                    StackFrame stackFrame = response.StackFrames[0];
                    UpdateCodePosition(stackFrame.Source.Path, stackFrame.Line);
                });
            }
            else if (e.Body is ThreadEvent threadEvent)
            {
                if (threadEvent.Reason == ThreadEvent.ReasonValue.Started)
                {
                    threads.Add(threadEvent.ThreadId);
                } else if (threadEvent.Reason == ThreadEvent.ReasonValue.Exited)
                {
                    threads.Remove(threadEvent.ThreadId);
                }
            } else if (e.Body is ContinuedEvent)
            {
                isRunning = true;
            }
        }

        private void UpdateCodePosition(string path, int line)
        {
            string title = Path.GetFileName(path);

            WindowSpace manager = WindowSpaceManager.ManagerInstance[WindowSpaceManager.LocalPlayer];
            CodeWindow codeWindow = manager.Windows.OfType<CodeWindow>().FirstOrDefault(window => window.Title == title);
            if (codeWindow == null)
            {
                codeWindow = gameObject.AddComponent<CodeWindow>();
                codeWindow.Title = title;
                codeWindow.EnterFromFile(path);
                manager.AddWindow(codeWindow);
            }
            codeWindow.ScrolledVisibleLine = line;
            manager.ActiveWindow = codeWindow;
        }

        private bool CreateAdapterHost()
        {
            adapterHost = new DebugProtocolHost(adapterProcess.StandardInput.BaseStream, adapterProcess.StandardOutput.BaseStream);
            adapterHost.DispatcherError += (sender, args) => LogError(new($"DispatcherError - {args.Exception}"));
            adapterHost.ResponseTimeThresholdExceeded += (_, args) => console.AddMessage($"ResponseTimeThresholdExceeded - \t{args.Command}\t{args.SequenceId}\t{args.Threshold}\n", ConsoleWindow.MessageSource.Adapter, ConsoleWindow.MessageLevel.Warning);
            adapterHost.EventReceived += OnEventReceived;
            adapterHost.Run();

            return adapterHost.IsRunning;
        }

        private void OnDestroy()
        {
            console.AddMessage("Debug session finished.\n");
            if (controls)
            {
                Destroyer.Destroy(controls);
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

        private void LogError(Exception e)
        {
            console.AddMessage(e.ToString() + "\n", ConsoleWindow.MessageSource.Adapter, ConsoleWindow.MessageLevel.Error);
            throw e;
        }
    }
}