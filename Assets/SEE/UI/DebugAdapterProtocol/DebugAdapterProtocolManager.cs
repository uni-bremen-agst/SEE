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
    /// <summary>
    /// Manages the buttons of the BaseWindow that are used for debugging.
    ///
    /// <para>
    /// Listens to these buttons of the *BaseWindow*:
    /// </para>
    /// <list type="table">
    /// <item>
    /// <term>Run</term>
    /// <description>Starts the debug session.</description>
    /// </item>
    /// <item>
    /// <term>Adapter Configuration</term>
    /// <description>Opens the debug adapter configration dialog.</description>
    /// </item>
    /// <item>
    /// <term>Launch Configuration</term>
    /// <description>Opens the launch configuration dialog.</description>
    /// </item>
    /// </list>
    /// </summary>
    public class DebugAdapterProtocolManager : PlatformDependentComponent
    {
        /// <summary>
        /// List of possible debug adapters.
        /// </summary>
        private static readonly DebugAdapter.DebugAdapter[] adapters =
        {
            new NetCoreDebugAdapter(),
            new JavaScriptDebugAdapter(),
            new GDBDebugAdapter(),
            new MockDebugAdapter(),
        };

        /// <summary>
        /// Selected debug adapter.
        /// </summary>
        private static DebugAdapter.DebugAdapter adapter = adapters[0];

        /// <summary>
        /// All cities.
        /// </summary>
        private static IList<AbstractSEECity> cities;

        /// <summary>
        /// Selected city in which the code position should be highlighted.
        /// </summary>
        private static AbstractSEECity city;

        /// <summary>
        /// Active debug session.
        /// </summary>
        private static DebugAdapterProtocolSession session;

        /// <summary>
        /// The run button.
        /// </summary>
        public GameObject RunButton;

        /// <summary>
        /// The debug adapter configuration button.
        /// </summary>
        public GameObject DebugAdapterConfigButton;

        /// <summary>
        /// The launch configuration button.
        /// </summary>
        public GameObject LaunchConfigButton;

        /// <summary>
        /// Retrieves all cities.
        /// </summary>
        protected override void Start()
        {
            base.Start();
            if (cities == null)
            {
                cities = GameObject.FindGameObjectsWithTag(Tags.CodeCity).Select(go => go.MustGetComponent<AbstractSEECity>()).OrderBy(c => c.name).ToList();
                city = cities.First();
            }
        }

        /// <summary>
        /// Adds listeners and tooltips to the BaseWindow buttons.
        /// </summary>
        protected override void StartDesktop() {
            RunButton.MustGetComponent<ButtonManagerBasic>().clickEvent.AddListener(Run);
            DebugAdapterConfigButton.MustGetComponent<ButtonManagerBasic>().clickEvent.AddListener(OpenDebugAdapterConfig);
            LaunchConfigButton.MustGetComponent<ButtonManagerBasic>().clickEvent.AddListener(OpenLaunchConfig);

            PointerHelper pointerHelper = RunButton.MustGetComponent<PointerHelper>();
            pointerHelper.EnterEvent.AddListener(_ => Tooltip.ActivateWith("Run"));
            pointerHelper.ExitEvent.AddListener(_ => Tooltip.Deactivate());

            pointerHelper = DebugAdapterConfigButton.MustGetComponent<PointerHelper>();
            pointerHelper.EnterEvent.AddListener(_ => Tooltip.ActivateWith("Debug Adapter"));
            pointerHelper.ExitEvent.AddListener(_ => Tooltip.Deactivate());

            pointerHelper = LaunchConfigButton.MustGetComponent<PointerHelper>();
            pointerHelper.EnterEvent.AddListener(_ => Tooltip.ActivateWith("Launch Configuration"));
            pointerHelper.ExitEvent.AddListener(_ => Tooltip.Deactivate());
        }

        protected override void StartVR()
        {
            StartDesktop();
        }

        /// <summary>
        /// Starts a debug session.
        /// </summary>
        public static void Run()
        {
            if (session == null)
            {
                session = Canvas.AddComponent<DebugAdapterProtocolSession>();
                session.Adapter = adapter;
                session.City = city;
            }
        }

        /// <summary>
        /// Opens the debug adapter configuration dialog.
        /// </summary>
        public static void OpenDebugAdapterConfig()
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
                debugAdapterProperty.HorizontalSelector.selectorEvent.AddListener(_ =>
                {
                    UpdateValues();
                    adapter = adapters[debugAdapterProperty.HorizontalSelector.index];
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

        /// <summary>
        /// Opens the launch configuration dialog.
        /// </summary>
        public static void OpenLaunchConfig()
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
