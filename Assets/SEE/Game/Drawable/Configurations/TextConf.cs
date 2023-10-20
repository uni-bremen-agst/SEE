using Assets.SEE.Game.Drawable;
using HSVPicker;
using SEE.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static SEE.Game.GameDrawer;

namespace SEE.Game.Drawable.Configurations
{
    /// <summary>
    /// The configuration class for a drawable text.
    /// </summary>
    [Serializable]
    public class TextConf : DrawableType, ICloneable
    {
        /// <summary>
        /// The position of the text.
        /// </summary>
        public Vector3 position;

        /// <summary>
        /// The euler angles of the text.
        /// </summary>
        public Vector3 eulerAngles;

        /// <summary>
        /// The scale of the text.
        /// </summary>
        public Vector3 scale;

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
        /// The order in layer for this drawable object.
        /// </summary>
        public int orderInLayer;

        /// <summary>
        /// Label in the configuration file for the id of a text.
        /// </summary>
        private const string IDLabel = "IDLabel";

        /// <summary>
        /// Label in the configuration file for the position of a text.
        /// </summary>
        private const string PositionLabel = "PositionLabel";

        /// <summary>
        /// Label in the configuration file for the euler angles of a text.
        /// </summary>
        private const string EulerAnglesLabel = "EulerAnglesLabel";

        /// <summary>
        /// Label in the configuration file for the scale of a text.
        /// </summary>
        private const string ScaleLabel = "ScaleLabel";

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
        /// Label in the configuration file for the order in layer of a text.
        /// </summary>
        private const string OrderInLayerLabel = "OrderInLayerLabel";

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
                text = new();
                text.id = textObject.name;
                text.position = textObject.transform.localPosition;
                text.eulerAngles = textObject.transform.localEulerAngles;
                text.scale = textObject.transform.localScale;
                TextMeshPro tmp = textObject.GetComponent<TextMeshPro>();
                text.text = tmp.text;
                text.fontColor = tmp.color;
                text.outlineColor = tmp.outlineColor;
                text.outlineThickness = tmp.outlineWidth;
                text.fontStyles = tmp.fontStyle;
                text.fontSize = tmp.fontSize;
                text.orderInLayer = textObject.GetComponent<OrderInLayerValueHolder>().GetOrderInLayer();
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
        internal void Save(ConfigWriter writer)
        {
            writer.BeginGroup();
            writer.Save(id, IDLabel);
            writer.Save(position, PositionLabel);
            writer.Save(eulerAngles, EulerAnglesLabel);
            writer.Save(scale, ScaleLabel);
            writer.Save(text, TextLabel);
            writer.Save(fontColor, FontColorLabel);
            writer.Save(outlineColor, OutlineColorLabel);
            writer.Save(outlineThickness, OutlineThicknessColorLabel);
            writer.Save(fontSize, FontSizeLabel);
            writer.Save(fontStyles.ToString(), FontStylesLabel);
            writer.Save(orderInLayer, OrderInLayerLabel);
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
        internal bool Restore(Dictionary<string, object> attributes)
        {
            bool errors = false;
            if (attributes.TryGetValue(IDLabel, out object name))
            {
                id = (string)name;
            }
            else
            {
                errors = true;
            }

            Vector3 loadedPosition = Vector3.zero;
            if (ConfigIO.Restore(attributes, PositionLabel, ref loadedPosition))
            {
                position = loadedPosition;
            }
            else
            {
                position = Vector3.zero;
                errors = true;
            }
            
            Vector3 loadedEulerAngles = Vector3.zero;
            if (ConfigIO.Restore(attributes, EulerAnglesLabel, ref loadedEulerAngles))
            {
                eulerAngles = loadedEulerAngles;
            }
            else
            {
                eulerAngles = Vector3.zero;
                errors = true;
            }

            Vector3 loadedScale = Vector3.zero;
            if (ConfigIO.Restore(attributes, ScaleLabel, ref loadedScale))
            {
                scale = loadedScale;
            }
            else
            {
                scale = Vector3.zero;
                errors = true;
            }

            if (attributes.TryGetValue(TextLabel, out object txt))
            {
                text = (string)txt;
            }
            else
            {
                errors = true;
            }

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

            if (attributes.TryGetValue(OutlineThicknessColorLabel, out object thickness))
            {
                outlineThickness = (float)thickness;
            }
            else
            {
                outlineThickness = 0.4f;
                errors = true;
            }

            if (attributes.TryGetValue(FontSizeLabel, out object size))
            {
                fontSize = (float)size;
            }
            else
            {
                fontSize = 0.5f;
                errors = true;
            }

            if (attributes.TryGetValue(FontStylesLabel, out object styles) && Enum.TryParse<FontStyles>((string)styles, out FontStyles result))
            {
                fontStyles = result;
            }
            else
            {
                fontStyles = FontStyles.Normal;
                errors = true;
            }

            if (!ConfigIO.Restore(attributes, OrderInLayerLabel, ref orderInLayer))
            {
                orderInLayer = ValueHolder.currentOrderInLayer;
                ValueHolder.currentOrderInLayer++;
                errors = true;
            }

            return !errors;
        }


    }
}