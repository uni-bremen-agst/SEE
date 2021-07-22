// Copyright 2021 Ruben Smidt
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS
// BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR
// IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

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
        public bool IsDefaultActive;
        public string ButtonText;

        private ButtonManagerBasic buttonManager;
        private UIGradient uiGradient;
        private TabGroup group;

        private Button button;

        /// <summary>
        /// The color when the button is inactive.
        /// </summary>
        public Color InactiveColor = new Color(0.203f, 0.213f, 0.224f, 1f);

        /// <summary>
        /// The color when the button is active (selected).
        /// </summary>
        public Color ActiveColor = Color.white;

        /// <summary>
        /// The color when the button gets hovered.
        /// </summary>
        public Color HoverColor = new Color(0.45f, 0.55f, 0.72f, 1f);
        private void Start()
        {
            group = GetComponentInParent<TabGroup>();
            if (!group)
            {
                throw new System.Exception("TabButton is not in a TabGroup.");
            }

            uiGradient = GetComponent<UIGradient>();
            buttonManager = GetComponent<ButtonManagerBasic>();
            button = buttonManager.GetComponent<Button>();

            buttonManager.normalText.text = ButtonText;

            ResetStyles();
            group.Subscribe(this);
            buttonManager.clickEvent.AddListener(() => group.OnTabSelected(this));
            ColorBlock buttonColors = button.colors;
            buttonColors.highlightedColor = HoverColor;
            button.colors = buttonColors;

            if (IsDefaultActive)
            {
                SetActive();
            }
        }
        public void ResetStyles()
        {
            uiGradient.enabled = false;
            buttonManager.normalText.color = InactiveColor;
            ColorBlock buttonColors = button.colors;
            Color normalColor = buttonColors.normalColor;
            normalColor.a = 0;
            buttonColors.normalColor = normalColor;
            button.colors = buttonColors;
        }
        public void SetActive()
        {
            uiGradient.enabled = true;
            buttonManager.normalText.color = ActiveColor;
            ColorBlock buttonColors = button.colors;
            Color normalColor = buttonColors.normalColor;
            normalColor.a = 1;
            buttonColors.normalColor = normalColor;
            button.colors = buttonColors;
        }
    }
}
