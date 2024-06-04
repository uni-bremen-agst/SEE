using SEE.Game.Drawable.ValueHolders;
using SEE.Utils;
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
        public string text;

        /// <summary>
        /// The text font color.
        /// </summary>
        public Color fontColor;

        /// <summary>
        /// The font outline color of the text.
        /// </summary>
        public Color outlineColor;

        /// <summary>
        /// The status if the outline color is enabled or disabled.
        /// True = enabled; false = disabled.
        /// </summary>
        public bool outlineStatus;

        /// <summary>
        /// The thickness for the text outline.
        /// </summary>
        public float outlineThickness;

        /// <summary>
        /// The current chosen font styles.
        /// </summary>
        public FontStyles fontStyles;

        /// <summary>
        /// The font size of the text.
        /// </summary>
        public float fontSize;

        /// <summary>
        /// Label in the configuration file for the text of a text.
        /// </summary>
        private const string TextLabel = "TextLabel";

        /// <summary>
        /// Label in the configuration file for the font color of a text.
        /// </summary>
        private const string FontColorLabel = "FontColorLabel";

        /// <summary>
        /// Label in the configuration file for the outline color of a text.
        /// </summary>
        private const string OutlineColorLabel = "OutlineColorLabel";

        /// <summary>
        /// Label in the configuration file for the outline color status.
        /// </summary>
        private const string OutlineStatusLabel = "OutlineStatusLabel";

        /// <summary>
        /// Label in the configuration file for the thickness of a text outline.
        /// </summary>
        private const string OutlineThicknessColorLabel = "OutlineThicknessColorLabel";

        /// <summary>
        /// Label in the configuration file for the font size of a text.
        /// </summary>
        private const string FontSizeLabel = "FontSizeLabel";

        /// <summary>
        /// Label in the configuration file for the font styles of a text.
        /// </summary>
        private const string FontStylesLabel = "FontStylesLabel";

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
                    id = textObject.name,
                    position = textObject.transform.localPosition,
                    eulerAngles = textObject.transform.localEulerAngles,
                    scale = textObject.transform.localScale,
                    text = tmp.text,
                    fontColor = tmp.color,
                    outlineColor = tmp.outlineColor,
                    outlineStatus = tmp.fontMaterial.IsKeywordEnabled(GameTexter.OutlineKeyWord),
                    outlineThickness = tmp.outlineWidth,
                    fontStyles = tmp.fontStyle,
                    fontSize = tmp.fontSize,
                    orderInLayer = textObject.GetComponent<OrderInLayerValueHolder>().GetOrderInLayer()
                };
            }

            return text;
        }

        /// <summary>
        /// This method clons the <see cref="TextConf"/> object.
        /// </summary>
        /// <returns>A new <see cref="TextConf"/> with the values of this object.</returns>
        public object Clone()
        {
            return new TextConf
            {
                id = this.id,
                position = this.position,
                eulerAngles = this.eulerAngles,
                scale = this.scale,
                text = this.text,
                fontColor = this.fontColor,
                outlineColor = this.outlineColor,
                outlineStatus = this.outlineStatus,
                outlineThickness = this.outlineThickness,
                fontStyles = this.fontStyles,
                fontSize = this.fontSize,
                orderInLayer = this.orderInLayer
            };
        }

        /// <summary>
        /// Writes this instances' attributes into the given <see cref="ConfigWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="ConfigWriter"/> to write the attributes into.</param>
        override internal void Save(ConfigWriter writer)
        {
            writer.BeginGroup();
            base.Save(writer);
            writer.Save(text, TextLabel);
            writer.Save(fontColor, FontColorLabel);
            writer.Save(outlineStatus, OutlineStatusLabel);
            writer.Save(outlineColor, OutlineColorLabel);
            writer.Save(outlineThickness, OutlineThicknessColorLabel);
            writer.Save(fontSize, FontSizeLabel);
            writer.Save(fontStyles.ToString(), FontStylesLabel);
            writer.EndGroup();
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

            /// Try to restores the text.
            if (attributes.TryGetValue(TextLabel, out object txt))
            {
                text = (string)txt;
            }
            else
            {
                errors = true;
            }

            /// Try to restores the font color.
            Color loadedFontColor = Color.black;
            if (ConfigIO.Restore(attributes, FontColorLabel, ref loadedFontColor))
            {
                fontColor = loadedFontColor;
            }
            else
            {
                fontColor = Color.black;
                errors = true;
            }

            /// Try to restores the outline color.
            Color loadedOutlineColor = Color.clear;
            if (ConfigIO.Restore(attributes, OutlineColorLabel, ref loadedOutlineColor))
            {
                outlineColor = loadedOutlineColor;
            }
            else
            {
                outlineColor = Color.clear;
                errors = true;
            }

            /// Try to restores the outline status.
            if (attributes.TryGetValue(OutlineStatusLabel, out object status))
            {
                outlineStatus = (bool)status;
            }
            else
            {
                outlineStatus = false;
                errors = true;
            }

            /// Try to restores the outline thickness.
            if (attributes.TryGetValue(OutlineThicknessColorLabel, out object thickness))
            {
                outlineThickness = (float)thickness;
            }
            else
            {
                outlineThickness = 0.4f;
                errors = true;
            }

            /// Try to restores the font size.
            if (attributes.TryGetValue(FontSizeLabel, out object size))
            {
                fontSize = (float)size;
            }
            else
            {
                fontSize = 0.5f;
                errors = true;
            }

            /// Try to restores the font styles.
            if (attributes.TryGetValue(FontStylesLabel, out object styles) 
                && Enum.TryParse<FontStyles>((string)styles, out FontStyles result))
            {
                fontStyles = result;
            }
            else
            {
                fontStyles = FontStyles.Normal;
                errors = true;
            }

            return !errors;
        }


    }
}