using System.Collections.Generic;
using SEE.Game.UI.Menu;
using SEE.Utils;
using SEE.GO;
using UnityEngine;
using SEE.Game.UI.PropertyDialog;
using System;
using SEE.Game.UI.Notification;
using SEE.Controls;

namespace SEE.UI
{
    /// <summary>
    /// Implements the behaviour of the in-game menu for the selection of the networking
    /// configuration (host, server, client, settings) that is shown at the start up.
    /// </summary>
    internal class OpeningDialog : MonoBehaviour
    {
        /// <summary>
        /// The UI object representing the menu the user chooses the action from.
        /// </summary>
        private SimpleMenu menu;

        /// <summary>
        /// This creates and returns the action menu, with which a user can configure the
        /// networking.
        /// </summary>
        /// <returns>the newly created action menu component.</returns>
        private SimpleMenu CreateMenu()
        {
            GameObject actionMenuGO = new GameObject { name = "Network Menu" };
            IList<ToggleMenuEntry> entries = SelectionEntries();
            SimpleMenu actionMenu = actionMenuGO.AddComponent<SimpleMenu>();
            actionMenu.AllowNoSelection(false); // the menu cannot be closed; user must make a decision
            actionMenu.Title = "Network Configuration";
            actionMenu.Description = "Please select the network configuration you want to activate.";
            actionMenu.AddEntries(entries);
            // We will handle the closing of the menu ourselves: we need to wait until a network
            // connection can be established.
            actionMenu.HideAfterSelection(false);
            return actionMenu;
        }

        /// <summary>
        /// Returns the menu entries for this dialog.
        /// </summary>
        /// <returns>menu entries for this dialog</returns>
        private IList<ToggleMenuEntry> SelectionEntries()
        {
            Color color = Color.blue;

            return new List<ToggleMenuEntry>
                    { new ToggleMenuEntry(active: false,
                                          entryAction: StartHost,
                                          exitAction: null,
                                          title: "Host",
                                          description: "Starts a server and local client process.",
                                          entryColor: NextColor(),
                                          icon: Resources.Load<Sprite>("Icons/Host")),
                      new ToggleMenuEntry(active: false,
                                          entryAction: StartClient,
                                          exitAction: null,
                                          title: "Client",
                                          description: "Starts a local client connection to a server.",
                                          entryColor: NextColor(),
                                          icon: Resources.Load<Sprite>("Icons/Client")),
                      new ToggleMenuEntry(active: false,
                                          entryAction: HostInVR,
                                          exitAction: null,
                                          title: "Host in VR",
                                          description: "Starts a server and local client process and enters in VR.",
                                          entryColor: NextColor(),
                                          icon: Resources.Load<Sprite>("Icons/Host")),
                      new ToggleMenuEntry(active: false,
                                          entryAction: ClientInVR,
                                          exitAction: null,
                                          title: "Client in VR",
                                          description: "Starts a local client connection to a server and enters in VR.",
                                          entryColor: NextColor(),
                                          icon: Resources.Load<Sprite>("Icons/Client")),

                      // FIXME: Running only a server is currently not working.
                      //new ToggleMenuEntry(active: false,
                      //                    entryAction: StartServer,
                      //                    exitAction: null,
                      //                    title: "Server",
                      //                    description: "Starts a dedicated server without local client.",
                      //                    entryColor: NextColor(),
                      //                    icon: Resources.Load<Sprite>("Icons/Server")),
                      new ToggleMenuEntry(active: false,
                                          entryAction: Settings,
                                          exitAction: null,
                                          title: "Settings",
                                          description: "Allows to set additional network settings.",
                                          entryColor: Color.gray,
                                          icon: Resources.Load<Sprite>("Icons/Settings")),
            };

            Color NextColor()
            {
                Color result = color;
                color = color.Lighter();
                return result;
            }
        }

        /// <summary>
        /// The <see cref="Net.Network"/> component configured by this dialog.
        /// </summary>
        private Net.Network network;

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
                menu.ShowMenu(false);
                network.StartHost(NetworkCallBack);
            }
            catch (Exception exception)
            {
                menu.ShowMenu(true);
                ShowNotification.Error("Host cannot be started", exception.Message);
            }
        }

        /// <summary>
        /// FIXME: Remove after ESE.
        /// </summary>
        private void HostInVR()
        {
            try
            {
                // Hide menu while the network is about to be started so that the user
                // user select any menu entry while this process is running. We do
                // not want the user to start any other network setting until this
                // process has come to an end.
                menu.ShowMenu(false);
                SceneSettings.InputType = PlayerInputType.VRPlayer;
                network.StartHost(NetworkCallBack);
            }
            catch (Exception exception)
            {
                menu.ShowMenu(true);
                ShowNotification.Error("Host cannot be started", exception.Message);
            }
        }

        /// <summary>
        /// FIXME: Remove after ESE.
        /// </summary>
        private void ClientInVR()
        {
            try
            {
                // Hide menu while the network is about to be started so that the user
                // user select any menu entry while this process is running. We do
                // not want the user to start any other network setting until this
                // process has come to an end.
                menu.ShowMenu(false);
                SceneSettings.InputType = PlayerInputType.VRPlayer;
                network.StartClient(NetworkCallBack);
            }
            catch (Exception exception)
            {
                menu.ShowMenu(true);
                ShowNotification.Error("Server connection failed", exception.Message);
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
                menu.ShowMenu(false);
                network.StartClient(NetworkCallBack);
            }
            catch (Exception exception)
            {
                menu.ShowMenu(true);
                ShowNotification.Error("Server connection failed", exception.Message);
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
                // user select any menu entry while this process is running. We do
                // not want the user to start any other network setting until this
                // process has come to an end.
                menu.ShowMenu(false);
                network.StartServer(NetworkCallBack);
            }
            catch (Exception exception)
            {
                menu.ShowMenu(true);
                ShowNotification.Error("Server cannot be started", exception.Message);
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
            menu.ShowMenu(!success);
            if (!success)
            {
                ShowNotification.Error("Network problem", message);
            }
        }

        /// <summary>
        /// Opens the dialog to configure the network settings.
        /// </summary>
        private void Settings()
        {
            /// Note: We arrive here because the user pressed one of the buttons of the
            /// menu, which - in turn - will call menu.ShowMenu(false). Thus
            /// at this time, menu is no longer visible. When the following dialog
            /// is finished, <see cref="Reactivate"/> will be called to turn the menu on again.
            NetworkPropertyDialog dialog = new NetworkPropertyDialog(network, Reactivate);
            dialog.Open();
        }

        /// <summary>
        /// Turns on the <see cref="menu"/>.
        /// </summary>
        private void Reactivate()
        {
            menu.ShowMenu(true);
        }

        /// <summary>
        /// Sets <see cref="network"/> if it exists as a component attached to
        /// this game object; otherwise this component is disabled.
        /// </summary>
        private void Awake()
        {
            if (!gameObject.TryGetComponentOrLog(out network))
            {
                enabled = false;
            }
        }

        /// <summary>
        /// Creates and shows the <see cref="menu"/>.
        /// </summary>
        private void Start()
        {
            menu = CreateMenu();
            menu.ShowMenu(true);
        }
    }
}
