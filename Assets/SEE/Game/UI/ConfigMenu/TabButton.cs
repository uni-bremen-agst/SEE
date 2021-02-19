using Michsky.UI.ModernUIPack;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Michsky.UI.ModernUIPack;

namespace SEE.Game.UI.ConfigMenu
{
    [RequireComponent(typeof(ButtonManagerBasic))]
    public class TabButton : MonoBehaviour
    {
        private ButtonManagerBasic _buttonManager;
        private UIGradient _uiGradient;
        private TabGroup _group;
        
        public bool isDefaultActive;
        public string buttonText;

        private Button _button;

        public Color inactiveColor = new Color(0.203f, 0.213f, 0.224f, 1f);
        public Color activeColor = Color.white;
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