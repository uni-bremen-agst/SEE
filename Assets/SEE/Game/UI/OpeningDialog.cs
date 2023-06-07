﻿using System.Collections.Generic;
using SEE.Game.UI.Menu;
using SEE.Utils;
using SEE.GO;
using UnityEngine;
using SEE.Game.UI.PropertyDialog;
using System;
using SEE.Game.UI.Notification;
using SEE.Controls;
using Sirenix.Utilities;

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
        private SimpleListMenu menu;

        /// <summary>
        /// This creates and returns the action menu, with which a user can configure the
        /// networking.
        /// </summary>
        /// <returns>the newly created action menu component.</returns>
        private SimpleListMenu CreateMenu()
        {
            GameObject actionMenuGO = new() { name = "Network Menu" };
            IList<MenuEntry> entries = SelectionEntries();
            SimpleListMenu actionMenu = actionMenuGO.AddComponent<SimpleListMenu>();
            actionMenu.AllowNoSelection = false; // the menu cannot be closed; user must make a decision
            actionMenu.Title = "Network Configuration";
            actionMenu.Description = "Please select the network configuration you want to activate.";
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
                      new(selectAction: this.StartHost,
                          unselectAction: null,
                          title: "Host",
                          description: "Starts a server and local client process.",
                          entryColor: NextColor(),
                          icon: Resources.Load<Sprite>("Icons/Host")),
                      new(selectAction: this.StartClient,
                          unselectAction: null,
                          title: "Client",
                          description: "Starts a local client connection to a server.",
                          entryColor: NextColor(),
                          icon: Resources.Load<Sprite>("Icons/Client")),
#if ENABLE_VR
                      new(selectAction: this.ToggleEnvironment,
                          unselectAction: null,
                          title: "Toggle Desktop/VR",
                          description: "Toggles between desktop and VR hardware.",
                          entryColor: NextColor(),
                          icon: Resources.Load<Sprite>("Icons/Client")),
#endif
                      // FIXME: Running only a server is currently not working.
                      //new (               entryAction: StartServer,
                      //                    exitAction: null,
                      //                    title: "Server",
                      //                    description: "Starts a dedicated server without local client.",
                      //                    entryColor: NextColor(),
                      //                    icon: Resources.Load<Sprite>("Icons/Server")),
                      new(selectAction: this.Settings,
                          unselectAction: null,
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
                menu.ShowMenu = false;
                SceneSettings.InputType = inputType;
                network.StartHost(NetworkCallBack);
            }
            catch (Exception exception)
            {
                menu.ShowMenu = true;
                ShowNotification.Error("Host cannot be started", exception.Message);
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
                SceneSettings.InputType = inputType;
                network.StartClient(NetworkCallBack);
            }
            catch (Exception exception)
            {
                menu.ShowMenu = true;
                ShowNotification.Error("Server connection failed", exception.Message);
            }
        }

        /// <summary>
        /// Starts a dedicated server on this machine (only server, no client).
        /// </summary>
        //private void StartServer()
        //{
        //    try
        //    {
        //        // Hide menu while the network is about to be started so that the user
        //        // user select any menu entry while this process is running. We do
        //        // not want the user to start any other network setting until this
        //        // process has come to an end.
        //        menu.ShowMenu(false);
        //        SceneSettings.InputType = inputType;
        //        network.StartServer(NetworkCallBack);
        //    }
        //    catch (Exception exception)
        //    {
        //        menu.ShowMenu(true);
        //        ShowNotification.Error("Server cannot be started", exception.Message);
        //    }
        //}

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
        private void Settings()
        {
            /// Note: We arrive here because the user pressed one of the buttons of the
            /// menu, which - in turn - will call menu.ShowMenu(false). Thus
            /// at this time, menu is no longer visible. When the following dialog
            /// is finished, <see cref="Reactivate"/> will be called to turn the menu on again.
            NetworkPropertyDialog dialog = new(network, Reactivate);
            dialog.Open();
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
            SceneSettings.Load();
            inputType = SceneSettings.InputType;
            // While this OpeningDialog is open, we want to run in a desktop environment,
            // because our GUI implementation is not yet complete for VR. The NetworkPropertyDialog
            // uses widgets that are not implemented for VR. Neither are ShowNotifications not
            // implemented for VR yet.
            // We reset SceneSettings.InputType here to a desktop environment.
            // The loaded settings for the input type is kept in inputType. This field
            // will be toggled by request of the user and only when the host or client is
            // actually started, we assign the value of inputType to SceneSettings.InputType.
            SceneSettings.InputType = PlayerInputType.DesktopPlayer;
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

            SceneSettings.Save();
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
    }
}
