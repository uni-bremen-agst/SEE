using OdinSerializer.Utilities;
using SEE.Controls;
using SEE.GO;
using SEE.Utils;
using UnityEngine;

namespace SEE.Game.UI
{
    /// <summary>
    /// This class represents a component whose Start() and Update() method differs based on the current platform.
    /// Inheritors are expected to override the respective Start() and Update() methods (e.g. <see cref="StartVR()"/>.
    /// If the current platform's start method was not overridden, the component will be destroyed.
    /// If the current platform's update method was not overridden, nothing will happen.
    ///
    /// This approach is especially well suited for UI components, as their presentation is almost always different
    /// based on the platform.
    /// </summary>
    public abstract class PlatformDependentComponent: MonoBehaviour
    {
        /// <summary>
        /// Name of the canvas on which UI elements are placed.
        /// Note that for HoloLens, the canvas will be converted to an MRTK canvas.
        /// </summary>
        private const string UI_CANVAS_NAME = "UI Canvas";

        /// <summary>
        /// Path to where the UI Canvas prefab is stored.
        /// This prefab should contain all components necessary for the UI canvas, such as an event system,
        /// a graphic raycaster, etc.
        /// </summary>
        private const string UI_CANVAS_PREFAB = "Prefabs/UI/UICanvas";

        /// <summary>
        /// The canvas on which UI elements are placed.
        /// This GameObject must be named <see cref="UI_CANVAS_NAME"/>.
        /// If it doesn't exist yet, it will be created from a prefab.
        /// </summary>
        protected GameObject Canvas;

        /// <summary>
        /// The current platform.
        /// </summary>
        protected PlayerInputType Platform { get; private set; }

        /// <summary>
        /// Whether the <see cref="Start"/> method of this component has already been called.
        /// </summary>
        protected bool HasStarted { get; private set; } = false;

        /// <summary>
        /// Called when the <see cref="Start()"/> method of this component is executed on the Desktop platform.
        /// </summary>
        protected virtual void StartDesktop() => PlatformUnsupported();
        /// <summary>
        /// Called when the <see cref="Start()"/> method of this component is executed on the TouchGamepad platform.
        /// </summary>
        protected virtual void StartTouchGamepad() => PlatformUnsupported();
        /// <summary>
        /// Called when the <see cref="Start()"/> method of this component is executed on the VR platform.
        /// </summary>
        protected virtual void StartVR() => PlatformUnsupported();
        /// <summary>
        /// Called when the <see cref="Start()"/> method of this component is executed on the HoloLens platform.
        /// </summary>
        protected virtual void StartHoloLens() => PlatformUnsupported();

        /// <summary>
        /// Called when the <see cref="Update()"/> method of this component is executed on the Desktop platform.
        /// </summary>
        protected virtual void UpdateDesktop() { }
        /// <summary>
        /// Called when the <see cref="Update()"/> method of this component is executed on the TouchGamepad platform.
        /// </summary>
        protected virtual void UpdateTouchGamepad() { }

        /// <summary>
        /// Called when the <see cref="Update()"/> method of this component is executed on the VR platform.
        /// </summary>
        protected virtual void UpdateVR() { }

        /// <summary>
        /// Called when the <see cref="Update()"/> method of this component is executed on the HoloLens platform.
        /// </summary>
        protected virtual void UpdateHoloLens() { }

        protected void Start()
        {
            Canvas = GameObject.Find(UI_CANVAS_NAME);
            if (Canvas == null)
            {
                // Create Canvas from prefab if it doesn't exist yet
                Canvas = PrefabInstantiator.InstantiatePrefab(UI_CANVAS_PREFAB);
                Canvas.name = UI_CANVAS_NAME;
            }

            HasStarted = true;

            // Execute platform dependent code
            Platform = PlayerSettings.GetInputType();
            switch (Platform)
            {
                case PlayerInputType.DesktopPlayer: StartDesktop();
                    break;
                case PlayerInputType.TouchGamepadPlayer: StartTouchGamepad();
                    break;
                case PlayerInputType.VRPlayer: StartVR();
                    //TODO: Apply CurvedUI to canvas
                    break;
                case PlayerInputType.HoloLensPlayer: StartHoloLens();
                    //TODO: Convert to MRTK Canvas and add NearInteractionTouchableUnityUI, as recommended
                    break;
                case PlayerInputType.None: // no UI has to be rendered
                    break;
                default: PlatformUnsupported();
                    break;
            }

        }

        protected void Update()
        {
            switch (Platform)
            {
                case PlayerInputType.DesktopPlayer: UpdateDesktop();
                    break;
                case PlayerInputType.TouchGamepadPlayer: UpdateTouchGamepad();
                    break;
                case PlayerInputType.VRPlayer: UpdateVR();
                    break;
                case PlayerInputType.HoloLensPlayer: UpdateHoloLens();
                    break;
                case PlayerInputType.None: // no UI has to be rendered
                    break;
                default: PlatformUnsupported();
                    break;
            }
        }

        /// <summary>
        /// Logs an error with information about this platform and component and destroys this component.
        /// </summary>
        protected void PlatformUnsupported()
        {
            Debug.LogError($"Component '{GetType().GetNiceName()}' doesn't support platform '{Platform.ToString()}'."
                           + " Component will now self-destruct.");
            Destroyer.DestroyComponent(this);
        }
    }
}
