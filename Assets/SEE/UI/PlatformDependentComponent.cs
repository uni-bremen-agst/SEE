﻿using SEE.Controls;
using SEE.GO;
using SEE.Utils;
using UnityEngine;
using Sirenix.Utilities;
using UnityEngine.Events;

namespace SEE.UI
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
    public abstract class PlatformDependentComponent : MonoBehaviour
    {
        /// <summary>
        /// The folder where to find UI Prefabs.
        /// </summary>
        protected const string UIPrefabFolder = "Prefabs/UI/";

        /// <summary>
        /// Name of the canvas on which UI elements are placed.
        /// </summary>
        private const string uiCanvasName = "UI Canvas";

        /// <summary>
        /// Path to where the UI Canvas prefab is stored.
        /// This prefab should contain all components necessary for the UI canvas, such as an event system,
        /// a graphic raycaster, etc.
        /// </summary>
        private const string uiCanvasPrefab = UIPrefabFolder + "UICanvas";

        /// <summary>
        /// The canvas on which UI elements are placed.
        /// </summary>
        protected static GameObject Canvas { get; private set; }

        /// <summary>
        /// The current platform.
        /// </summary>
        protected PlayerInputType Platform { get; private set; }

        /// <summary>
        /// Whether the component is initialized.
        /// </summary>
        /// <see cref="Start"/>
        protected bool HasStarted { get; private set; }

        /// <summary>
        /// Initializes the component for the current platform.
        /// </summary>
        /// <remarks>
        /// Only override this if you need to execute platform-independent code on startup.
        /// Otherwise, use <see cref="StartDesktop"/>, <see cref="StartVR"/> or <see cref="StartTouchGamepad"/>.
        /// </remarks>
        protected virtual void Start()
        {
            // initializes the Canvas if necessary
            if (Canvas == null)
            {
                // TODO: Is it needed to search for the UI canvas?
                // The canvas is now static and nobody else should instantiate the canvas...
                Canvas = GameObject.Find(uiCanvasName) ?? PrefabInstantiator.InstantiatePrefab(uiCanvasPrefab);
                Canvas.name = uiCanvasName;
            }

            // calls the start method for the current platform
            Platform = SceneSettings.InputType;
            switch (Platform)
            {
                case PlayerInputType.DesktopPlayer:
                    StartDesktop();
                    break;
                case PlayerInputType.TouchGamepadPlayer:
                    StartTouchGamepad();
                    break;
                case PlayerInputType.VRPlayer:
                    StartVR();
                    //TODO: Apply CurvedUI to canvas
                    break;
                case PlayerInputType.None: // no UI has to be rendered
                    break;
                default:
                    PlatformUnsupported();
                    break;
            }

            // initialization finished
            HasStarted = true;
            OnStartFinished();
            OnComponentInitialized?.Invoke();
        }

        /// <summary>
        /// Updates the component for the current platform.
        /// </summary>
        protected virtual void Update()
        {
            switch (Platform)
            {
                case PlayerInputType.DesktopPlayer:
                    UpdateDesktop();
                    break;
                case PlayerInputType.TouchGamepadPlayer:
                    UpdateTouchGamepad();
                    break;
                case PlayerInputType.VRPlayer:
                    UpdateVR();
                    break;
                case PlayerInputType.None: // no UI has to be rendered
                    break;
                default:
                    PlatformUnsupported();
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
            Destroyer.Destroy(this);
        }

        /// <summary>
        /// Start method for the Desktop platform.
        /// </summary>
        protected virtual void StartDesktop() => PlatformUnsupported();

        /// <summary>
        /// Start method for the VR platform.
        /// </summary>
        protected virtual void StartVR() => PlatformUnsupported();

        /// <summary>
        /// Start method for the TouchGamepad platform.
        /// </summary>
        protected virtual void StartTouchGamepad() => PlatformUnsupported();

        /// <summary>
        /// Update method for the Desktop platform.
        /// </summary>
        protected virtual void UpdateDesktop() { }

        /// <summary>
        /// Update method for the VR platform.
        /// </summary>
        protected virtual void UpdateVR() { }

        /// <summary>
        /// Update method for the TouchGamepad platform.
        /// </summary>
        protected virtual void UpdateTouchGamepad() { }

        /// <summary>
        /// Triggered when the component was started. (<see cref="Start"/>)
        /// Can be used add listeners and update UI after initialization.
        /// </summary>
        protected virtual void OnStartFinished() { }

        /**
         * Triggers when the component has been initialized, i.e., when the Start() method has been called.
         */
        public event UnityAction OnComponentInitialized;
    }
}
