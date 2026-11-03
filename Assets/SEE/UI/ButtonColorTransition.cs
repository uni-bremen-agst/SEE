using Michsky.MUIP;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// User interface components for SEE.
/// </summary>
namespace SEE.UI
{
    /// <summary>
    /// Preserves color transitions and content updates for buttons with a single set of text and icon objects.
    /// </summary>
    /// <remarks>
    /// Used with <see cref="ButtonManager"/> in Custom animation mode. The existing
    /// <see cref="Button"/> provides color transitions without separate graphics for each state.
    /// </remarks>
    [RequireComponent(typeof(ButtonManager), typeof(Button))]
    [DefaultExecutionOrder(1)]
    public class ButtonColorTransition : MonoBehaviour
    {
        /// <summary>
        /// The <see cref="ButtonManager"/> providing the content and click events.
        /// </summary>
        private ButtonManager manager;

        /// <summary>
        /// The <see cref="Button"/> providing the color transitions.
        /// </summary>
        private Button button;

        /// <summary>
        /// Caches the required button components.
        /// </summary>
        private void Awake()
        {
            manager = GetComponent<ButtonManager>();
            button = GetComponent<Button>();
        }

        /// <summary>
        /// Restores the color transition disabled by <see cref="ButtonManager"/> during initialization.
        /// </summary>
        /// <remarks>
        /// The execution order ensures that this runs after <see cref="ButtonManager"/> is enabled.
        /// </remarks>
        private void OnEnable()
        {
            button.transition = Selectable.Transition.ColorTint;
        }

        /// <summary>
        /// Applies the button content assigned by callers after instantiation.
        /// </summary>
        private void Start()
        {
            manager.UpdateUI();
        }

        /// <summary>
        /// Synchronizes <see cref="Button.interactable"/> with <see cref="ButtonManager.isInteractable"/>.
        /// </summary>
        private void LateUpdate()
        {
            button.interactable = manager.isInteractable;
        }
    }
}
