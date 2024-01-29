using SEE.Controls;
using SEE.UI.Window;
using SEE.UI.Window.ConsoleWindow;
using SEE.Utils;
using System;
using UnityEngine;

namespace SEE.UI.DebugAdapterProtocol
{
    public class DebugAdapterProtocolSession : PlatformDependentComponent
    {
        public DebugAdapter.DebugAdapter debugAdapter;

        private ConsoleWindow consoleWindow;
        private DebugAdapterProtocolSessionControls controls;

        protected override void Start()
        {
            base.Start();
            Debug.Log("Starting Debug Adapter Protocol Session");
            if (debugAdapter == null)
            {
                Debug.LogError("Debug adapter not set.");
                Destroyer.Destroy(this);
                return;
            }
            OpenConsole();
            controls = gameObject.AddComponent<DebugAdapterProtocolSessionControls>();
            controls.OnTerminalButtonClicked += OpenConsole;
        }

        void OpenConsole()
        {
            WindowSpace manager = WindowSpaceManager.ManagerInstance[WindowSpaceManager.LocalPlayer];
            if (consoleWindow == null)
            {
                consoleWindow = gameObject.AddComponent<ConsoleWindow>();
                foreach (ConsoleWindow.MessageSource source in Enum.GetValues(typeof(ConsoleWindow.MessageSource)))
                {
                    foreach (ConsoleWindow.MessageLevel level in Enum.GetValues(typeof(ConsoleWindow.MessageLevel)))
                    {
                        consoleWindow.AddMessage($"Hello - {source} - {level}", source, level);
                    }
                }
                manager.AddWindow(consoleWindow);
            }
            manager.ActiveWindow = consoleWindow;
        }

        protected override void StartDesktop()
        {
        }
    }
}