using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol;
using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages;
using SEE.Controls;
using SEE.GO;
using SEE.UI.Window;
using SEE.UI.Window.ConsoleWindow;
using SEE.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;
using Encoding = System.Text.Encoding;

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

        private Queue<Action> queuedActions = new();

        private int? threadId;
        private object? restartData;


        protected void Start()
        {
            OpenConsole();
            SetupControls();

            if (Adapter == null)
            {
                Debug.LogError("Debug adapter not set.");
                console.AddMessage("Debug adapter not set.\n", ConsoleWindow.MessageSource.Adapter, ConsoleWindow.MessageLevel.Error);
                Destroyer.Destroy(this);
                return;
            }
            console.AddMessage($"Start debugging session: " +
                $"{Adapter.Name} - {Adapter.AdapterFileName} - {Adapter.AdapterArguments} - {Adapter.AdapterWorkingDirectory}\n");

            if (!CreateAdapterProcess())
            {
                console.AddMessage("Couldn't create the debug adapter process.\n", ConsoleWindow.MessageSource.Adapter, ConsoleWindow.MessageLevel.Error);
                Destroyer.Destroy(this);
                return;
            }
            else
            {
                console.AddMessage("Created the debug adapter process.\n", ConsoleWindow.MessageSource.Adapter, ConsoleWindow.MessageLevel.Log);
            }
            if (!CreateAdapterHost())
            {
                console.AddMessage("Couldn't create the debug adapter host.\n", ConsoleWindow.MessageSource.Adapter, ConsoleWindow.MessageLevel.Error);
                Destroyer.Destroy(this);
                return;
            }
            else
            {
                console.AddMessage("Created the debug adapter host.\n", ConsoleWindow.MessageSource.Adapter, ConsoleWindow.MessageLevel.Log);
            }

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
        }

        private void SetupControls()
        {
            controls = PrefabInstantiator.InstantiatePrefab(DebugControlsPrefab, transform, false);
            controls.transform.position = new Vector3(Screen.width * 0.5f, Screen.height * 0.9f, 0);

            Button continueButton = controls.transform.Find("Continue").gameObject.MustGetComponent<Button>();
            continueButton.onClick.AddListener(() =>
            {
                if (threadId is not null)
                {
                    queuedActions.Enqueue(() => adapterHost.SendRequest(new ContinueRequest { ThreadId = (int)threadId }, _ => { }));
                }
            });

            Button pauseButton = controls.transform.Find("Pause").gameObject.MustGetComponent<Button>();
            pauseButton.onClick.AddListener(() => queuedActions.Enqueue(() =>
            {
                if (threadId is not null)
                {
                    adapterHost.SendRequest(new PauseRequest { ThreadId = (int)threadId }, _ => { });
                }
            }));

            Button reverseButton = controls.transform.Find("Reverse").gameObject.MustGetComponent<Button>();
            reverseButton.onClick.AddListener(() =>
            {
                if (capabilities.SupportsStepBack == true && threadId is not null)
                {
                    queuedActions.Enqueue(() => adapterHost.SendRequest(new ReverseContinueRequest{ ThreadId = (int)threadId }, _ => { }));
                }
            });

            Button nextButton = controls.transform.Find("Next").gameObject.MustGetComponent<Button>();
            nextButton.onClick.AddListener(() =>
            {
                if (threadId is not null)
                {
                    queuedActions.Enqueue(() => adapterHost.SendRequest(new NextRequest{ ThreadId = (int)threadId }, _ => { }));
                }
            });

            Button stepBackButton = controls.transform.Find("StepBack").gameObject.MustGetComponent<Button>();
            stepBackButton.onClick.AddListener(() =>
            {
                if (capabilities.SupportsStepBack == true && threadId is not null)
                {
                    queuedActions.Enqueue(() => adapterHost.SendRequest(new StepBackRequest{ ThreadId = (int)threadId }, _ => { }));
                }
            });

            Button stepInButton = controls.transform.Find("StepIn").gameObject.MustGetComponent<Button>();
            stepInButton.onClick.AddListener(() =>
            {
                if (threadId is not null)
                {
                    queuedActions.Enqueue(() => adapterHost.SendRequest(new StepInRequest{ ThreadId = (int)threadId }, _ => { }));
                }
            });

            Button stepOutButton = controls.transform.Find("StepOut").gameObject.MustGetComponent<Button>();
            stepOutButton.onClick.AddListener(() =>
            {
                if (threadId is not null)
                {
                    queuedActions.Enqueue(() => adapterHost.SendRequest(new StepOutRequest{ ThreadId = (int)threadId }, _ => { }));
                }
            });

            Button restartButton = controls.transform.Find("Restart").gameObject.MustGetComponent<Button>();
            restartButton.onClick.AddListener(() =>
            {
                if (capabilities.SupportsRestartRequest==true && threadId is not null)
                {
                    queuedActions.Enqueue(() => adapterHost.SendRequest(new RestartRequest { Arguments = Adapter.GetLaunchRequest(capabilities) }, _ => { }));
                }
            });

            Button stopButton = controls.transform.Find("Stop").gameObject.MustGetComponent<Button>();
            stopButton.onClick.AddListener(() =>
            {
                 queuedActions.Enqueue(() => adapterHost.SendRequest(new DisconnectRequest(), _ => { }));
            });

            Button terminalButton = controls.transform.Find("Terminal").gameObject.MustGetComponent<Button>();
            terminalButton.onClick.AddListener(OpenConsole);
        }

        private void OpenConsole()
        {
            WindowSpace manager = WindowSpaceManager.ManagerInstance[WindowSpaceManager.LocalPlayer];
            if (console == null)
            {
                console = gameObject.AddComponent<ConsoleWindow>();
                console.AddMessage("Console created\n");
                manager.AddWindow(console);
            }
            if (manager.ActiveWindow != console)
            {
                console.AddMessage("Console opened\n");
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
            adapterProcess.Exited += (_, args) => console.AddMessage($"Process: Exited! ({adapterProcess.ProcessName}", ConsoleWindow.MessageSource.Adapter, ConsoleWindow.MessageLevel.Error);
            adapterProcess.Disposed += (_, args) => console.AddMessage($"Process: Exited! ({adapterProcess.ProcessName}", ConsoleWindow.MessageSource.Adapter, ConsoleWindow.MessageLevel.Error);
            adapterProcess.ErrorDataReceived += (_, args) => console.AddMessage($"Process: ErrorDataReceived! ({adapterProcess.ProcessName}\t{args.Data}", ConsoleWindow.MessageSource.Adapter, ConsoleWindow.MessageLevel.Error);
            adapterProcess.OutputDataReceived += (_, args) => console.AddMessage($"Process: OutputDataReceived! ({adapterProcess.ProcessName}\t{args.Data}", ConsoleWindow.MessageSource.Adapter, ConsoleWindow.MessageLevel.Error);

            string currentDirectory = Directory.GetCurrentDirectory();
            // working directory needs to be set manually so that executables can be found
            Directory.SetCurrentDirectory(Adapter.AdapterWorkingDirectory);
            try
            {
                if (!adapterProcess.Start())
                {
                    adapterProcess = null;
                }
            }
            catch (Exception e)
            {
                console.AddMessage(e.ToString(), ConsoleWindow.MessageSource.Adapter, ConsoleWindow.MessageLevel.Error);
                adapterProcess = null;
            }
            // working directory needs to be reset (otherwise unity crashes)
            Directory.SetCurrentDirectory(currentDirectory);

            return adapterProcess != null && !adapterProcess.HasExited;
        }

        private void Update()
        {
            if (queuedActions.Count > 0 && capabilities != null)
            {
                try
                {
                    queuedActions.Dequeue()();
                }
                catch (Exception e)
                {
                    console.AddMessage(e.ToString(), ConsoleWindow.MessageSource.Debugee, ConsoleWindow.MessageLevel.Error);
                }
            }
        }

        private void OnEventReceived(object sender, EventReceivedEventArgs e)
        {
            switch (e.Body)
            {
                case InitializedEvent _:
                    queuedActions.Enqueue(() => adapterHost.SendRequestSync(Adapter.GetLaunchRequest(capabilities)));
                    queuedActions.Enqueue(() =>
                    {
                        if (capabilities.SupportsConfigurationDoneRequest == true)
                        {
                            adapterHost.SendRequestSync(new ConfigurationDoneRequest());
                        }
                    });
                    break;
            }
            if (e.Body is InitializedEvent)
            {
                queuedActions.Enqueue(() => adapterHost.SendRequestSync(Adapter.GetLaunchRequest(capabilities)));
                queuedActions.Enqueue(() =>
                {
                    if (capabilities.SupportsConfigurationDoneRequest == true)
                    {
                        adapterHost.SendRequestSync(new ConfigurationDoneRequest());
                    }
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
                    // FIXME: Why does it require a cast?
                    console.AddMessage(outputEvent.Output, source, (ConsoleWindow.MessageLevel)level);
                }
            }
            else if (e.Body is TerminatedEvent terminatedEvent)
            {
                // TODO: Let user restart the program.
                console.AddMessage("Terminated\n", ConsoleWindow.MessageSource.Debugee, ConsoleWindow.MessageLevel.Log);
                Destroyer.Destroy(this);
            }
            else if (e.Body is ExitedEvent exitedEvent)
            {
                console.AddMessage($"Exited with exit code {exitedEvent.ExitCode}\n", ConsoleWindow.MessageSource.Debugee, ConsoleWindow.MessageLevel.Log);
                queuedActions.Enqueue(() => Destroyer.Destroy(this));
            }
            else if (e.Body is StoppedEvent stoppedEvent)
            {
                threadId = stoppedEvent.ThreadId;
                console.AddMessage($"Stopped\n", ConsoleWindow.MessageSource.Debugee, ConsoleWindow.MessageLevel.Log);
            }
        }

        private bool CreateAdapterHost()
        {
            adapterHost = new DebugProtocolHost(adapterProcess.StandardInput.BaseStream, adapterProcess.StandardOutput.BaseStream);
            adapterHost.LogMessage += (sender, args) => Debug.Log($"LogMessage - {args.Category} - {args.Message}");
            adapterHost.DispatcherError += (sender, args) => Debug.Log($"DispatcherError - {args.Exception}");
            adapterHost.ResponseTimeThresholdExceeded += (_, args) => Debug.Log($"ResponseTimeThresholdExceeded - \t{args.Command}\t{args.SequenceId}\t{args.Threshold}");
            adapterHost.EventReceived += (_, args) => Debug.Log($"EventReceived - {args.EventType}");
            adapterHost.RequestReceived += (_, args) => Debug.Log($"RequestReceived - {args.Command}");
            adapterHost.RequestCompleted += (_, args) => Debug.Log($"RequestCompleted - {args.Command} - {args.SequenceId} - {args.ElapsedTime}");

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

    }
}