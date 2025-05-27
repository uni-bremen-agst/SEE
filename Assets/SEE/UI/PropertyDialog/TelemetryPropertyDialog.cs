using System;
using SEE.Controls;
using SEE.UI.Notification;
using SEE.UI.PropertyDialog;
using SEE.Utils;
using UnityEngine;

namespace SEE.UI.PropertyDialog
{
    /// <summary>
    /// Dialog to configure telemetry settings (mode and optional remote URL).
    /// </summary>
    internal class TelemetryPropertyDialog
    {
        /// <summary>
        /// The root GameObject of this dialog.
        /// </summary>
        private GameObject dialog;

        /// <summary>
        /// The property dialog component.
        /// </summary>
        private PropertyDialog propertyDialog;

        /// <summary>
        /// Dropdown for selecting telemetry mode.
        /// </summary>
        private SelectionProperty telemetryModeSelection;

        /// <summary>
        /// Text input field for specifying the remote telemetry URL.
        /// </summary>
        private StringProperty urlField;

        /// <summary>
        /// The callback invoked after confirmation or cancellation.
        /// </summary>
        private readonly Action callback;

        /// <summary>
        /// Default URL used if none is provided for remote telemetry.
        /// </summary>
        private readonly string defaultRemoteURL = "https://telemetry.see.uni-bremen.de";

        /// <summary>
        /// Initializes a new instance of the <see cref="TelemetryPropertyDialog"/> class.
        /// </summary>
        /// <param name="callback">Optional callback to invoke on close.</param>
        public TelemetryPropertyDialog(Action callback = null)
        {
            this.callback = callback;
        }

        /// <summary>
        /// Opens the dialog and initializes all components.
        /// </summary>
        public void Open()
        {
            dialog = new GameObject("Telemetry Settings");

            PropertyGroup group = dialog.AddComponent<PropertyGroup>();
            group.Name = "Telemetry Settings";

            telemetryModeSelection = dialog.AddComponent<SelectionProperty>();
            telemetryModeSelection.Name = "Telemetry Mode";
            telemetryModeSelection.Description = "Choose how telemetry data should be handled.";
            telemetryModeSelection.AddOptions(new[] { "Disabled", "Local", "Remote" });
            telemetryModeSelection.Value = GetInitialSelectionName();
            group.AddProperty(telemetryModeSelection);

            urlField = dialog.AddComponent<StringProperty>();
            urlField.Name = "URL";
            urlField.Description = "Used when telemetry mode is set to 'Remote'.";
            urlField.Value = SceneSettings.CustomTelemetryServerURL ?? defaultRemoteURL;
            group.AddProperty(urlField);

            propertyDialog = dialog.AddComponent<PropertyDialog>();
            propertyDialog.Title = "Telemetry Settings";
            propertyDialog.Description = "Configure telemetry mode and optional remote endpoint.";
            propertyDialog.AddGroup(group);
            propertyDialog.AllowClosing(false);

            propertyDialog.OnConfirm.AddListener(ConfirmPressed);
            propertyDialog.OnCancel.AddListener(CancelPressed);

            SEEInput.KeyboardShortcutsEnabled = false;
            propertyDialog.DialogShouldBeShown = true;
        }

        /// <summary>
        /// Returns the name of the initially selected telemetry mode.
        /// </summary>
        /// <returns>One of "Disabled", "Local", or "Remote".</returns>
        private string GetInitialSelectionName()
        {
            return SceneSettings.telemetryMode switch
            {
                TelemetryMode.Disabled => "Disabled",
                TelemetryMode.Local => "Local",
                TelemetryMode.Remote => "Remote",
                _ => "Disabled"
            };
        }

        /// <summary>
        /// Called when the confirm button is pressed.
        /// Applies settings and invokes callback.
        /// </summary>
        private void ConfirmPressed()
        {
            string selected = telemetryModeSelection.Value;

            switch (selected)
            {
                case "Disabled":
                    SceneSettings.telemetryMode = TelemetryMode.Disabled;
                    break;

                case "Local":
                    SceneSettings.telemetryMode = TelemetryMode.Local;
                    break;

                case "Remote":
                    if (!string.IsNullOrWhiteSpace(urlField.Value))
                    {
                        SceneSettings.telemetryMode = TelemetryMode.Remote;
                        SceneSettings.CustomTelemetryServerURL = urlField.Value.Trim();
                    }
                    else
                    {
                        ShowNotification.Error("URL missing", "Please enter a valid URL for remote telemetry.");
                        return;
                    }

                    break;
            }

            SceneSettings.SaveTelemetrySettings();
            Close();
            callback?.Invoke();
            SEEInput.KeyboardShortcutsEnabled = true;
        }

        /// <summary>
        /// Called when the cancel button is pressed.
        /// Closes dialog and invokes callback.
        /// </summary>
        private void CancelPressed()
        {
            Close();
            callback?.Invoke();
            SEEInput.KeyboardShortcutsEnabled = true;
        }

        /// <summary>
        /// Closes and destroys the dialog GameObject.
        /// </summary>
        private void Close()
        {
            Destroyer.Destroy(dialog);
            dialog = null;
        }
    }
}
