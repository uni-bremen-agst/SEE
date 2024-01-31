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
        private bool isInitialized;

        private Queue<Action> queuedRequests = new();


        protected void Start()
        {
            OpenConsole();
            SetupControls();

            if (Adapter == null)
            {
                Debug.LogError("Debug adapter not set.");
                Destroyer.Destroy(this);
                return;
            }
            console.AddMessage($"Start debugging session: {Adapter.Name} - {Adapter.AdapterFileName} - {Adapter.AdapterArguments} - {Adapter.AdapterWorkingDirectory}");

            if (!CreateAdapterProcess())
            {
                console.AddMessage("Couldn't create the debug adapter process.", ConsoleWindow.MessageSource.Adapter, ConsoleWindow.MessageLevel.Error);
                Destroyer.Destroy(this);
                return;
            } else
            {
                console.AddMessage("Created the debug adapter process.", ConsoleWindow.MessageSource.Adapter, ConsoleWindow.MessageLevel.Log);
            }
            if (!CreateAdapterHost())
            {
                console.AddMessage("Couldn't create the debug adapter host.", ConsoleWindow.MessageSource.Adapter, ConsoleWindow.MessageLevel.Error);
                Destroyer.Destroy(this);
                return;
            } else
            {
                console.AddMessage("Created the debug adapter host.", ConsoleWindow.MessageSource.Adapter, ConsoleWindow.MessageLevel.Log);
            }

            capabilities = adapterHost.SendRequestSync(new InitializeRequest()
            {
                ClientID = "SEE",
                ClientName = "Software Engineering Experience",
                AdapterID = Adapter.Name,
                PathFormat = InitializeArguments.PathFormatValue.Path,
            });
        }

        private void SetupControls()
        {
            controls = PrefabInstantiator.InstantiatePrefab(DebugControlsPrefab, transform, false);
            controls.transform.position = new Vector3(Screen.width * 0.5f, Screen.height * 0.9f, 0);

            Button terminalButton = controls.transform.Find("Terminal").gameObject.MustGetComponent<Button>();
            terminalButton.onClick.AddListener(OpenConsole);

            Button stopButton = controls.transform.Find("Stop").gameObject.MustGetComponent<Button>();
            stopButton.onClick.AddListener(() => Destroyer.Destroy(this));
        }

        private void OpenConsole()
        {
            WindowSpace manager = WindowSpaceManager.ManagerInstance[WindowSpaceManager.LocalPlayer];
            if (console == null)
            {
                console = gameObject.AddComponent<ConsoleWindow>();
                console.AddMessage("Console created");
                manager.AddWindow(console);
            }
            console.AddMessage("Console opened");
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
            } catch (Exception e) {
                console.AddMessage(e.ToString(), ConsoleWindow.MessageSource.Adapter, ConsoleWindow.MessageLevel.Error);
                adapterProcess = null;
            }
            // working directory needs to be reset (otherwise unity crashes)
            Directory.SetCurrentDirectory(currentDirectory);

            return adapterProcess != null && !adapterProcess.HasExited;
        }

        private void Launch()
        {
            queuedRequests.Enqueue(() =>
            {
                if (capabilities.SupportsConfigurationDoneRequest == true)
                {
                    adapterHost.SendRequestSync(new ConfigurationDoneRequest());
                } else
                {
                    adapterHost.SendRequestSync(new SetExceptionBreakpointsRequest());
                }
            });

            queuedRequests.Enqueue(() => adapterHost.SendRequestSync(Adapter.GetLaunchRequest(capabilities)));
        }

        private void Update()
            if (queuedRequests.Count > 0)
            {
                try
                {
                    queuedRequests.Dequeue()();
                } catch (Exception e)
                {
                    console.AddMessage(e.ToString(), ConsoleWindow.MessageSource.Debugee, ConsoleWindow.MessageLevel.Error);
                }
            }
        }

        private void OnEventReceived(object sender, EventReceivedEventArgs e)
        {
            if (e.Body is InitializedEvent) {
                isInitialized = true;
                Launch();
            } else if (e.Body is OutputEvent outputEvent) {
                ConsoleWindow.MessageSource source;
                ConsoleWindow.MessageLevel level;
                switch (outputEvent.Category)
                {
                    case OutputEvent.CategoryValue.Console:
                    case OutputEvent.CategoryValue.MessageBox:
                    case OutputEvent.CategoryValue.Unknown:
                        (source, level) = (ConsoleWindow.MessageSource.Adapter, ConsoleWindow.MessageLevel.Log);
                        break;
                    case OutputEvent.CategoryValue.Important:
                        (source, level) = (ConsoleWindow.MessageSource.Adapter, ConsoleWindow.MessageLevel.Warning);
                        break;
                    case OutputEvent.CategoryValue.Exception:
                        (source, level) = (ConsoleWindow.MessageSource.Adapter, ConsoleWindow.MessageLevel.Error);
                        break;
                    case OutputEvent.CategoryValue.Stdout:
                        (source, level) = (ConsoleWindow.MessageSource.Debugee, ConsoleWindow.MessageLevel.Log);
                        break;
                    case OutputEvent.CategoryValue.Stderr:
                        (source, level) = (ConsoleWindow.MessageSource.Debugee, ConsoleWindow.MessageLevel.Error);
                        break;
                    case null:
                    default:
                    case OutputEvent.CategoryValue.Telemetry:
                        return;
                }
                console.AddMessage(outputEvent.Output, source, level);
            }
            else
            {
                console.AddMessage("Event Received: " + e.EventType, ConsoleWindow.MessageSource.Debugee, ConsoleWindow.MessageLevel.Log);
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
            console.AddMessage("Debug session finished.");
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