using SEE.GO;
using SEE.Utils;
using UnityEngine;

namespace SEE.Game.UI.CodeWindow
{
    /// <summary>
    /// Represents a movable window.
    /// </summary>
    public abstract class BaseWindow : PlatformDependentComponent
    {
        /// <summary>
        /// The title (e.g. filename) for the window.
        /// </summary>
        public string Title;

        /// <summary>
        /// Resolution of the window.
        /// </summary>
        protected Vector2 Resolution = new(900, 500);

        /// <summary>
        /// GameObject containing the actual UI for the window.
        /// </summary>
        public GameObject window { get; protected set; }

        /// <summary>
        /// Path to the window canvas prefab.
        /// </summary>
        private const string WINDOW_PREFAB = "Prefabs/UI/BaseWindow";

        protected override void StartDesktop()
        {
            if (Title == null)
            {
                Debug.LogError("Title must be defined when setting up Window!\n");
                return;
            }

            window = PrefabInstantiator.InstantiatePrefab(WINDOW_PREFAB, Canvas.transform, false);

            // Position code window in center of screen
            window.transform.localPosition = Vector3.zero;

            // Set resolution to preferred values
            if (window.TryGetComponentOrLog(out RectTransform rect))
            {
                rect.sizeDelta = Resolution;
            }
        }

        /// <summary>
        /// Shows or hides the window, depending on the <see cref="show"/> parameter.
        /// </summary>
        /// <param name="show">Whether the window should be shown.</param>
        /// <remarks>If this window is used in a <see cref="CodeSpace"/>, this method
        /// needn't (and shouldn't) be used.</remarks>
        public void Show(bool show)
        {
            switch (Platform)
            {
                case PlayerInputType.DesktopPlayer:
                    ShowDesktop(show);
                    break;
                case PlayerInputType.TouchGamepadPlayer:
                    ShowDesktop(show);
                    break;
                case PlayerInputType.VRPlayer:
                    PlatformUnsupported();
                    break;
                case PlayerInputType.None: // nothing needs to be done
                    break;
                default:
                    Debug.LogError($"Platform {Platform} not handled in switch case.\n");
                    break;
            }
        }

        /// <summary>
        /// When disabling the window, its controlled UI objects will also be disabled.
        /// </summary>
        public void OnDisable()
        {
            if (window)
            {
                window.SetActive(false);
            }
        }

        /// <summary>
        /// When enabling the window, its controlled UI objects will also be enabled.
        /// </summary>
        public void OnEnable()
        {
            if (window)
            {
                window.SetActive(true);
            }
        }

        /// <summary>
        /// Shows or hides the window on Desktop platforms.
        /// </summary>
        /// <param name="show">Whether the window should be shown.</param>
        private void ShowDesktop(bool show)
        {
            if (window)
            {
                window.SetActive(show);
            }
        }
    }
}