using Michsky.UI.ModernUIPack;
using UnityEngine;
using UnityEngine.UI;

namespace SEE.Game.UI.ConfigMenu
{
    /// <summary>
    /// A button that is used in the sidebar of the config menu.
    /// It indicates the currently active page and allows for page switches.
    /// </summary>
    [RequireComponent(typeof(ButtonManagerBasic))]
    public class TabButton : MonoBehaviour
    {
        private ButtonManagerBasic _buttonManager;
        private UIGradient _uiGradient;
        private TabGroup _group;

        public bool isDefaultActive;
        public string buttonText;

        private Button _button;

        /// <summary>
        /// The color when the button is inactive.
        /// </summary>
        public Color inactiveColor = new Color(0.203f, 0.213f, 0.224f, 1f);

        /// <summary>
        /// The color when the button is active (selected).
        /// </summary>
        public Color activeColor = Color.white;

        /// <summary>
        /// The color when the button gets hovered.
        /// </summary>
        public Color hoverColor = new Color(0.45f, 0.55f, 0.72f, 1f);

        void Start()
        {
            _group = GetComponentInParent<TabGroup>();
            if (!_group)
            {
                Debug.LogError("TabButton is not in a TabGroup.");
            }

            _uiGradient = GetComponent<UIGradient>();
            _buttonManager = GetComponent<ButtonManagerBasic>();
            _button = _buttonManager.GetComponent<Button>();

            _buttonManager.normalText.text = buttonText;

            ResetStyles();
            _group.Subscribe(this);
            _buttonManager.clickEvent.AddListener(() => _group.OnTabSelected(this));
            var buttonColors = _button.colors;
            buttonColors.highlightedColor = hoverColor;
            _button.colors = buttonColors;

            if (isDefaultActive)
            {
                SetActive();
            }
        }

        public void ResetStyles()
        {
            _uiGradient.enabled = false;
            _buttonManager.normalText.color = inactiveColor;
            var buttonColors = _button.colors;
            var normalColor = buttonColors.normalColor;
            normalColor.a = 0;
            buttonColors.normalColor = normalColor;
            _button.colors = buttonColors;
        }

        public void SetActive()
        {
            _uiGradient.enabled = true;
            _buttonManager.normalText.color = activeColor;
            var buttonColors = _button.colors;
            var normalColor = buttonColors.normalColor;
            normalColor.a = 1;
            buttonColors.normalColor = normalColor;
            _button.colors = buttonColors;
        }
    }
}
