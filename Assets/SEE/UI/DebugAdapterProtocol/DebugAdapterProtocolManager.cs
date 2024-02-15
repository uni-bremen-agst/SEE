using Michsky.UI.ModernUIPack;
using SEE.Controls;
using SEE.Game;
using SEE.Game.City;
using SEE.GO;
using SEE.UI.DebugAdapterProtocol.DebugAdapter;
using SEE.UI.PropertyDialog;
using SEE.Utils;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SEE.UI.DebugAdapterProtocol
{
    public class DebugAdapterProtocolManager : PlatformDependentComponent
    {
        private static readonly DebugAdapter.DebugAdapter[] adapters =
        {
            new NetCoreDebugAdapter(),
            new MockDebugAdapter(),
        };
        private static DebugAdapter.DebugAdapter adapter = adapters[0];
        private static DebugAdapterProtocolSession session;

        private Tooltip.Tooltip tooltip;

        public GameObject RunButton;

        public GameObject DebugAdapterConfigButton;

        public GameObject LaunchConfigButton;

        IEnumerable<AbstractSEECity> cities;
        AbstractSEECity city;

        protected override void Start()
        {
            base.Start();
            cities = GameObject.FindGameObjectsWithTag(Tags.CodeCity).Select(go => go.MustGetComponent<AbstractSEECity>()).OrderBy(c => c.name);
            city = cities.First();
        }

        protected override void StartDesktop() {
            tooltip = gameObject.AddComponent<Tooltip.Tooltip>();

            RunButton.MustGetComponent<ButtonManagerBasic>().clickEvent.AddListener(Run);
            DebugAdapterConfigButton.MustGetComponent<ButtonManagerBasic>().clickEvent.AddListener(OpenDebugAdapterConfig);
            LaunchConfigButton.MustGetComponent<ButtonManagerBasic>().clickEvent.AddListener(OpenLaunchConfig);

            PointerHelper pointerHelper;
            if (RunButton.TryGetComponent<PointerHelper>(out pointerHelper))
            {
                pointerHelper.EnterEvent.AddListener(_ => tooltip.Show("Run"));
                pointerHelper.ExitEvent.AddListener(_ => tooltip.Hide());
            }
            if (DebugAdapterConfigButton.TryGetComponent<PointerHelper>(out pointerHelper))
            {
                pointerHelper.EnterEvent.AddListener(_ => tooltip.Show("Debug Adapter"));
                pointerHelper.ExitEvent.AddListener(_ => tooltip.Hide());
            }
            if (LaunchConfigButton.TryGetComponent<PointerHelper>(out pointerHelper))
            {
                pointerHelper.EnterEvent.AddListener(_ => tooltip.Show("Launch Configuration"));
                pointerHelper.ExitEvent.AddListener(_ => tooltip.Hide());
            }
        }

        public void Run()
        {
            if (session == null)
            {
                session = Canvas.AddComponent<DebugAdapterProtocolSession>();
                session.Adapter = adapter;
                session.City = city;
            }
        }

        public void OpenDebugAdapterConfig()
        {
            GameObject go = new GameObject("Debug Adapter Configuration");

            // create property group
            PropertyGroup group = go.gameObject.AddComponent<PropertyGroup>();
            group.Name = "Debug Adapter Group";
            group.Compact = true;

            SelectionProperty debugAdapterProperty = go.AddComponent<SelectionProperty>();
            debugAdapterProperty.Name = "Debug Adapter";
            debugAdapterProperty.Description = "The type of the debug adapter.";
            debugAdapterProperty.AddOptions(adapters.Select(adapter => adapter.Name));
            debugAdapterProperty.Value = adapter.Name;
            group.AddProperty(debugAdapterProperty);

            FilePathProperty workingDirectoryProperty = go.AddComponent<FilePathProperty>();
            workingDirectoryProperty.Name = "Working Directory";
            workingDirectoryProperty.Description = "The working directory of the debug adapter.";
            workingDirectoryProperty.PickMode = SimpleFileBrowser.FileBrowser.PickMode.Folders;
            group.AddProperty(workingDirectoryProperty);

            FilePathProperty fileNameProperty = go.AddComponent<FilePathProperty>();
            fileNameProperty.Name = "File Name";
            fileNameProperty.Description = "The file name (executable) of the debug adapter.";
            fileNameProperty.FallbackDirectory = adapter.AdapterWorkingDirectory;
            group.AddProperty(fileNameProperty);

            StringProperty argumentsProperty = go.AddComponent<StringProperty>();
            argumentsProperty.Name = "Arguments";
            argumentsProperty.Description = "The arguments of the debug adapter.";
            group.AddProperty(argumentsProperty);

            SelectionProperty cityProperty = go.AddComponent<SelectionProperty>();
            cityProperty.AddOptions(cities.Select(c => c.name));
            cityProperty.Value = city.name;
            cityProperty.Description = "City used where current execution position is highlighted.";
            group.AddProperty(cityProperty);

            OnAdapterChanged();
            debugAdapterProperty.OnComponentInitialized += () =>
            {
                debugAdapterProperty.horizontalSelector.selectorEvent.AddListener(_ =>
                {
                    UpdateValues();
                    adapter = adapters[debugAdapterProperty.horizontalSelector.index];
                    OnAdapterChanged();
                });
            };

            // create dialog
            PropertyDialog.PropertyDialog dialog = go.AddComponent<PropertyDialog.PropertyDialog>();
            dialog.Title = "Debug Adapter";
            dialog.Description = "Enter the debug adapter configuration.";
            dialog.AddGroup(group);
            dialog.OnConfirm.AddListener(UpdateValues);

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
            void OnAdapterChanged()
            {
                workingDirectoryProperty.Value = adapter.AdapterWorkingDirectory;
                fileNameProperty.Value = adapter.AdapterFileName;
                argumentsProperty.Value = adapter.AdapterArguments;
            }
            void UpdateValues()
            {
                adapter.AdapterWorkingDirectory = workingDirectoryProperty.Value ;
                adapter.AdapterFileName = fileNameProperty.Value ;
                adapter.AdapterArguments  = argumentsProperty.Value ;
                city = cities.First(c => c.name == cityProperty.Value);
            }
        }

        public void OpenLaunchConfig()
        {
            GameObject go = new GameObject("Launch Request");

            // create property group
            PropertyGroup group = go.AddComponent<PropertyGroup>();
            group.Name = "Launch Request Group";
            group.Compact = true;
            adapter.SetupLaunchConfig(go, group);

            // create dialog
            PropertyDialog.PropertyDialog dialog = go.AddComponent<PropertyDialog.PropertyDialog>();

            dialog.Title = "Launch Configuration";
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