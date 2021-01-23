using OdinSerializer.Utilities;
using SEE.Controls;
using SEE.Utils;
using UnityEngine;

namespace SEE.Game.UI
{
    /// <summary>
    /// This class represents a component whose Start() and Update() method differs based on the current platform.
    /// Inheritors are expected to override the respective Start() and Update() methods (e.g. <see cref="StartVR()"/>.
    /// If the current platform's method was not overridden, the component will be destroyed.
    /// 
    /// This approach is especially suited for UI components, as their presentation is almost always different
    /// based on the platform.
    /// </summary>
    public abstract class PlatformDependentComponent: MonoBehaviour
    {

        /// <summary>
        /// The current platform.
        /// </summary>
        private PlayerSettings.PlayerInputType platform;

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
        protected virtual void UpdateDesktop() => PlatformUnsupported();
        /// <summary>
        /// Called when the <see cref="Update()"/> method of this component is executed on the TouchGamepad platform.
        /// </summary>
        protected virtual void UpdateTouchGamepad() => PlatformUnsupported();
        /// <summary>
        /// Called when the <see cref="Update()"/> method of this component is executed on the VR platform.
        /// </summary>
        protected virtual void UpdateVR() => PlatformUnsupported();
        /// <summary>
        /// Called when the <see cref="Update()"/> method of this component is executed on the HoloLens platform.
        /// </summary>
        protected virtual void UpdateHoloLens() => PlatformUnsupported();

        
        protected void Start()
        {
            platform = PlayerSettings.GetInputType();
            switch (platform)
            {
                case PlayerSettings.PlayerInputType.Desktop: StartDesktop();
                    break;
                case PlayerSettings.PlayerInputType.TouchGamepad: StartTouchGamepad();
                    break;
                case PlayerSettings.PlayerInputType.VR: StartVR();
                    break;
                case PlayerSettings.PlayerInputType.HoloLens: StartHoloLens();
                    break;
                case PlayerSettings.PlayerInputType.None: // no UI has to be rendered
                    break;  
                default: PlatformUnsupported();
                    break;
            }
        }

        protected void Update()
        {
            switch (platform)
            {
                case PlayerSettings.PlayerInputType.Desktop: UpdateDesktop();
                    break;
                case PlayerSettings.PlayerInputType.TouchGamepad: UpdateTouchGamepad();
                    break;
                case PlayerSettings.PlayerInputType.VR: UpdateVR();
                    break;
                case PlayerSettings.PlayerInputType.HoloLens: UpdateHoloLens();
                    break;
                case PlayerSettings.PlayerInputType.None: // no UI has to be rendered
                    break;  
                default: PlatformUnsupported();
                    break;
            }
        }

        /// <summary>
        /// Logs an error with information about this platform and component and destroys this component.
        /// </summary>
        private void PlatformUnsupported()
        {
            Debug.LogError($"Component '{GetType().GetNiceName()}' doesn't support platform '{platform.ToString()}'."
                           + " Component will now self-destruct.");
            Destroyer.DestroyComponent(this);
        }

    }
}