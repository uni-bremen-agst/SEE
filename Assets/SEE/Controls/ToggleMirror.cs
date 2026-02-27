using SEE.GO;
using UnityEngine;

namespace SEE.Controls
{
    /// <summary>
    /// Toggles the mirror.
    /// </summary>
    /// <remarks>This component is assumed to be attached to the mirror in the scene.
    /// That mirror object is assumed to have a child named <see cref="mirrorCamName"/>
    /// holding the mirror camera.</remarks>
    public class ToggleMirror : ToggleChildren
    {
        /// <summary>
        /// When the user requests to toggle the mirror, the children of the game object
        /// this component is attached to, will be enabled/disabled. The child object
        /// of the game object is the mirror object holding the camera mimicing a mirror.
        /// In other words, this camera is enabled or disabled, respectively.
        /// </summary>
        /// <returns>True if the user requested to toggle the mirror.</returns>
        protected override bool ToggleCondition()
        {
            bool result = SEEInput.ToggleMirror();
            // Was toggling requested and are we to toggle the mirror off?
            if (result && ChildrenAreActive())
            {
                /// Note: <see cref="ChildrenAreActive"/> gives us the state
                /// before the toggling takes place. That is, if it yields
                /// true, the children will be off in the next frame, but
                /// there are not off now.
                BlankMirror();
            }
            return result;
        }

        /// <summary>
        /// The texture where the content of the mirror camera is rendered.
        /// This is the surface of the mirror. We want to blank it such that
        /// the last pictured captured by the mirror camera disappears.
        /// </summary>
        private RenderTexture mirrorTexture;

        /// <summary>
        /// The name of the child of the gameObject holding the mirror camera.
        /// </summary>
        private const string mirrorCamName = "MirrorCam";

        /// <summary>
        /// Sets <see cref="mirrorTexture"/> if possible. If it cannot be
        /// found, an error message is emitted and this component gets disabled.
        /// </summary>
        private void Start()
        {
            Transform mirrorCam = gameObject.transform.Find(mirrorCamName);
            if (mirrorCam == null)
            {
                Debug.LogError($"{gameObject.FullName()} does not have a child named {mirrorCamName}. Mirror will be disabled.\n");
                enabled = false;
                return;
            }
            if (mirrorCam.TryGetComponent(out Camera camera))
            {
                mirrorTexture = camera.targetTexture;
            }
            else
            {
                Debug.LogError($"{mirrorCam.gameObject.FullName()} does not have a camera. Mirror will be disabled.\n");
                enabled = false;
                return;
            }
        }

        /// <summary>
        /// Blanks the mirror so that the last picture rendered disappears.
        /// </summary>
        private void BlankMirror()
        {
            // Set the "active" drawing target to be our mirror's texture
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture.active = mirrorTexture;

            // Clear the texture (Clear Depth, Clear Color, Color to use)
            GL.Clear(true, true, Color.black);

            // Restore the previous active target (so Unity keeps rendering the screen)
            RenderTexture.active = previousActive;
        }
    }
}
