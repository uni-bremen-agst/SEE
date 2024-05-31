using SEE.Controls;
using SEE.UI.Notification;
using System;
using SEE.Utils;
using UnityEngine;
using UnityEngine.Events;

namespace SEE.UI.PropertyDialog
{
    internal class UserPropertyDialog
    {
        /// <summary>
        /// Callback to be called when this dialog closes.
        /// </summary>
        public delegate void OnClosed();

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="networkConfig">the network configuration to be manipulated by this dialog</param>
        /// <param name="callBack">delegate to be called when this dialog is closed</param>
        public UserPropertyDialog(Net.Network networkConfig, OnClosed callBack = null)
        {
            this.networkConfig = networkConfig;
            this.callBack = callBack;
        }

        /// <summary>
        /// Delegate to be called when this dialog is closed. May be null in which case
        /// nothing is called.
        /// </summary>
        private OnClosed callBack;

        /// <summary>
        /// The dialog used to manipulate the node.
        /// </summary>
        private GameObject dialog;

        /// <summary>
        /// The dialog property for the playername.
        /// </summary>
        private StringProperty playerName;

        /// <summary>
        /// The dialog for the user input.
        /// </summary>
        private PropertyDialog propertyDialog;

        /// <summary>
        /// Event triggered when the user presses the OK button. Clients can
        /// register on this event to receive a notification when this happens.
        /// </summary>
        public readonly UnityEvent OnConfirm = new();

        /// <summary>
        /// Event triggered when the user presses the Cancel button. Clients can
        /// register on this event to receive a notification when this happens.
        /// </summary>
        public readonly UnityEvent OnCancel = new();



        /// <summary>
        /// The network configuration to be manipulated by this dialog.
        /// </summary>
        private Net.Network networkConfig;

        /// <summary>
        /// Creates and opens the dialog.
        /// </summary>
        public void Open()
        {
            networkConfig.Load();

            dialog = new GameObject("User settings");
            PropertyGroup group = dialog.AddComponent<PropertyGroup>();
            group.Name = "User settings";
            {
                playerName = dialog.AddComponent<StringProperty>();
                playerName.Name = "Username";
                playerName.Value = networkConfig.Playername;
                playerName.Description = "Username which will be used as avatar-tag and in other function.";
                group.AddProperty(playerName);
            }
            // Dialog
            propertyDialog = dialog.AddComponent<PropertyDialog>();
            propertyDialog.Title = "User settings";
            propertyDialog.Description = "Enter the user settings";
            propertyDialog.AddGroup(group);

            // Register listeners
            propertyDialog.OnConfirm.AddListener(OKButtonPressed);
            propertyDialog.OnCancel.AddListener(CancelButtonPressed);

            SEEInput.KeyboardShortcutsEnabled = false;
            // Go online
            propertyDialog.DialogShouldBeShown = true;
        }

        /// <summary>
        /// Notifies all listeners on <see cref="OnCancel"/> and closes the dialog.
        /// </summary>
        private void CancelButtonPressed()
        {
            propertyDialog.Close();
            OnCancel.Invoke();
            SEEInput.KeyboardShortcutsEnabled = true;
            Close();
        }

        /// <summary>
        /// Notifies all listeners on <see cref="OnConfirm"/> and save name and closes the dialog.
        /// </summary>
        private void OKButtonPressed()
        {
            bool errorOccurred = false;
            {
                string playername = playerName.Value.Trim();

                if (ValidUsername(playername))
                {
                    networkConfig.Playername = playername;
                }
                else
                {
                    ShowNotification.Error
                       ("Username Error",
                        "Username needs to be minimum one character long.");
                    errorOccurred = true;
                }
            }
            if (!errorOccurred)
            {
                propertyDialog.Close();
                OnConfirm.Invoke();
                SEEInput.KeyboardShortcutsEnabled = true;
                Close();
                callBack?.Invoke();
            }
        }

        /// <summary>
        /// Checks if playername is at least one character long. Other restrictions could be added in this method too.
        /// </summary>
        /// <param name="playername">the playername to validate</param>
        /// <returns>true if <paramref name="playername"/> conforms the restrictions</returns>
        private static bool ValidUsername(string playername)
        {
            if (String.IsNullOrWhiteSpace(playername))
            {
                return false;
            }
            return true;
        }


        /// <summary>
        /// Destroys <see cref="dialog"/>. <see cref="dialog"/> will be null afterwards.
        /// </summary>
        private void Close()
        {
            Destroyer.Destroy(dialog);
            dialog = null;
        }
    }
}