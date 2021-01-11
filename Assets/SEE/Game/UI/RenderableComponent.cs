using OdinSerializer.Utilities;
using SEE.Controls;
using UnityEngine;

namespace SEE.Game.UI
{
    /// <summary>
    /// This class represents a UI component which can be rendered (i.e. displayed in the game)
    /// on all platforms present in the <see cref="RenderActions"/> field.
    /// </summary>
    public abstract class RenderableComponent: MonoBehaviour
    {
        /// <summary>
        /// Renders this component with a UI based on the given <paramref name="inputType"/>.
        /// If the platform is unsupported, false will be returned and nothing will be rendered.
        /// </summary>
        /// <param name="inputType">The platform for which this UI shall be rendered. Must not be null.</param>
        /// <returns>true iff the given platform is supported.
        /// In the case false is returned, no UI is rendered.</returns>
        protected abstract bool RenderComponent(PlayerSettings.PlayerInputType inputType);
        
        protected void Awake()
        {
            // Render UI for current platform.
            if (!RenderComponent(PlayerSettings.GetInputType()))
            {
                Debug.LogError($"Component '{GetType().GetNiceName()}' doesn't support "
                               + $"platform '{PlayerSettings.GetInputType().ToString()}'.");
            }
        }

    }
}