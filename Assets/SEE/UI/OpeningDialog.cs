using System;
using System.Collections.Generic;
using SEE.GO;
using SEE.UI.Menu;
using SEE.UI.Notification;
using SEE.UI.PropertyDialog;
using SEE.Utils;
using Sirenix.Utilities;
using UnityEngine;

namespace SEE.UI
{
    /// <summary>
    /// Implements the behaviour of the in-game menu for the selection of the general
    /// configuration (host, server, client, network and user-settings) that is shown at the start up.
    /// </summary>
    internal class OpeningDialog : MonoBehaviour
    {
        /// <summary>
        /// The UI object representing the menu the user chooses the action from.
        /// </summary>
        private SimpleListMenu menu;

        /// <summary>
        /// This creates and returns the action menu, with which a user can configure the
        /// networking.
        /// </summary>
        /// <returns>the newly created action menu component.</returns>
        private SimpleListMenu CreateMenu()
        {
            GameObject actionMenuGO = new() { name = "Start Menu" };
            IList<MenuEntry> entries = SelectionEntries();
            SimpleListMenu actionMenu = actionMenuGO.AddComponent<SimpleListMenu>();
            actionMenu.AllowNoSelection = false; // the menu cannot be closed; user must make a decision
            actionMenu.Title = "Configuration";
            actionMenu.Description = "Please select the configuration you want to activate.";
            entries.ForEach(actionMenu.AddEntry);
            // We will handle the closing of the menu ourselves: we need to wait until a network
            // connection can be established.
            actionMenu.HideAfterSelection = false;
            return actionMenu;
        }

        /// <summary>
        /// Returns the menu entries for this dialog.
        /// </summary>
        /// <returns>menu entries for this dialog</returns>
        private IList<MenuEntry> SelectionEntries()
        {
            Color color = Color.blue;

            return new List<MenuEntry>
            {
                new(SelectAction: StartHost,
                    Title: "Host",
                    Description: "Starts a server and local client process.",
                    EntryColor: NextColor(),
                    Icon: Icons.Broadcast),

                new(SelectAction: StartClient,
                    Title: "Client",
                    Description: "Starts a local client connecting to a server.",
                    EntryColor: NextColor(),
                    Icon: Icons.Link),

#if ENABLE_VR
                new(SelectAction: ToggleEnvironment,
                    Title: "Toggle Desktop/VR",
                    Description: "Toggles between desktop and VR hardware.",
                    EntryColor: NextColor(),
                    Icon: Icons.VR),
#endif
                new(
                    SelectAction: TelemetrySettings,
                    Title: "Telemetry Mode",
                    Description: $"Currently: {User.UserSettings.Instance ?.Telemetry.Mode}",
                    EntryColor: NextColor(),
                    Icon: Icons.Export),

                new(SelectAction: NetworkSettings,
                    Title: "Settings",
                    Description: "Allows to set additional network and user settings.",
                    EntryColor: Color.gray,
                    Icon: Icons.Gear),
            };

            Color NextColor()
            {
                Color result = color;
                color = color.Lighter();
                return result;
            }
        }

        /// <summary>
        /// Starts a host (= server + local client) on this machine.
        /// </summary>
        private void StartHost()
        {
            try
            {
                // Hide menu while the network is about to be started so that the user
                // user select any menu entry while this process is running. We do
                // not want the user to start any other network setting until this
                // process has come to an end.
                menu.ShowMenu = false;
                User.UserSettings.Instance.InputType = inputType;
                User.UserSettings.Instance.Network.StartHost(NetworkCallBack);
            }
            catch (Exception exception)
            {
                menu.ShowMenu = true;
                ShowNotification.Error("Host cannot be started", exception.Message, log: false);
                throw;
            }
        }

        /// <summary>
        /// Starts a client on this machine.
        /// </summary>
        private void StartClient()
        {
            try
            {
                // Hide menu while the network is about to be started so that the user
                // user select any menu entry while this process is running. We do
                // not want the user to start any other network setting until this
                // process has come to an end.
                menu.ShowMenu = false;
                User.UserSettings.Instance.InputType = inputType;
                User.UserSettings.Instance.Network.StartClient(NetworkCallBack);
            }
            catch (Exception exception)
            {
                menu.ShowMenu = true;
                ShowNotification.Error("Server connection failed", exception.Message, log: false);
                throw;
            }
        }

        /// <summary>
        /// Starts a dedicated server on this machine (only server, no client).
        /// </summary>
        private void StartServer()
        {
            try
            {
                // Hide menu while the network is about to be started so that the user
                // cannot select any menu entry while this process is running. We do
                // not want the user to start any other network setting until this
                // process has come to an end.
                menu.ShowMenu = false;
                User.UserSettings.Instance.InputType = PlayerInputType.None;
                User.UserSettings.Instance.Network.StartServer(NetworkCallBack);
            }
            catch (Exception exception)
            {
                menu.ShowMenu = true;
                ShowNotification.Error("Server cannot be started", exception.Message);
                throw;
            }
        }

        /// <summary>
        /// If <paramref name="success"/>, the <see cref="menu"/> is turned off.
        ///
        /// This method is used as a callback in <see cref="StartClient"/>, <see cref="StartServer"/>,
        /// and <see cref="StartHost"/>.
        /// </summary>
        /// <param name="success">true tells us that the network could be started successfully</param>
        /// <param name="message">a description of what happened</param>
        private void NetworkCallBack(bool success, string message)
        {
            menu.ShowMenu = !success;
            if (!success)
            {
                ShowNotification.Error("Network problem", message);
            }
        }

        /// <summary>
        /// Opens the dialog to configure the network settings.
        /// </summary>
        private void NetworkSettings()
        {
            /// Note: We arrive here because the user pressed one of the buttons of the
            /// menu, which - in turn - will call menu.ShowMenuAsync(false). Thus
            /// at this time, menu is no longer visible. When the following dialog
            /// is finished, <see cref="Reactivate"/> will be called to turn the menu on again.
            UserSettingsDialog dialog = new(User.UserSettings.Instance?.Network, Reactivate);
            dialog.Open();
        }

        /// <summary>
        /// The telemetry settings dialog currently open, or null if no dialog is open.
        /// </summary>
        private TelemetryPropertyDialog telemetryDialog;

        /// <summary>
        /// Opens the telemetry settings dialog and disables the main menu while the dialog is active.
        /// When the dialog is closed, the <see cref="Reactivate"/> method will be called to show the menu again.
        /// </summary>
        private void TelemetrySettings()
        {
            telemetryDialog = new TelemetryPropertyDialog(Reactivate);
            telemetryDialog.Open();
            menu.ShowMenu = false;
        }

        /// <summary>
        /// Turns on the <see cref="menu"/>.
        /// </summary>
        private void Reactivate()
        {
            menu.ShowMenu = true;
        }

        /// <summary>
        /// Sets <see cref="network"/> if it exists as a component attached to
        /// this game object; otherwise this component is disabled.
        /// </summary>
        private void Awake()
        {
            if (User.UserSettings.Instance == null)
            {
                Debug.LogWarning($"No {typeof(User.UserSettings)} component exists in the scene! "
                    + $"{typeof(OpeningDialog)} requires a {typeof(User.UserSettings)} component to be present. "
                    + "Disabling this component.\n");
                enabled = false;
                return;
            }
        }

        /// <summary>
        /// Creates and shows the <see cref="menu"/>.
        /// </summary>
        private void Start()
        {
            menu = CreateMenu();
            User.UserSettings.Instance.Load();
            inputType = User.UserSettings.Instance.InputType;
            // While this OpeningDialog is open, we want to run in a desktop environment,
            // because our GUI implementation is not yet complete for VR. The NetworkPropertyDialog
            // uses widgets that are not implemented for VR. Neither are ShowNotifications not
            // implemented for VR yet.
            // We reset the InputType here to a desktop environment.
            // The loaded settings for the input type is kept in inputType. This field
            // will be toggled by request of the user and only when the host or client is
            // actually started, we assign the value of inputType to User.UserSettings.Instance.InputType.
            User.UserSettings.Instance.InputType = PlayerInputType.DesktopPlayer;
            menu.ShowMenu = true;
            ShowEnvironment();
        }

        /// <summary>
        /// The currently selected player input type.
        /// </summary>
        private PlayerInputType inputType;

        /// <summary>
        /// Toggles <see cref="inputType"/> between <see cref="PlayerInputType.VRPlayer"/>
        /// and <see cref="PlayerInputType.DesktopPlayer"/>. The resulting value is saved.
        /// </summary>
        private void ToggleEnvironment()
        {
            if (inputType == PlayerInputType.DesktopPlayer)
            {
                inputType = PlayerInputType.VRPlayer;
            }
            else
            {
                inputType = PlayerInputType.DesktopPlayer;
            }

            User.UserSettings.Instance.Save();
            ShowEnvironment();
        }

        /// <summary>
        /// Notifies the user via <see cref="ShowNotification.Info(string, string, float, bool)"/>
        /// which player input type was chosen.
        /// </summary>
        private void ShowEnvironment()
        {
            ShowNotification.Info("Environment", Environment());

            string Environment()
            {
                return inputType switch
                {
                    PlayerInputType.DesktopPlayer => "Desktop environment is selected.",
                    PlayerInputType.VRPlayer => "VR environment is selected.",
                    PlayerInputType.TouchGamepadPlayer => "Touch gamepad environment is selected.",
                    PlayerInputType.None => "No environment is selected.",
                    _ => throw new NotImplementedException($"Case {inputType} is not handled")
                };
            }
        }
        /// <summary>
        /// Delegates the Unity frame-based update call to the telemetry dialog,
        /// so that changes in the dropdown selection can be polled and handled.
        /// This ensures that the telemetry mode is updated dynamically even
        /// without event-based support from the underlying UI component.
        /// </summary>
        private void Update()
        {
            telemetryDialog?.Update();
        }
    }
}
