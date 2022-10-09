using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Michsky.UI.ModernUIPack;
using SEE.Utils;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Game.UI.Notification
{
    public class SEENotificationManager: MonoBehaviour
    {
        /// <summary>
        /// Amount of padding between each notification.
        /// </summary>
        private const float PADDING = 10f;

        /// <summary>
        /// Notifications managed by this component along with their respective height.
        /// </summary>
        private readonly List<(Notification notification, float height)> Notifications = new List<(Notification notification, float height)>();

        /// <summary>
        /// Creates and immediately displays a notification using the given parameters.
        /// </summary>
        /// <param name="title">The title of the notification.</param>
        /// <param name="description">The description of the notification.</param>
        /// <param name="icon">The icon of the notification.</param>
        /// <param name="color">The color of the notification.</param>
        /// <param name="duration">The duration of the notification.</param>
        /// <returns>The created notification. Will be <c>null</c> as soon it's done playing.</returns>
        public Notification Show(string title, string description, Sprite icon, Color color, float duration)
        {
            GameObject notificationGameObject = new GameObject {name = $"Notification '{title}'"};
            Notification notification = notificationGameObject.AddComponent<Notification>();
            notification.Title = title;
            notification.Text = description;
            notification.Timer = duration;
            notification.Icon = icon;
            notification.Color = color;
            notification.DestroyAfterPlaying = true;
            notification.OnFinished = () => HandleClosedNotification(notification);
            HandleNewNotification(notification).Forget();
            return notification;
        }

        /// <summary>
        /// Handles newly created notifications by moving other notifications down.
        /// </summary>
        /// <param name="notification">The newly created notification</param>
        private async UniTaskVoid HandleNewNotification(Notification notification)
        {
            // Notification will be initialized next frame.
            float finalHeight = 0;
            await UniTask.WaitUntil(() =>
            {
                float? height = notification.GetHeight();
                // We've reached a final height if it's non-null (implicit), non-zero, and stable (doesn't change).
                bool finalHeightReached = height > 0 && height == finalHeight;
                finalHeight = height ?? 0;
                return finalHeightReached;
            });
            
            // We iterate through the list in reverse so we can remove elements without complications.
            for (int i = Notifications.Count - 1; i >= 0; i--)
            {
                if (Notifications[i].notification == null)
                {
                    Notifications.RemoveAt(i);
                    continue;
                }
                // New notification is at the top. We move all others down by the height of this one.
                Notifications[i].notification.MoveDown(finalHeight + PADDING);
            }
            Notifications.Add((notification, finalHeight + PADDING));
        }

        /// <summary>
        /// Handles closed notifications by moving below notifications up.
        /// </summary>
        /// <param name="notification">The closed notification</param>
        private void HandleClosedNotification(Notification notification)
        {
            bool belowRemovedNotification = false;
            float height = 0;
            foreach ((Notification notification, float height) entry in Notifications.AsQueryable().Reverse())
            {
                // All notifications below this one need to be moved up by the height of the closed notification.
                // We do nothing until we find our notification, as the ones above do not need to be adjusted.
                if (!belowRemovedNotification)
                {
                    if (entry.notification == notification)
                    {
                        belowRemovedNotification = true;
                        height = entry.height;
                    }
                }
                else
                {
                    // Moving up by inverting the sign
                    entry.notification.MoveDown(-height);
                }
            }
        }
    }
}