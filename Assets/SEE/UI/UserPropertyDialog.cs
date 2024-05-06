using SEE.Controls;
using SEE.UI.Notification;
using SEE.GO;
using System;
using System.Collections.Generic;
using System.Linq;
using SEE.Utils;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using SEE.UI.PropertyDialog;

namespace SEE.UI.PropertyDialog
{
    internal class UserPropertyDialog
    {
        /// <summary>
        /// The dialog used to manipulate the node.
        /// </summary>
        private GameObject dialog;

        /// <summary>
        /// The dialog property for the username.
        /// </summary>
        private StringProperty userName;

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
        /// Creates and opens the dialog.
        /// </summary>
        public void Open()
        {
            dialog = new GameObject("User settings");
            PropertyGroup group = dialog.AddComponent<PropertyGroup>();
            group.Name = "User settings";
            {
                userName = dialog.AddComponent<StringProperty>();
                userName.Name = "Username";
                userName.Value = PlayerNameReader.ReadPlayerName();
                userName.Description = "Username which will be used for the avatar-tag and in chat function.";
                group.AddProperty(userName);
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
            {
                string playername = userName.Value.Trim();
                PlayerNameReader.CreatePlayerJson(playername);
            }
            propertyDialog.Close();
            OnConfirm.Invoke();
            SEEInput.KeyboardShortcutsEnabled = true;
            Close();
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