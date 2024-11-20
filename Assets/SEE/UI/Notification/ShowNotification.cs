using System;
using SEE.Utils;
using UnityEngine;

namespace SEE.UI.Notification
{
    /// <summary>
    /// Static helper class which can create notifications in similar variants as Unity's logging, for example
    /// <see cref="ShowError"/> or <see cref="ShowWarning"/>.
    /// The <see cref="Notification"/> class will be used for this.
    /// </summary>
    public static class ShowNotification
    {
        /// <summary>
        /// Default amount of time a notification is shown (in seconds), if not specified any further.
        /// </summary>
        private const float defaultDuration = 10f;

        /// <summary>
        /// Background color for error messages.
        /// </summary>
        private static readonly Color errorColor = new(0x55 / 255f, 0x2D / 255f, 0x2E / 255f);

        /// <summary>
        /// Background color for warning messages.
        /// </summary>
        private static readonly Color warningColor = new(0x55 / 255f, 0x4E / 255f, 0x2D / 255f);

        /// <summary>
        /// Background color for info messages.
        /// </summary>
        private static readonly Color infoColor = new(0x2D / 255f, 0x40 / 255f, 0x55 / 255f);

        /// <summary>
        /// Sprite for the error icon.
        /// </summary>
        private static readonly Lazy<Sprite> errorIcon = new(() => Resources.Load<Sprite>("Materials/Notification/Error"));

        /// <summary>
        /// Sprite for the warning icon.
        /// </summary>
        private static readonly Lazy<Sprite> warningIcon = new(() => Resources.Load<Sprite>("Materials/Notification/Warning"));

        /// <summary>
        /// Sprite for the info icon.
        /// </summary>
        private static readonly Lazy<Sprite> infoIcon = new(() => Resources.Load<Sprite>("Materials/Notification/Info"));

        /// <summary>
        /// Lazily initialized notification manager instance. Behaves like a singleton.
        /// </summary>
        private static readonly Lazy<SEENotificationManager> manager = new(CreateManager);

        /// <summary>
        /// Creates a new <see cref="SEENotificationManager"/> along with a corresponding new <see cref="GameObject"/>.
        /// </summary>
        /// <returns>the newly created <see cref="SEENotificationManager"/></returns>
        private static SEENotificationManager CreateManager()
        {
            // All other notifications will be children to this manager object.
            GameObject managerGameObject = new()
            {
                name = "Notifications"
            };
            return managerGameObject.AddComponent<SEENotificationManager>();
        }

        /// <summary>
        /// Displays an informational message to the user as a notification.
        /// </summary>
        /// <param name="title">Title of the notification.</param>
        /// <param name="description">Description of the notification.</param>
        /// <param name="duration">Time in seconds the notification should stay on the screen.</param>
        /// <param name="log">Whether to log the given notification in Unity's log as well</param>
        /// <returns>The notification object representing the newly created notification.</returns>
        public static Notification Info(string title, string description, float duration = defaultDuration,
                                        bool log = true)
        {
            if (log)
            {
                Debug.Log($"{title}: {description}\n");
            }
            return Show(title, description, infoIcon, infoColor, duration);
        }

        /// <summary>
        /// Displays a warning message to the user as a notification.
        /// </summary>
        /// <param name="title">Title of the notification.</param>
        /// <param name="description">Description of the notification.</param>
        /// <param name="duration">Time in seconds the notification should stay on the screen.</param>
        /// <param name="log">Whether to log the given notification in Unity's log as well</param>
        /// <returns>The notification object representing the newly created notification.</returns>
        public static Notification Warn(string title, string description, float duration = defaultDuration,
                                bool log = true)
        {
            if (log)
            {
                Debug.LogWarning($"{title}: {description}\n");
            }
            return Show(title, description, warningIcon, warningColor, duration);
        }

        /// <summary>
        /// Displays an error message to the user as a notification.
        /// </summary>
        /// <param name="title">Title of the notification.</param>
        /// <param name="description">Description of the notification.</param>
        /// <param name="duration">Time in seconds the notification should stay on the screen.</param>
        /// <param name="log">Whether to log the given notification in Unity's log as well</param>
        /// <returns>The notification object representing the newly created notification.</returns>
        public static Notification Error(string title, string description, float duration = defaultDuration,
                                 bool log = true)
        {
            if (log)
            {
                Debug.LogError($"{title}: {description}\n");
            }
            return Show(title, description, errorIcon, errorColor, duration);
        }

        /// <summary>
        /// Creates and immediately displays a notification using the given parameters.
        /// </summary>
        /// <param name="title">The title of the notification.</param>
        /// <param name="description">The description of the notification.</param>
        /// <param name="icon">The icon of the notification.</param>
        /// <param name="color">The color of the notification.</param>
        /// <param name="duration">The duration of the notification.</param>
        /// <returns>The notification object representing the newly created notification.</returns>
        private static Notification Show(string title, string description, Lazy<Sprite> icon, Color color,
                                         float duration = defaultDuration)
        {
            // Only show the notification if we are on the main thread.
            if (AsyncUtils.IsRunningOnMainThread)
            {
                return manager.Value.Show(title, description, icon.Value, color, duration);
            }
            return null;
        }
    }
}
