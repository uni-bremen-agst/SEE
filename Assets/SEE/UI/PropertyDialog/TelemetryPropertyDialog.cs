using System;
using SEE.Controls;
using SEE.UI.Notification;
using SEE.Utils;
using UnityEngine;
using UnityEngine.UI;
using SEE.Tools.OpenTelemetry;

namespace SEE.UI.PropertyDialog
{
    /// <summary>
    /// Dialog to configure telemetry settings (mode and optional remote URL).
    /// The URL field is shown only when "Remote" is selected.
    /// Visibility is updated once per frame.
    /// </summary>
    internal class TelemetryPropertyDialog
    {
        private GameObject dialog;
        private PropertyDialog propertyDialog;
        private SelectionProperty telemetryModeSelection;
        private StringProperty urlField;
        private readonly Action callback;
        private readonly string defaultRemoteURL = "https://telemetry.see.uni-bremen.de";

        /// <summary>
        /// Remembers last selection to detect changes.
        /// </summary>
        private string lastSelection;

        public TelemetryPropertyDialog(Action callback = null)
        {
            this.callback = callback;
        }

        /// <summary>Call this once per frame from außen, um Änderungen zu erkennen.</summary>
        public void Update()
        {
            string current = telemetryModeSelection.Value;
            if (current != lastSelection)
            {
                lastSelection = current;
                UpdateURLFieldVisibility(current);
            }
        }

        /// <summary>
        /// Opens the telemetry settings dialog and initializes all UI components,
        /// including the telemetry mode dropdown and the optional remote URL field.
        /// The dialog is shown immediately and blocks closing until the user confirms or cancels.
        /// </summary>
        public void Open()
        {
            dialog = new GameObject("Telemetry Settings");

            PropertyGroup group = dialog.AddComponent<PropertyGroup>();
            group.Name = "Telemetry Settings";

            telemetryModeSelection = dialog.AddComponent<SelectionProperty>();
            telemetryModeSelection.Name  = "Telemetry Mode";
            telemetryModeSelection.Description = "Choose how telemetry data should be handled.";
            telemetryModeSelection.AddOptions(new[] { "Disabled", "Local", "Remote" });
            telemetryModeSelection.Value = GetInitialSelectionName();
            group.AddProperty(telemetryModeSelection);

            urlField = dialog.AddComponent<StringProperty>();
            urlField.Name = "URL";
            urlField.Description = "Used when telemetry mode is set to 'Remote'.";
            urlField.Value = User.UserSettings.Instance?.Telemetry.ServerURL ?? defaultRemoteURL;
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

            // nach einem Frame UI gebaut -> Sichtbarkeit setzen
            dialog.AddComponent<DelayedUIAction>().Run(() =>
            {
                lastSelection = telemetryModeSelection.Value;     // initial speichern
                UpdateURLFieldVisibility(lastSelection);
            });
        }

        /// <summary>
        /// Returns the name of the currently selected telemetry mode,
        /// based on the stored scene settings.
        /// Used to initialize the dropdown selection when opening the dialog.
        /// </summary>
        /// <returns>
        /// A string representation of the telemetry mode: "Disabled", "Local", or "Remote".
        /// </returns>
        private string GetInitialSelectionName()
        {
            return User.UserSettings.Instance?.Telemetry.Mode.ToString();
        }

        /// <summary>
        /// Handles the confirmation logic when the user presses the confirm button.
        /// The selected telemetry mode is applied to the scene settings, and in case of
        /// remote telemetry, a valid URL is required. If successful, the dialog is closed
        /// and the callback is invoked.
        /// </summary>
        private void ConfirmPressed()
        {
            if (Enum.TryParse(telemetryModeSelection.Value, out TelemetryMode selectedMode))
            {
                User.UserSettings.Instance.Telemetry.Mode = selectedMode;
            }
            else
            {
                ShowNotification.Error("Invalid Selection", "The selected telemetry mode is not recognized.");
                return;
            }
            if (User.UserSettings.Instance?.Telemetry.Mode == TelemetryMode.Remote)
            {
                if (!string.IsNullOrWhiteSpace(urlField.Value))
                {
                    User.UserSettings.Instance.Telemetry.ServerURL = urlField.Value.Trim();
                }
                else
                {
                    ShowNotification.Error("URL missing", "Please enter a valid URL for remote telemetry.");
                    return;
                }
            }
            User.UserSettings.Instance?.Save();
            Close();
            callback?.Invoke();
            SEEInput.KeyboardShortcutsEnabled = true;
        }

        /// <summary>
        /// Handles cancellation of the dialog. No changes are applied to the settings.
        /// The dialog is closed and the optional callback is invoked.
        /// </summary>
        private void CancelPressed()
        {
            Close();
            callback?.Invoke();
            SEEInput.KeyboardShortcutsEnabled = true;
        }

        /// <summary>
        /// Destroys the root GameObject of the dialog and resets internal references.
        /// </summary>
        private void Close()
        {
            Destroyer.Destroy(dialog);
            dialog = null;
        }

        /// <summary>
        /// Updates the visibility of the URL input field based on the selected telemetry mode.
        /// If the mode is "Remote", the field is shown; otherwise, it is hidden. Additionally,
        /// if the field is shown, the layout is forcefully rebuilt to ensure correct rendering
        /// of the updated UI elements.
        /// </summary>
        /// <param name="selectedMode">The currently selected telemetry mode (e.g., "Disabled", "Local", or "Remote").</param>
        private void UpdateURLFieldVisibility(string selectedMode)
        {
            if (urlField.InputFieldObject == null)
            {
                return;
            }

            if (Enum.TryParse(selectedMode, out TelemetryMode mode))
            {
                bool showURL = mode == TelemetryMode.Remote;

                urlField.InputFieldObject.SetActive(showURL);

                if (showURL)
                {
                    RectTransform rect = urlField.InputFieldObject.GetComponent<RectTransform>();
                    if (rect != null)
                    {
                        LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
                    }
                }
            }
            else
            {
                ShowNotification.Error("Invalid Selection", "The selected telemetry mode is not recognized.");
                return;
            }
        }
    }
}
