using SEE.Controls.Actions;
using SEE.Controls.Actions.Drawable;
using SEE.UI.Menu.Drawable;
using SEE.Utils;
using UnityEngine;

namespace SEE.UI.Drawable
{
    /// <summary>
    /// This class ensures that the color picker menu is hidden when an action state change occurs.
    /// (It is required to prevent a display error of the switch. See <see cref="ColorPickerAction.Awake"/>.)
    /// </summary>
    public class ColorPickerMenuDisabler : MonoBehaviour
    {
        /// <summary>
        /// Checks every frame if the action has changed.
        /// If yes, the component will be destroyed.
        /// </summary>
        private void Update()
        {
            if (GlobalActionHistory.Current() != ActionStateTypes.ColorPicker)
            {
                gameObject.Destroy<ColorPickerMenuDisabler>();
            }
        }

        /// <summary>
        /// If this component will be destroyed, it destroys the color picker menu.
        /// </summary>
        private void OnDestroy()
        {
            ColorPickerMenu.Instance.Destroy();
        }
    }
}