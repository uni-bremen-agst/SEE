using SEE.GO;
using SEE.Utils;
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
        /// String constant representing the "active" status, used in tooltip messages.
        /// Used for a tooltip.
        /// </summary>
        private const string active = "active";

        /// <summary>
        /// String constant representing the "inactive" status, used in tooltip messages.
        /// </summary>
        private const string inactive = "inactive";

        /// <summary>
        /// Indicator for the webcam status (contains an text with a fontawesome icon).
        /// The color reflects the current state (<see cref="activeColor"/> / <see cref="inactiveColor"/>).
        /// </summary>
        private static TextMeshProUGUI webcamStatus;

        /// <summary>
        /// Slash overlay placed over the webcam icon.
        /// Visible when the webcam is deactivated, hidden when the webcam is active.
        /// </summary>
        private static GameObject webcamSlashOverlay;

        /// <summary>
        /// Container GameObject holding the BodyAnimator status UI elements.
        /// Can be hidden or shown depending on overlay state.
        /// </summary>
        private static GameObject bodyAnimatorContainer;

        /// <summary>
        /// Indicator for the BodyAnimator status (contains an text with a fontawesome icon).
        /// The color reflects the current state  (<see cref="activeColor"/> / <see cref="inactiveColor"/>).
        /// </summary>
        private static TextMeshProUGUI bodyAnimatorText;

        /// <summary>
        /// Tooltip component for the BodyAnimator status indicator.
        /// Displays additional information on hover.
        /// </summary>
        private static UIHoverTooltip bodyAnimatorTooltip;

        /// <summary>
        /// Represents the "Body Animator" string used by video system tooltips.
        /// </summary>
        private const string bodyanimator = "Body Animator";

        /// <summary>
        /// Container GameObject holding the Livekit status UI elements.
        /// Can be hidden or shown depending on overlay state.
        /// </summary>
        private static GameObject livekitContainer;

        /// <summary>
        /// Indicator for the Livekit status (contains an text with a fontawesome icon).
        /// The color reflects the current state  (<see cref="activeColor"/> / <see cref="inactiveColor"/>).
        /// </summary>
        private static TextMeshProUGUI livekitText;

        /// <summary>
        /// Tooltip component for the Livekit status indicator.
        /// Displays additional information on hover.
        /// </summary>
        private static UIHoverTooltip livekitTooltip;

        /// <summary>
        /// Represents the "Livekit" string used by video system tooltips.
        /// </summary>
        private const string livekit = "Livekit";

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
        /// Finds and initializes all relevant UI elements inside the webcam overlay (WebcamUIOverlay).
        /// Sets the slash overlay to inactiveColor, and initializes all status displays to inactive state.
        /// </summary>
        private void RegisterWebcamOverlay()
        {
            GameObject webcamOverlay = overlayGameObject.FindDescendant("WebcamUIOverlay");
            webcamStatus = webcamOverlay.FindDescendant("WebcamStatus").GetComponent<TextMeshProUGUI>();
            webcamSlashOverlay = webcamOverlay.FindDescendant("WebcamStatusSlash");
            bodyAnimatorContainer = webcamOverlay.FindDescendant("BodyAnimatorStatus");
            bodyAnimatorText = bodyAnimatorContainer.GetComponent<TextMeshProUGUI>();
            bodyAnimatorTooltip = bodyAnimatorContainer.GetComponent<UIHoverTooltip>();
            livekitContainer = webcamOverlay.FindDescendant("LivekitStatus");
            livekitText = livekitContainer.GetComponent<TextMeshProUGUI>();
            livekitTooltip = livekitContainer.GetComponent<UIHoverTooltip>();

            webcamSlashOverlay.GetComponent<TextMeshProUGUI>().color = inactiveColor;
            DeactivateWebcam();
            DeactivateBodyAnimator();
            DeactivateLivekit();
        }

        /// <summary>
        /// Deactivates the webcam UI indicator and updates the related status elements.
        /// When the webcam is deactivated:
        /// - The webcam text label is set to the inactive color.
        /// - The slash overlay is shown to indicate that the webcam is off.
        /// - The BodyAnimator and Livekit status objects are hidden,
        ///   because they are only relevant when the webcam is active.
        /// </summary>
        public static void DeactivateWebcam()
        {
            webcamStatus.color = inactiveColor;
            webcamSlashOverlay.SetActive(true);
            bodyAnimatorContainer.SetActive(false);
            livekitContainer.SetActive(false);
        }

        /// <summary>
        /// Activates the webcam UI indicator and updates the related status elements.
        /// When the webcam is activated:
        /// - The webcam text label is set to the active color.
        /// - The slash overlay is hidden.
        /// - The BodyAnimator and Livekit status objects are made visible,
        ///   allowing their respective text labels to show the current state via color.
        /// </summary>
        public static void ActivateWebcam()
        {
            webcamStatus.color = activeColor;
            webcamSlashOverlay.SetActive(false);
            bodyAnimatorContainer.SetActive(true);
            livekitContainer.SetActive(true);
        }

        /// <summary>
        /// Updates the BodyAnimator status icon to the inactive state.
        /// </summary>
        public static void DeactivateBodyAnimator()
        {
            bodyAnimatorText.color = inactiveColor;
            bodyAnimatorTooltip.Message = bodyanimator + " " + inactive;
        }

        /// <summary>
        /// Updates the BodyAnimator status icon to the active state.
        /// </summary>
        public static void ActivateBodyAnimator()
        {
            bodyAnimatorText.color = activeColor;
            bodyAnimatorTooltip.Message = bodyanimator + " " + active;
        }

        /// <summary>
        /// Updates the Livekit status icon to the inactive state.
        /// </summary>
        public static void DeactivateLivekit()
        {
            livekitText.color = inactiveColor;
            livekitTooltip.Message = livekit + " " + inactive;
        }

        /// <summary>
        /// Updates the Livekit status icon to the active state.
        /// </summary>
        public static void ActivateLivekit()
        {
            livekitText.color = activeColor;
            livekitTooltip.Message = livekit + " " + active;
        }
    }
}
