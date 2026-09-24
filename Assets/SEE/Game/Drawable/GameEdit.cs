using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable.Editing;
using TMPro;
using UnityEngine;
using TextConf = SEE.Game.Drawable.Configurations.TextConf;

namespace SEE.Game.Drawable
{
    /// <summary>
    /// This class manages the editing of a <see cref="DrawableType"/>.
    /// </summary>
    public static class GameEdit
    {
        /// <summary>
        /// This method changes all editable values of a drawable text at once.
        /// </summary>
        /// <param name="textObj">The text whose values should be changed.</param>
        /// <param name="text">Contains the new values.</param>
        public static void ChangeText(GameObject textObj, TextConf text)
        {
            GameTextEdit.ChangeText(textObj, text);
        }

        /// <summary>
        /// This method changes the text of a drawable text.
        /// If the text object is a part of a mind map node, the border will refreshed.
        /// </summary>
        /// <param name="textObj">The object whose text should be changed.</param>
        /// <param name="text">The new text.</param>
        public static void ChangeText(GameObject textObj, string text)
        {
            GameTextEdit.ChangeText(textObj, text);
        }

        /// <summary>
        /// This method changes the font size of a text.
        /// If the text object is a part of a mind map node, the border will be refreshed.
        /// </summary>
        /// <param name="textObj">The object whose text should be changed.</param>
        /// <param name="fontSize">The new font size.</param>
        public static void ChangeFontSize(GameObject textObj, float fontSize)
        {
            GameTextEdit.ChangeFontSize(textObj, fontSize);
        }

        /// <summary>
        /// This method changes the font style of a text.
        /// If the text object is a part of a mind map node, the border will refreshed.
        /// </summary>
        /// <param name="textObj">The object whose text should be changed.</param>
        /// <param name="styles">The new font style.</param>
        public static void ChangeFontStyles(GameObject textObj, FontStyles styles)
        {
            GameTextEdit.ChangeFontStyles(textObj, styles);
        }

        /// <summary>
        /// This method changes the font color of a text.
        /// </summary>
        /// <param name="textObj">The object whose text should be changed.</param>
        /// <param name="color">The new font color.</param>
        public static void ChangeFontColor(GameObject textObj, Color color)
        {
            GameTextEdit.ChangeFontColor(textObj, color);
        }

        /// <summary>
        /// This method changes the outline color of a text.
        /// </summary>
        /// <param name="textObj">The object whose text should be changed.</param>
        /// <param name="color">The new outline color.</param>
        public static void ChangeOutlineColor(GameObject textObj, Color color)
        {
            GameTextEdit.ChangeOutlineColor(textObj, color);
        }

        /// <summary>
        /// This method changes the outline thickness of a text.
        /// </summary>
        /// <param name="textObj">The object whose text should be changed.</param>
        /// <param name="thickness">The new outline thickness.</param>
        public static void ChangeOutlineThickness(GameObject textObj, float thickness)
        {
            GameTextEdit.ChangeOutlineThickness(textObj, thickness);
        }

        /// <summary>
        /// This method changes the image color of a image.
        /// </summary>
        /// <param name="imageObj">The image object whose image should be changed.</param>
        /// <param name="color">The new color for the image.</param>
        public static void ChangeImageColor(GameObject imageObj, Color color)
        {
            GameImageEdit.ChangeImageColor(imageObj, color);
        }

        /// <summary>
        /// This method changes all editable values of a drawable image at once.
        /// </summary>
        /// <param name="imageObj">The image object whose values should be changed.</param>
        /// <param name="conf">The configuration which holds the necessary values.</param>
        public static void ChangeImage(GameObject imageObj, ImageConf conf)
        {
            GameImageEdit.ChangeImage(imageObj, conf);
        }

        /// <summary>
        /// This method changes all editable values of a drawable mind map node at once.
        /// </summary>
        /// <param name="node">The node object whose values should be changed.</param>
        /// <param name="conf">The configuration which holds the necessary values.</param>
        public static void ChangeMindMapNode(GameObject node, MindMapNodeConf conf)
        {
            GameMindMapEdit.ChangeMindMapNode(node, conf);
        }
    }
}