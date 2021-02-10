using Michsky.UI.ModernUIPack;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Michsky.UI.ModernUIPack;

namespace SEE.UI.Tab
{
    [RequireComponent(typeof(ButtonManagerBasic))]
    public class TabButton : MonoBehaviour
    {
        public ButtonManagerBasic buttonManager;
        public UIGradient uiGradient;
        public TabGroup group;
        private Button _button;

        public Color inactiveColor = new Color(0.203f, 0.213f, 0.224f, 1f);
        public Color activeColor = Color.white;
        public Color hoverColor = new Color(0.45f, 0.55f, 0.72f, 1f);

        private void Awake()
        {
            group = GetComponentInParent<TabGroup>();
            if (!group)
            {
                Debug.LogError("TabButton is not in a TabGroup.");
            }

            uiGradient = GetComponent<UIGradient>();
            buttonManager = GetComponent<ButtonManagerBasic>();
            _button = buttonManager.GetComponent<Button>();
        }

        void Start()
        {
            ResetStyles();
            group.Subscribe(this);
            buttonManager.clickEvent.AddListener(() => group.OnTabSelected(this));
            var buttonColors = _button.colors;
            buttonColors.highlightedColor = hoverColor;
            _button.colors = buttonColors;
        }

        public void ResetStyles()
        {
            uiGradient.enabled = false;
            buttonManager.normalText.color = inactiveColor;
            var buttonColors = _button.colors;
            var normalColor = buttonColors.normalColor;
            normalColor.a = 0;
            buttonColors.normalColor = normalColor;
            _button.colors = buttonColors;
        }

        public void SetActive()
        {
            uiGradient.enabled = true;
            buttonManager.normalText.color = activeColor;
            var buttonColors = _button.colors;
            var normalColor = buttonColors.normalColor;
            normalColor.a = 1;
            buttonColors.normalColor = normalColor;
            _button.colors = buttonColors;
        }
    }
}