using SEE.GO;
using SEE.Utils;
using System.ComponentModel;
using TMPro;
using UnityEngine;

namespace SEE.UI
{
    /// <summary>
    /// A persistent UI overlay that displays status indicators, text labels, and tooltips for any system or component.
    /// Designed to remain visible independently of other UI elements, providing real-time feedback to the user.
    /// </summary>
    public class UIOverlay : PlatformDependentComponent
    {
        /// <summary>
        /// Prefab for the <see cref="UIOverlay"/>.
        /// </summary>
        private const string uiOverlayPrefab = UIPrefabFolder + "UIOverlay";

        /// <summary>
        /// The game object instantiated for the <see cref="uiOverlayPrefab"/>.
        /// </summary>
        private GameObject overlayGameObject;

        /// <summary>
        /// Color used for active states indicators.
        /// </summary>
        private static readonly Color activeColor = Color.green;

        /// <summary>
        /// Color used for inactive states indicators.
        /// </summary>
        private static readonly Color inactiveColor = Color.grey;

        /// <summary>
        /// Represents a generic UI status element consisting of:
        /// - a container GameObject controlling visibility,
        /// - a text label used as an indicator, and
        /// - an optional tooltip.
        ///
        /// This struct provides utility logic to update the visual state
        /// of the indicator and synchronize the tooltip message accordingly.
        /// It is intended for lightweight status representations in persistent UI overlays.
        /// </summary>
        private struct StatusIndicator
        {
            /// <summary>
            /// The GameObject that contains all UI elements belonging to this status indicator.
            /// Toggling its active state controls the overall visibility of the indicator.
            /// </summary>
            public GameObject Container;

            /// <summary>
            /// The visual text element representing the status. The text's color indicates
            /// whether the associated system is active or inactive.
            /// </summary>
            public TextMeshProUGUI Text;

            /// <summary>
            /// Optional tooltip shown when the user hovers over the indicator.
            /// The tooltip text is automatically updated to reflect the current state.
            /// </summary>
            public UIHoverTooltip Tooltip;

            /// <summary>
            /// Display label used to construct the tooltip message.
            /// For example: "Body Animator", "Livekit", or any other system name.
            /// </summary>
            public string Label;

            /// <summary>
            /// Updates the text color and tooltip message of the status indicator.
            /// </summary>
            /// <param name="active">If true, sets the indicator to the active state; otherwise to inactive.</param>
            /// <param name="activeColor">The color used when the indicator is active.</param>
            /// <param name="inactiveColor">The color used when the indicator is inactive.</param>
            public void SetActive(bool active, Color activeColor, Color inactiveColor)
            {
                if (Text != null)
                {
                    Text.color = active ? activeColor : inactiveColor;
                }
                if (Tooltip != null)
                {
                    Tooltip.Message = $"{Label} {(active ? "active" : "inactive")}";
                }
            }

            /// <summary>
            /// Shows or hides the entire status indicator.
            /// This affects the Container root object and therefore all of its child UI elements.
            /// </summary>
            /// <param name="visible">True to make the indicator visible, false to hide it.</param>
            public void ShowContainer(bool visible)
            {
                if (Container != null)
                {
                    Container.SetActive(visible);
                }
            }
        }

        /// <summary>
        /// Status indicator for the BodyAnimator.
        /// </summary>
        private static StatusIndicator bodyAnimator;

        /// <summary>
        /// Status indicator for the Livekit system.
        /// </summary>
        private static StatusIndicator livekit;

        /// <summary>
        /// The main webcam status icon. Its color reflects whether the webcam is active.
        /// </summary>
        private static TextMeshProUGUI webcamStatus;

        /// <summary>
        /// A slash overlay drawn across the webcam icon. Enabled when the webcam is inactive,
        /// hidden when the webcam is active.
        /// </summary>
        private static GameObject webcamSlashOverlay;

        /// <summary>
        /// Initializes the UI overlay for desktop platforms.
        /// Instantiates the overlay prefab, attaches it to the main canvas,
        /// and registers all status indicators and tooltip components used by the overlay.
        /// </summary>
        protected override void StartDesktop()
        {
            overlayGameObject = PrefabInstantiator.InstantiatePrefab(uiOverlayPrefab, Canvas.transform, false);
            RegisterWebcamOverlay();
        }

        /// <summary>
        /// Locates and initializes all UI elements inside the WebcamUIOverlay section of the prefab.
        /// This includes the webcam indicator, its slash overlay, and the BodyAnimator and Livekit
        /// status blocks including their tooltips.
        ///
        /// After initialization, all indicators are set to their inactive visual state.
        /// </summary>
        private void RegisterWebcamOverlay()
        {
            GameObject webcamOverlay = overlayGameObject.FindDescendant("WebcamUIOverlay");

            webcamStatus = webcamOverlay.FindDescendant("WebcamStatus").GetComponent<TextMeshProUGUI>();
            webcamSlashOverlay = webcamOverlay.FindDescendant("WebcamStatusSlash");

            GameObject bodyAnimatorObj = webcamOverlay.FindDescendant("BodyAnimatorStatus");
            bodyAnimator = new StatusIndicator
            {
                Container = bodyAnimatorObj,
                Text = bodyAnimatorObj.GetComponent<TextMeshProUGUI>(),
                Tooltip = bodyAnimatorObj.GetComponent<UIHoverTooltip>(),
                Label = "Body Animator"
            };

            GameObject livekitObj = webcamOverlay.FindDescendant("LivekitStatus");
            livekit = new StatusIndicator
            {
                Container = livekitObj,
                Text = livekitObj.GetComponent<TextMeshProUGUI>(),
                Tooltip = livekitObj.GetComponent<UIHoverTooltip>(),
                Label = "Livekit"
            };

            webcamSlashOverlay.GetComponent<TextMeshProUGUI>().color = inactiveColor;
            DeactivateWebcam();
            SetBodyAnimatorActive(false);
            SetLivekitActive(false);
        }

        /// <summary>
        /// Sets the webcam indicator to its inactive state.
        /// Hides the related system indicators, since they are only relevant
        /// when the webcam is enabled.
        /// </summary>
        public static void DeactivateWebcam()
        {
            webcamStatus.color = inactiveColor;
            webcamSlashOverlay.SetActive(true);
            bodyAnimator.ShowContainer(false);
            livekit.ShowContainer(false);
        }

        /// <summary>
        /// Sets the webcam indicator to its active state.
        /// Shows the related system indicators so that their status can be displayed.
        /// </summary>
        public static void ActivateWebcam()
        {
            webcamStatus.color = activeColor;
            webcamSlashOverlay.SetActive(false);
            bodyAnimator.ShowContainer(true);
            livekit.ShowContainer(true);
        }

        /// <summary>
        /// Updates the BodyAnimator status indicator and ensures its container is visible.
        /// </summary>
        private static void SetBodyAnimatorActive(bool active)
        {
            bodyAnimator.SetActive(active, activeColor, inactiveColor);
        }

        /// <summary>
        /// Updates the Livekit status indicator and ensures its container is visible.
        /// </summary>
        private static void SetLivekitActive(bool active)
        {
            livekit.SetActive(active, activeColor, inactiveColor);
        }

        /// <summary>
        /// Toggles the BodyAnimator status indicator.
        /// If the indicator is currently active, it will be set to inactive, and vice versa.
        /// The text color and tooltip message are updated accordingly.
        /// </summary>
        public static void ToggleBodyAnimator()
        {
            bool isActive = bodyAnimator.Text.color == activeColor;
            bodyAnimator.SetActive(!isActive, activeColor, inactiveColor);
        }

        /// <summary>
        /// Toggles the Livekit status indicator.
        /// If the indicator is currently active, it will be set to inactive, and vice versa.
        /// The text color and tooltip message are updated accordingly.
        /// </summary>
        public static void ToggleLivekit()
        {
            bool isActive = livekit.Text.color == activeColor;
            livekit.SetActive(!isActive, activeColor, inactiveColor);
        }
    }
}
