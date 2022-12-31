using System;
using System.Linq;
using Michsky.UI.ModernUIPack;
using SEE.Game.Operator;
using SEE.GO;
using SEE.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SEE.Game.UI.Notification
{
    /// <summary>
    /// Represents a notification with text and an icon that can be displayed to the user.
    /// The notification will be displayed immediately once it's Start() method is called,
    /// and will be destroyed once it's been closed.
    /// It can be closed after a certain time interval, and additionally by clicking on it.
    /// By default, the notification will be displayed in the upper right corner, but this can be changed
    /// using the <see cref="AnchorMax"/>, <see cref="AnchorMin"/> and <see cref="Pivot"/> fields.
    /// </summary>
    public class Notification : PlatformDependentComponent
    {
        /// <summary>
        /// The title displayed in the notification.
        /// Setting this won't have an effect after Start() has been called.
        /// </summary>
        public string Title = "Notification";

        /// <summary>
        /// The description displayed in the notification.
        /// Setting this won't have an effect after Start() has been called.
        /// </summary>
        public string Text = "";

        /// <summary>
        /// The icon displayed in the notification.
        /// Setting this won't have an effect after Start() has been called.
        /// </summary>
        public Sprite Icon;

        /// <summary>
        /// Color of the notification's background.
        /// Whether the text shall be black or white will be automatically determined based on readability.
        /// </summary>
        public Color Color = Color.gray;

        /// <summary>
        /// The time in seconds how long the notification should be shown after enabling it.
        /// If set to a negative value, the notification won't disappear.
        /// Setting this won't have an effect after Start() has been called.
        /// </summary>
        public float Timer = -1f;

        /// <summary>
        /// Whether the notification shall disappear after <see cref="Timer"/> seconds.
        /// </summary>
        public bool TimerEnabled = true;

        /// <summary>
        /// The normalized position in the canvas that the upper right corner is anchored to.
        /// Changes will only have an effect before Start() is called.
        /// </summary>
        public Vector2 AnchorMin = Vector2.one;

        /// <summary>
        /// The normalized position in the canvas that the lower left corner is anchored to.
        /// Changes will only have an effect before Start() is called.
        /// </summary>
        public Vector2 AnchorMax = Vector2.one;

        /// <summary>
        /// The normalized position in this Canvas that it rotates around.
        /// </summary>
        public Vector2 Pivot = Vector2.one;

        /// <summary>
        /// Modern UI Pack component managing the notification.
        /// </summary>
        private NotificationManager manager;

        /// <summary>
        /// Whether to destroy the notification's game object as well as
        /// <b>the game object this component is attached to (!)</b> after the notification is done playing.
        /// </summary>
        public bool DestroyAfterPlaying = false;

        /// <summary>
        /// Path to the notification prefab.
        /// </summary>
        private const string NOTIFICATION_PREFAB = "Prefabs/UI/Notification";

        /// <summary>
        /// Time it takes the notification to move along the y axis.
        /// </summary>
        private const float ANIMATION_DURATION = 1f;

        /// <summary>
        /// The number of frames this notification has been active.
        /// </summary>
        private int frames;

        /// <summary>
        /// The target y coordinate for the notification. May be null.
        /// </summary>
        private float? targetY;

        /// <summary>
        /// The game object on the <see cref="Canvas"/> containing the actual notification UI components.
        /// </summary>
        private GameObject ManagedNotification
        {
            get;
            set;
        }

        /// <summary>
        /// The operator for the <see cref="ManagedNotification"/>.
        /// </summary>
        private NotificationOperator Operator;

        /// <summary>
        /// Called when the notification is finished.
        /// NOTE: May be called more than once.
        /// </summary>
        public Action OnFinished;

        /// <summary>
        /// Closes the notification immediately.
        /// </summary>
        public void Close()
        {
            if (manager != null)
            {
                OnFinished?.Invoke();
                manager.CloseNotification();
            }
        }

        private void OnDestroy()
        {
            if (manager != null)
            {
                Destroy(manager.gameObject);
            }
        }

        /// <summary>
        /// Returns the height of the <see cref="ManagedNotification"/>, or null if it does not exist yet.
        /// </summary>
        public float? GetHeight()
        {
            if (ManagedNotification == null)
            {
                return null;
            }
            else
            {
                return ((RectTransform)ManagedNotification.transform).sizeDelta.y;
            }
        }

        /// <summary>
        /// Moves this notification down by the given amount <paramref name="y"/>, 
        /// taking <see cref="ANIMATION_DURATION"/> seconds.
        /// If the notification has not been created yet (i.e., Start() has not been called), the y position will
        /// be set immediately upon creation.
        /// </summary>
        /// <param name="y">target y down movement</param>
        /// <returns>a callback for the started animation, or null if no animation has been started</returns>
        public IOperationCallback<Action> MoveDown(float y)
        {
            if (Operator == null)
            {
                targetY = y;
                return null;
            }
            else
            {
                return Operator.MoveToY(Operator.TargetPositionY - y, ANIMATION_DURATION);
            }
        }

        /// <summary>
        /// Creates the notification for the desktop platform.
        /// </summary>
        protected override void StartDesktop()
        {
            ManagedNotification = PrefabInstantiator.InstantiatePrefab(NOTIFICATION_PREFAB, Canvas.transform, false);
            ManagedNotification.MustGetComponent(out Operator);

            // Setup anchoring
            RectTransform rectTransform = (RectTransform)ManagedNotification.transform;
            rectTransform.pivot = Pivot;
            rectTransform.anchorMax = AnchorMax;
            rectTransform.anchorMin = AnchorMin;
            if (targetY != null)
            {
                rectTransform.position = rectTransform.position.WithXYZ(y: targetY);
            }

            // Setup colors
            if (ManagedNotification.transform.Find("Background").gameObject.TryGetComponentOrLog(out Image image))
            {
                image.color = Color;
                // Set ideal color (black/white) for the text itself
                if (ManagedNotification.transform.Find("Title").TryGetComponent(out TextMeshProUGUI titleText) &&
                    ManagedNotification.transform.Find("Description").TryGetComponent(out TextMeshProUGUI descText) &&
                    ManagedNotification.transform.Find("Icon").TryGetComponent(out Image iconImage))
                {
                    Color idealColor = Color.IdealTextColor();
                    titleText.color = idealColor;
                    descText.color = idealColor;
                    iconImage.color = idealColor;
                }
            }

            // Setup notification
            if (ManagedNotification.TryGetComponentOrLog(out manager))
            {
                manager.title = Title;
                manager.description = Text;
                manager.icon = Icon;
                manager.timer = Timer;
                manager.enableTimer = Timer >= 1f && TimerEnabled;
                manager.destroyAfterPlaying = DestroyAfterPlaying;
                manager.OpenNotification();

                // Close the notification when clicking on it
                if (ManagedNotification.TryGetComponentOrLog(out EventTrigger trigger))
                {
                    trigger.triggers.Single().callback.AddListener(_ => manager.CloseNotification());
                }
            }
        }

        protected override void UpdateDesktop()
        {
            // We only perform this expensive comparison once every 50 frames, if necessary
            if (DestroyAfterPlaying && ++frames % 50 == 0 && manager == null)
            {
                OnFinished?.Invoke();
                // We finally destroy ourselves once we're done
                Destroy(gameObject);
            }
        }
    }
}