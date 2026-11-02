using Michsky.UI.ModernUIPack;
using SEE.Utils;
using SEE.Controls;
using SEE.DataModel.DG;

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SEE;
using SEE.Game.City;
using SEE.Game;
using SEE.GO;

using TMPro;
using UnityEngine.UI;
using Cypher;

using HighlightPlus;

namespace SEE.UI.Window.CypherQueryWindow
{
    /// <summary>
    /// Represents a snapshot item, which will be displayed in a list in the snapshot window.
    /// </summary>
    public class CypherQueryWindowCell : MonoBehaviour //: PlatformDependentComponent
    {
        public GraphElement Element { get; private set; }
        public GraphElementRef Reference { get; private set; }

        private Button button;
        private TMP_Text buttonText;

        public void Initialize(object value, GraphElementRef r = null)
        {
            button = transform
            .Find("Button")
            .gameObject
            .MustGetComponent<Button>();

            buttonText = button.transform
            .Find("Text (TMP)")
            .gameObject
            .GetComponentInChildren<TMP_Text>();

            if (value is GraphElement element)
            {
                this.Element = element;
                this.Reference = r;
                buttonText.text = element.ToShortString();
                button.interactable = true;
                button.onClick.AddListener(HighlightFromCell);
            }
            else
            {
                buttonText.text = value?.ToString() ?? "null";
                button.interactable = false;
            }
        }

        public void InitializeHeader(string text)
        {
            button = transform
            .Find("Button")
            .gameObject
            .MustGetComponent<Button>();

            buttonText = button.transform
            .Find("Text (TMP)")
            .gameObject
            .GetComponentInChildren<TMP_Text>();

            buttonText.text = text;
            button.interactable = false;

            // Hier Farbe
            var colors = button.colors;
            colors.disabledColor = new Color(0.25f, 0.25f, 0.25f);
            button.colors = colors;

            buttonText.color = Color.white;
        }

        public float PreferredWidth
        {
            get
            {
                return buttonText.preferredWidth;
            }
        }

        public void SetWidth(float width)
        {
            LayoutElement layoutElement = GetComponent<LayoutElement>();

            layoutElement.preferredWidth = width;
        }

        private void HighlightFromCell()
        {
            if (Reference != null) {
                GameObject g = Reference.gameObject;
                HighlightEffect effect = g.GetHighlightEffect();
                if (effect.outlineColor == Color.yellow) {
                    effect.outlineColor = Color.red;
                    effect.glowHQColor = Color.red;
                } else
                {
                    effect.outlineColor = Color.yellow;
                    effect.glowHQColor = Color.yellow;
                }
            }
        }
    }
}
