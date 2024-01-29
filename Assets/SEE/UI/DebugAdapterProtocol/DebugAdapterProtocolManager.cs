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
        private static readonly DebugAdapter.DebugAdapter[] adapters =
        {
            new MockDebugAdapter(),
            new NetCoreDebugAdapter()
        };
        private static DebugAdapter.DebugAdapter adapter = adapters[0];
        private static DebugAdapterProtocolSession session;

        protected override void StartDesktop() {}


        public void Run()
        {
            if (session == null)
            {
                session = Canvas.AddComponent<DebugAdapterProtocolSession>();
                session.Adapter = adapter;
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
            debugAdapterProperty.AddOptions(adapters.Select(adapter => adapter.Name));
            debugAdapterProperty.Value = adapter.Name;
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
                    adapter = adapters[debugAdapterProperty.horizontalSelector.index];
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
                workingDirectoryProperty.Value = adapter.AdapterWorkingDirectory;
                fileNameProperty.Value = adapter.AdapterFileName;
                argumentsProperty.Value = adapter.AdapterArguments;
            }
            void UpdateAdapterValues()
            {
                adapter.AdapterWorkingDirectory = workingDirectoryProperty.Value ;
                adapter.AdapterFileName = fileNameProperty.Value ;
                adapter.AdapterArguments  = argumentsProperty.Value ;
            }
        }

        public void OpenLaunchConfig()
        {
            GameObject go = new GameObject("Launch Request");

            // create property group
            PropertyGroup group = go.gameObject.AddComponent<PropertyGroup>();
            group.Name = "Launch Request Group";
            adapter.SetupLaunchConfig(go, group);

            // create dialog
            PropertyDialog.PropertyDialog dialog = go.AddComponent<PropertyDialog.PropertyDialog>();

            dialog.Title = "Launch Request Configuration";
            dialog.Description = "Enter the launch request configuration.";
            dialog.AddGroup(group);
            dialog.OnConfirm.AddListener(adapter.SaveLaunchConfig);

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