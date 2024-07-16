using Michsky.UI.ModernUIPack;
using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol;
using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages;
using SEE.Controls;
using SEE.GO;
using SEE.UI.Window;
using SEE.UI.Window.ConsoleWindow;
using SEE.UI.Window.VariablesWindow;
using SEE.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SEE.UI.DebugAdapterProtocol
{
    /// <summary>
    /// This part of the <see cref="DebugAdapterProtocolSession"/> class sets up the controls for the debug session.
    /// </summary>
    public partial class DebugAdapterProtocolSession
    {
        /// <summary>
        /// Sets up the debug controls.
        /// Hides controls which can't be used.
        /// </summary>
        private void SetupControls()
        {
            controls = PrefabInstantiator.InstantiatePrefab(debugControlsPrefab, transform, false);
            controls.transform.position = new Vector3(Screen.width * 0.5f, Screen.height * 0.9f, 0);

            actions.Enqueue(() =>
            {
                Dictionary<string, (Action, string)> listeners = new()
                {
                    { "Continue", (OnContinue, "Continue") },
                    { "Pause", (OnPause, "Pause") },
                    { "Reverse", (OnReverseContinue, "Reverse") },
                    { "StepOver", (OnNext, "Step Over") },
                    { "StepBack", (OnStepBack, "Step Back") },
                    { "StepIn", (OnStepIn, "Step In") },
                    { "StepOut", (OnStepOut, "Step Out") },
                    { "Restart", (OnRestart, "Restart") },
                    { "Stop", (OnStop, "Stop") },
                    { "Console", (() => OpenConsole(), "Console") },
                    { "Variables", (OpenVariables, "Variables") },
                    { "CodePosition", (() => ShowCodePosition(true, true, highlightDurationInitial), "Code Position") }
                };
                foreach (var (name, (action, description)) in listeners)
                {
                    GameObject button = controls.transform.Find(name).gameObject;
                    button.MustGetComponent<ButtonManagerBasic>().clickEvent.AddListener(() => action());
                    if (button.TryGetComponentOrLog(out PointerHelper pointerHelper))
                    {
                        pointerHelper.EnterEvent.AddListener(_ => Tooltip.ActivateWith(description));
                        pointerHelper.ExitEvent.AddListener(_ => Tooltip.Deactivate());
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
                        { "Reverse", capabilities.SupportsStepBack },
                        { "StepBack", capabilities.SupportsStepBack },
                        { "Restart", capabilities.SupportsRestartRequest },
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
    }
}
