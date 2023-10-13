using RTG;
using SEE.Game;
using SEE.Game.Drawable.Configurations;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;

namespace Assets.SEE.Game.Drawable
{
    /// <summary>
    /// This class manages the editing of a drawable type.
    /// </summary>
    public static class GameEdit
    {
        /// <summary>
        /// This method changes the thickness of a line.
        /// </summary>
        /// <param name="line">The line whose thickness should be changed.</param>
        /// <param name="thickness">The new thickness.</param>
        public static void ChangeThickness(GameObject line, float thickness)
        {
            if (line.CompareTag(Tags.Line))
            {
                LineRenderer renderer = line.GetComponent<LineRenderer>();
                renderer.startWidth = thickness;
                renderer.endWidth = thickness;
                GameDrawer.RefreshCollider(line);
            }
        }

        /// <summary>
        /// This method changes the loop state of a line.
        /// </summary>
        /// <param name="line">The line whose loop should be changed.</param>
        /// <param name="loop">The new loop state.</param>
        public static void ChangeLoop(GameObject line, bool loop)
        {
            if (line.CompareTag(Tags.Line))
            {
                LineRenderer renderer = line.GetComponent<LineRenderer>();
                renderer.loop = loop;
                GameDrawer.RefreshCollider(line);
            }
        }

        /// <summary>
        /// This method changes the color of a drawbale type.
        /// </summary>
        /// <param name="obj">The drawable type whose color should be changed.</param>
        /// <param name="color">The new color.</param>
        public static void ChangeColor(GameObject obj, Color color)
        {
            if (obj.CompareTag(Tags.Line))
            {
                LineRenderer renderer = obj.GetComponent<LineRenderer>();
                renderer.material.color = color;
            }
        }

        /// <summary>
        /// This method changes all editable values of a line at once.
        /// </summary>
        /// <param name="line">The line whose values should be changed.</param>
        /// <param name="line">Contains the new values.</param>
        public static void ChangeLine(GameObject lineObj, Line line)
        {
            ChangeThickness(lineObj, line.thickness);
            ChangeLayer(lineObj, line.orderInLayer);
            ChangeColor(lineObj, line.color);
            ChangeLoop(lineObj, line.loop);
            GameDrawer.ChangeLineKind(lineObj, line.lineKind, line.tiling);
        }

        /// <summary>
        /// This method changes the order in layer of a <see cref="DrawableType"/>.
        /// </summary>
        /// <param name="obj">The <see cref="DrawableType"/> whose color should be changed.</param>
        /// <param name="layer">The new order in layer.</param>
        public static void ChangeLayer(GameObject obj, int layer)
        {
            if (Tags.DrawableTypes.Contains(obj.tag))
            {
                obj.GetComponent<OrderInLayerValueHolder>().SetOrderInLayer(layer);
                if (obj.CompareTag(Tags.DText))
                {
                    obj.GetComponent<TextMeshPro>().sortingOrder = layer;
                }
            }
        }

        /// <summary>
        /// This method changes all editable values of a drawable text at once.
        /// </summary>
        /// <param name="textObj">The text whose values should be changed.</param>
        /// <param name="text">Contains the new values.</param>
        public static void ChangeText(GameObject textObj, Text text)
        {
            ChangeText(textObj, text.text);
            ChangeFontSize(textObj, text.fontSize);
            ChangeLayer(textObj, text.orderInLayer);
            ChangeFontStyles(textObj, text.fontStyles);
            ChangeFontColor(textObj, text.fontColor);
            ChangeOutlineColor(textObj, text.outlineColor);
            ChangeOutlineThickness(textObj, text.outlineThickness);
            textObj.GetComponent<TextMeshPro>().ForceMeshUpdate(true);
            GameTexter.RefreshMeshCollider(textObj);
        }

        /// <summary>
        /// This method changes the text of a drawable text.
        /// </summary>
        /// <param name="textObj">The textObj whose text should be changed.</param>
        /// <param name="text">The new text.</param>
        public static void ChangeText(GameObject textObj, string text)
        {
            if (textObj.CompareTag(Tags.DText))
            {
                textObj.GetComponent<TextMeshPro>().text = text;
                textObj.GetComponent<TextMeshPro>().ForceMeshUpdate();
                GameTexter.RefreshMeshCollider(textObj);
            }
        }

        /// <summary>
        /// This method changes the font size of a text.
        /// </summary>
        /// <param name="textObj">The textObj whose text should be changed.</param>
        /// <param name="fontSize">The new font size.</param>
        public static void ChangeFontSize(GameObject textObj, float fontSize)
        {
            if (textObj.CompareTag(Tags.DText))
            {
                TextMeshPro tmp = textObj.GetComponent<TextMeshPro>();
                tmp.fontSize = fontSize;
                tmp.rectTransform.sizeDelta = GameTexter.CalculateWidthAndHeight(tmp.text, tmp.font, fontSize, tmp.fontStyle);
                textObj.GetComponent<TextMeshPro>().ForceMeshUpdate(true);
                GameTexter.RefreshMeshCollider(textObj);
            }
        }

        /// <summary>
        /// This method changes the font style of a text.
        /// </summary>
        /// <param name="textObj">The textObj whose text should be changed.</param>
        /// <param name="styles">The new font style.</param>
        public static void ChangeFontStyles(GameObject textObj, FontStyles styles)
        {
            if (textObj.CompareTag(Tags.DText))
            {
                TextMeshPro tmp = textObj.GetComponent<TextMeshPro>();
                tmp.fontStyle = styles;
                tmp.rectTransform.sizeDelta = GameTexter.CalculateWidthAndHeight(tmp.text, tmp.font, tmp.fontSize, tmp.fontStyle);
                textObj.GetComponent<TextMeshPro>().ForceMeshUpdate(true);
                GameTexter.RefreshMeshCollider(textObj);
            }
        }

        /// <summary>
        /// This method changes the font color of a text.
        /// </summary>
        /// <param name="textObj">The textObj whose text should be changed.</param>
        /// <param name="color">The new font color.</param>
        public static void ChangeFontColor(GameObject textObj, Color color)
        {
            if (textObj.CompareTag(Tags.DText))
            {
                textObj.GetComponent<TextMeshPro>().color = color;
                textObj.GetComponent<TextMeshPro>().faceColor = color;
            }
        }

        /// <summary>
        /// This method changes the outline color of a text.
        /// </summary>
        /// <param name="textObj">The textObj whose text should be changed.</param>
        /// <param name="color">The new outline color.</param>
        public static void ChangeOutlineColor(GameObject textObj, Color color)
        {
            if (textObj.CompareTag(Tags.DText))
            {
                textObj.GetComponent<TextMeshPro>().outlineColor = color;
            }
        }

        /// <summary>
        /// This method changes the outline thickness of a text.
        /// </summary>
        /// <param name="textObj">The textObj whose text should be changed.</param>
        /// <param name="thickness">The new outline thickness.</param>
        public static void ChangeOutlineThickness(GameObject textObj, float thickness)
        {
            if (textObj.CompareTag(Tags.DText))
            {
                textObj.GetComponent<TextMeshPro>().outlineWidth = thickness;
                textObj.GetComponent<TextMeshPro>().ForceMeshUpdate(true);
                GameTexter.RefreshMeshCollider(textObj);
            }
        }
    }
}