using SEE.Game.Drawable.ValueHolders;
using SEE.Utils.Config;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace SEE.Game.Drawable.Configurations
{
    /// <summary>
    /// The configuration class for a drawable text.
    /// </summary>
    [Serializable]
    public class TextConf : DrawableType, ICloneable
    {
        /// <summary>
        /// The written text.
        /// </summary>
        public string Text;

        /// <summary>
        /// The text font color.
        /// </summary>
        public Color FontColor;

        /// <summary>
        /// The font outline color of the text.
        /// </summary>
        public Color OutlineColor;

        /// <summary>
        /// Whether the outline color is enabled or disabled.
        /// True = enabled; false = disabled.
        /// </summary>
        public bool IsOutlined;

        /// <summary>
        /// The thickness for the text outline.
        /// </summary>
        public float OutlineThickness;

        /// <summary>
        /// The current chosen font styles.
        /// </summary>
        public FontStyles FontStyles;

        /// <summary>
        /// The font size of the text.
        /// </summary>
        public float FontSize;

        /// <summary>
        /// Creates a <see cref="TextConf"/> for the given game object.
        /// </summary>
        /// <param name="textObject">The game object with the <see cref="TextMeshPro"/> component</param>
        /// <returns>The created <see cref="TextConf"/> object</returns>
        public static TextConf GetText(GameObject textObject)
        {
            TextConf text = null;
            if (textObject != null && textObject.CompareTag(Tags.DText))
            {
                TextMeshPro tmp = textObject.GetComponent<TextMeshPro>();
                text = new()
                {
                    Id = textObject.name,
                    AssociatedPage = textObject.GetComponent<AssociatedPageHolder>().AssociatedPage,
                    Position = textObject.transform.localPosition,
                    EulerAngles = textObject.transform.localEulerAngles,
                    Scale = textObject.transform.localScale,
                    Text = tmp.text,
                    FontColor = tmp.color,
                    OutlineColor = tmp.outlineColor,
                    IsOutlined = tmp.fontMaterial.IsKeywordEnabled(GameTexter.OutlineKeyWord),
                    OutlineThickness = tmp.outlineWidth,
                    FontStyles = tmp.fontStyle,
                    FontSize = tmp.fontSize,
                    OrderInLayer = textObject.GetComponent<OrderInLayerValueHolder>().OrderInLayer
                };
            }

            return text;
        }

        /// <summary>
        /// Returns a clone of this <see cref="TextConf"/> object.
        /// </summary>
        /// <returns>A new <see cref="TextConf"/> with the values of this object.</returns>
        public object Clone()
        {
            return new TextConf
            {
                Id = this.Id,
                AssociatedPage = this.AssociatedPage,
                Position = this.Position,
                EulerAngles = this.EulerAngles,
                Scale = this.Scale,
                Text = this.Text,
                FontColor = this.FontColor,
                OutlineColor = this.OutlineColor,
                IsOutlined = this.IsOutlined,
                OutlineThickness = this.OutlineThickness,
                FontStyles = this.FontStyles,
                FontSize = this.FontSize,
                OrderInLayer = this.OrderInLayer
            };
        }

        #region Config I/O

        /// <summary>
        /// Label in the configuration file for the text of a text.
        /// </summary>
        private const string textLabel = "TextLabel";

        /// <summary>
        /// Label in the configuration file for the font color of a text.
        /// </summary>
        private const string fontColorLabel = "FontColorLabel";

        /// <summary>
        /// Label in the configuration file for the outline color of a text.
        /// </summary>
        private const string outlineColorLabel = "OutlineColorLabel";

        /// <summary>
        /// Label in the configuration file for the outline color status.
        /// </summary>
        private const string outlineStatusLabel = "OutlineStatusLabel";

        /// <summary>
        /// Label in the configuration file for the thickness of a text outline.
        /// </summary>
        private const string outlineThicknessColorLabel = "OutlineThicknessColorLabel";

        /// <summary>
        /// Label in the configuration file for the font size of a text.
        /// </summary>
        private const string fontSizeLabel = "FontSizeLabel";

        /// <summary>
        /// Label in the configuration file for the font styles of a text.
        /// </summary>
        private const string fontStylesLabel = "FontStylesLabel";

        /// <summary>
        /// Saves this instance's attributes using the given <see cref="ConfigWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="ConfigWriter"/> to write the attributes.</param>
        protected override void SaveAttributes(ConfigWriter writer)
        {
            writer.Save(Text, textLabel);
            writer.Save(FontColor, fontColorLabel);
            writer.Save(IsOutlined, outlineStatusLabel);
            writer.Save(OutlineColor, outlineColorLabel);
            writer.Save(OutlineThickness, outlineThicknessColorLabel);
            writer.Save(FontSize, fontSizeLabel);
            writer.Save(FontStyles.ToString(), fontStylesLabel);
        }

        /// <summary>
        /// Given the representation of a <see cref="LineConf"/> as created by the <see cref="ConfigWriter"/>, this
        /// method parses the attributes from that representation and puts them into this <see cref="LineConf"/>
        /// instance.
        /// </summary>
        /// <param name="attributes">A list of labels (strings) of attributes and their values (objects). This
        /// has to be the representation of a <see cref="LineConf"/> as created by
        /// <see cref="ConfigWriter"/>.</param>
        /// <returns>Whether or not the <see cref="LineConf"/> was loaded without errors.</returns>
        override internal bool Restore(Dictionary<string, object> attributes)
        {
            bool errors = base.Restore(attributes);

            /// Try to restore the text.
            if (attributes.TryGetValue(textLabel, out object txt))
            {
                Text = (string)txt;
            }
            else
            {
                errors = true;
            }

            /// Try to restore the font color.
            Color loadedFontColor = Color.black;
            if (ConfigIO.Restore(attributes, fontColorLabel, ref loadedFontColor))
            {
                FontColor = loadedFontColor;
            }
            else
            {
                FontColor = Color.black;
                errors = true;
            }

            /// Try to restore the outline color.
            Color loadedOutlineColor = Color.clear;
            if (ConfigIO.Restore(attributes, outlineColorLabel, ref loadedOutlineColor))
            {
                OutlineColor = loadedOutlineColor;
            }
            else
            {
                OutlineColor = Color.clear;
                errors = true;
            }

            /// Try to restore the outline status.
            if (attributes.TryGetValue(outlineStatusLabel, out object status))
            {
                IsOutlined = (bool)status;
            }
            else
            {
                IsOutlined = false;
                errors = true;
            }

            /// Try to restore the outline thickness.
            if (attributes.TryGetValue(outlineThicknessColorLabel, out object thickness))
            {
                OutlineThickness = (float)thickness;
            }
            else
            {
                OutlineThickness = 0.4f;
                errors = true;
            }

            /// Try to restore the font size.
            if (attributes.TryGetValue(fontSizeLabel, out object size))
            {
                FontSize = (float)size;
            }
            else
            {
                FontSize = 0.5f;
                errors = true;
            }

            /// Try to restore the font styles.
            if (attributes.TryGetValue(fontStylesLabel, out object styles)
                && Enum.TryParse<FontStyles>((string)styles, out FontStyles result))
            {
                FontStyles = result;
            }
            else
            {
                FontStyles = FontStyles.Normal;
                errors = true;
            }

            return !errors;
        }

        #endregion
    }
}