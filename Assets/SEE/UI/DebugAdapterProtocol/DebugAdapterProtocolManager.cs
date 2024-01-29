using SEE.Controls;
using SEE.UI.DebugAdapterProtocol.DebugAdapter;
using SEE.UI.PropertyDialog;
using SEE.Utils;
using System.Linq;
using UnityEngine;

namespace SEE.UI.DebugAdapterProtocol
{
    public class DebugAdapterProtocolManager : PlatformDependentComponent
    {
        private static readonly DebugAdapter.DebugAdapter[] debugAdapters =
        {
            new MockDebugAdapter(),
            new NetCoreDebugAdapter()
        };
        private static DebugAdapter.DebugAdapter debugAdapter = debugAdapters[0];
        private static DebugAdapterProtocolSession debugSession;

        protected override void StartDesktop() {}


        public void Run()
        {
            if (debugSession == null)
            {
                debugSession = Canvas.AddComponent<DebugAdapterProtocolSession>();
                debugSession.debugAdapter = debugAdapter;
            }
        }

        public void OpenDebugAdapterConfig()
        {
            Debug.Log("OpenDebugAdapterConfig");
            GameObject go = new GameObject("Debug Adapter Configuration");

            // create property group
            PropertyGroup group = go.gameObject.AddComponent<PropertyGroup>();
            group.Name = "Debug Adapter Group";

            SelectionProperty debugAdapterProperty = go.AddComponent<SelectionProperty>();
            debugAdapterProperty.Name = "Debug Adapter";
            debugAdapterProperty.Description = "The type of the debug adapter.";
            debugAdapterProperty.AddOptions(debugAdapters.Select(adapter => adapter.Name));
            debugAdapterProperty.Value = debugAdapter.Name;
            group.AddProperty(debugAdapterProperty);

            StringProperty workingDirectoryProperty = go.AddComponent<StringProperty>();
            workingDirectoryProperty.Name = "Working Directory";
            workingDirectoryProperty.Description = "The working directory of the debug adapter.";
            group.AddProperty(workingDirectoryProperty);

            StringProperty fileNameProperty = go.AddComponent<StringProperty>();
            fileNameProperty.Name = "File Name";
            fileNameProperty.Description = "The file name (executable) of the debug adapter.";
            group.AddProperty(fileNameProperty);

            StringProperty argumentsProperty = go.AddComponent<StringProperty>();
            argumentsProperty.Name = "Arguments";
            argumentsProperty.Description = "The arguments of the debug adapter.";
            group.AddProperty(argumentsProperty);

            UpdateUIValues();

            debugAdapterProperty.OnComponentInitialized += () =>
            {
                debugAdapterProperty.horizontalSelector.selectorEvent.AddListener(_ =>
                {
                    UpdateAdapterValues();
                    debugAdapter = debugAdapters[debugAdapterProperty.horizontalSelector.index];
                    UpdateUIValues();
                });
            };

            // create dialog
            PropertyDialog.PropertyDialog dialog = go.AddComponent<PropertyDialog.PropertyDialog>();
            dialog.Title = "Debug Adapter";
            dialog.Description = "Enter the debug adapter configuration.";
            dialog.AddGroup(group);
            dialog.OnConfirm.AddListener(UpdateAdapterValues);

            // open dialog
            SEEInput.KeyboardShortcutsEnabled = false;
            dialog.OnCancel.AddListener(OnClose);
            dialog.OnConfirm.AddListener(OnClose);
            dialog.DialogShouldBeShown = true;

            void OnClose()
            {
                SEEInput.KeyboardShortcutsEnabled = true;
                Destroyer.Destroy(go);
            }
            void UpdateUIValues()
            {
                workingDirectoryProperty.Value = debugAdapter.AdapterWorkingDirectory;
                fileNameProperty.Value = debugAdapter.AdapterFileName;
                argumentsProperty.Value = debugAdapter.AdapterArguments;
            }
            void UpdateAdapterValues()
            {
                debugAdapter.AdapterWorkingDirectory = workingDirectoryProperty.Value ;
                debugAdapter.AdapterFileName = fileNameProperty.Value ;
                debugAdapter.AdapterArguments  = argumentsProperty.Value ;
            }
        }

        public void OpenLaunchConfig()
        {
            GameObject go = new GameObject("Launch Request");

            // create property group
            PropertyGroup group = go.gameObject.AddComponent<PropertyGroup>();
            group.Name = "Launch Request Group";
            debugAdapter.SetupLaunchConfig(go, group);

            // create dialog
            PropertyDialog.PropertyDialog dialog = go.AddComponent<PropertyDialog.PropertyDialog>();

            dialog.Title = "Launch Request Configuration";
            dialog.Description = "Enter the launch request configuration.";
            dialog.AddGroup(group);
            dialog.OnConfirm.AddListener(debugAdapter.SaveLaunchConfig);

            // open dialog
            SEEInput.KeyboardShortcutsEnabled = false;
            dialog.OnCancel.AddListener(OnClose);
            dialog.OnConfirm.AddListener(OnClose);
            dialog.DialogShouldBeShown = true;

            void OnClose()
            {
                Destroyer.Destroy(go);
                SEEInput.KeyboardShortcutsEnabled = true;
            }

        }
    }
}