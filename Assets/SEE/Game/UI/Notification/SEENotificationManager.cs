using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace SEE.Game.UI.Notification
{
    /// <summary>
    /// Displays notifications in a vertical list.
    ///
    /// It behaves as follows:
    /// <ul>
    /// <li>New notifications are displayed at the top right. Any existing notifications will be pushed down,
    /// along with a configurable <see cref="MARGIN"/>.</li>
    /// <li>Notifications can be closed by clicking on them,
    /// which will cause below notifications to "slide up" and fill the empty space.</li>
    /// <li>Movement in both directions is done via the <see cref="NotificationOperator"/>,
    /// so that animations do not interfere with one another.</li>
    /// <li>Notifications have a timer associated to them, that is, they close themselves after a configurable
    /// amount of time. This timer is only applied to the topmost notification, which means that no notification
    /// below will close automatically (except when reaching the top spot).
    /// If the notification slides below the top spot, its timer will be cancelled until it again reaches the top spot.
    /// The reason behind this is that the user should not be overwhelmed by auto-closing notifications and should have
    /// time to read everything, especially considering that some notifications may be below the screen.</li>
    /// </ul>
    /// </summary>
    public class SEENotificationManager : MonoBehaviour
    {
        /// <summary>
        /// Size of vertical margin between each notification.
        /// </summary>
        private const float MARGIN = 10f;

        /// <summary>
        /// Notifications managed by this component along with their respective height.
        /// </summary>
        private readonly List<NotificationData> Notifications = new List<NotificationData>();

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
            GameObject notificationGameObject = new GameObject { name = $"Notification '{title}'" };
            Notification notification = notificationGameObject.AddComponent<Notification>();
            notification.Title = title;
            notification.Text = description;
            notification.Timer = duration;
            notification.TimerEnabled = false; // We handle this ourselves.
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
                if (Notifications[i].Notification == null)
                {
                    Notifications.RemoveAt(i);
                    continue;
                }

                // New notification is at the top. We move all others down by the height of this one.
                Notifications[i].Notification.MoveDown(finalHeight + MARGIN);
                // This also cancels their timer. Only the notification at the very top will be timed.
                Notifications[i].Token.Cancel();
            }

            CancellationTokenSource token = new CancellationTokenSource();
            Notifications.Add(new NotificationData(notification, finalHeight + MARGIN, token));

            StartTimer(notification, token.Token).Forget();
        }

        /// <summary>
        /// Starts the timer for the given <paramref name="notification"/>, attaching the given
        /// cancellation <paramref name="token"/>.
        /// After the timer has reached zero <em>and</em> the notification is at the top of the notification list,
        /// the notification will be closed. Otherwise, nothing will be done.
        /// </summary>
        /// <param name="notification">notification whose timer shall be started</param>
        /// <param name="token">cancellation token with which the timer can be cancelled</param>
        private async UniTaskVoid StartTimer(Notification notification, CancellationToken token)
        {
            if (notification.Timer <= 0f)
            {
                // Timer disabled.
                return;
            }

            await UniTask.Delay(TimeSpan.FromSeconds(notification.Timer), cancellationToken: token);
            // If the notification is still active AND at the top of the list, we'll close it
            if (Notifications.Count > 0 && Notifications.Last().Notification == notification)
            {
                notification.Close();
            }
        }

        /// <summary>
        /// Handles closed notifications by moving below notifications up.
        /// </summary>
        /// <param name="notification">The closed notification</param>
        private void HandleClosedNotification(Notification notification)
        {
            bool belowRemovedNotification = false;
            float height = 0;
            int index = Notifications.Count - 1;
            foreach (NotificationData entry in Notifications.AsQueryable().Reverse())
            {
                // All notifications below this one need to be moved up by the height of the closed notification.
                // We do nothing until we find our notification, as the ones above do not need to be adjusted.
                if (!belowRemovedNotification)
                {
                    if (entry.Notification == notification)
                    {
                        belowRemovedNotification = true;
                        height = entry.Height;
                    }
                    else
                    {
                        index--;
                    }
                }
                else
                {
                    // Moving up by inverting the sign
                    entry.Notification.MoveDown(-height);
                }
            }

            if (index <= 0)
            {
                // We never found the notification, so it was already handled. Nothing left to do.
                return;
            }

            if (index == Notifications.Count - 1 && Notifications.Count > 1)
            {
                // Top spot has changed. We start the timer for the new top notification.
                CancellationTokenSource token = Notifications[index - 1].Token = new CancellationTokenSource();
                StartTimer(Notifications[index - 1].Notification, token.Token).Forget();
            }

            // We also need to dispose the token.
            Notifications[index].Token.Cancel();
            Notifications[index].Token.Dispose();
            Notifications.RemoveAt(index);
        }

        /// <summary>
        /// Encapsulates a notification along with its height and a <see cref="CancellationToken"/> for its timer.
        /// </summary>
        private class NotificationData
        {
            /// <summary>
            /// The notification represented by this class.
            /// </summary>
            public readonly Notification Notification;
            
            /// <summary>
            /// The height of this notification on the canvas.
            /// </summary>
            public readonly float Height;
            
            /// <summary>
            /// The token with which the notification's timer can be cancelled.
            /// </summary>
            public CancellationTokenSource Token;

            public NotificationData(Notification notification, float height, CancellationTokenSource token)
            {
                Notification = notification;
                Height = height;
                Token = token;
            }
        }
    }
}