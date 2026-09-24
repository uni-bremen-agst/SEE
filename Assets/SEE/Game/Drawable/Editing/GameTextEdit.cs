using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable.MindMap;
using TMPro;
using UnityEngine;

namespace SEE.Game.Drawable.Editing
{
    /// <summary>
    /// Provides editing operations for drawable text objects.
    /// </summary>
    internal static class GameTextEdit
    {
        /// <summary>
        /// Changes all editable values of a drawable text.
        /// </summary>
        /// <param name="textObj">The text whose values should be changed.</param>
        /// <param name="text">Contains the new values.</param>
        internal static void ChangeText(GameObject textObj, TextConf text)
        {
            if (textObj.CompareTag(Tags.DText))
            {
                ChangeText(textObj, text.Text);
                ChangeFontSize(textObj, text.FontSize);
                GameEdit.ChangeLayer(textObj, text.OrderInLayer);
                ChangeFontStyles(textObj, text.FontStyles);
                ChangeFontColor(textObj, text.FontColor);
                GameTexter.ChangeOutlineStatus(textObj, text.IsOutlined);
                ChangeOutlineColor(textObj, text.OutlineColor);
                ChangeOutlineThickness(textObj, text.OutlineThickness);
                textObj.GetComponent<TextMeshPro>().ForceMeshUpdate(true);
                GameTexter.RefreshMeshCollider(textObj);
            }
        }

        /// <summary>
        /// Changes the text of a drawable text.
        /// If the text object is part of a Mind Map node, its border is refreshed.
        /// </summary>
        /// <param name="textObj">The object whose text should be changed.</param>
        /// <param name="text">The new text.</param>
        internal static void ChangeText(GameObject textObj, string text)
        {
            if (textObj.CompareTag(Tags.DText))
            {
                TextMeshPro textMesh = textObj.GetComponent<TextMeshPro>();
                textMesh.text = text;
                textMesh.ForceMeshUpdate();
                GameTexter.RefreshMeshCollider(textObj);
            }

            RefreshMindMapNode(textObj);
        }

        /// <summary>
        /// Changes the font size of a text.
        /// If the text object is part of a Mind Map node, its border is refreshed.
        /// </summary>
        /// <param name="textObj">The object whose font size should be changed.</param>
        /// <param name="fontSize">The new font size.</param>
        internal static void ChangeFontSize(GameObject textObj, float fontSize)
        {
            if (textObj.CompareTag(Tags.DText))
            {
                TextMeshPro textMesh = textObj.GetComponent<TextMeshPro>();
                textMesh.fontSize = fontSize;
                textMesh.rectTransform.sizeDelta =
                    GameTexter.CalculateWidthAndHeight(
                        textMesh.text,
                        textMesh.font,
                        fontSize,
                        textMesh.fontStyle);

                textMesh.ForceMeshUpdate(true);
                GameTexter.RefreshMeshCollider(textObj);
            }

            RefreshMindMapNode(textObj);
        }

        /// <summary>
        /// Changes the font style of a text.
        /// If the text object is part of a Mind Map node, its border is refreshed.
        /// </summary>
        /// <param name="textObj">The object whose font style should be changed.</param>
        /// <param name="styles">The new font style.</param>
        internal static void ChangeFontStyles(GameObject textObj, FontStyles styles)
        {
            if (textObj.CompareTag(Tags.DText))
            {
                TextMeshPro textMesh = textObj.GetComponent<TextMeshPro>();
                textMesh.fontStyle = styles;
                textMesh.rectTransform.sizeDelta =
                    GameTexter.CalculateWidthAndHeight(
                        textMesh.text,
                        textMesh.font,
                        textMesh.fontSize,
                        textMesh.fontStyle);

                textMesh.ForceMeshUpdate(true);
                GameTexter.RefreshMeshCollider(textObj);
            }

            RefreshMindMapNode(textObj);
        }

        /// <summary>
        /// Changes the font color of a text.
        /// </summary>
        /// <param name="textObj">The object whose font color should be changed.</param>
        /// <param name="color">The new font color.</param>
        internal static void ChangeFontColor(GameObject textObj, Color color)
        {
            if (textObj.CompareTag(Tags.DText))
            {
                TextMeshPro textMesh = textObj.GetComponent<TextMeshPro>();
                textMesh.color = color;
                textMesh.faceColor = color;
            }
        }

        /// <summary>
        /// Changes the outline color of a text.
        /// </summary>
        /// <param name="textObj">The object whose outline color should be changed.</param>
        /// <param name="color">The new outline color.</param>
        internal static void ChangeOutlineColor(GameObject textObj, Color color)
        {
            if (textObj.CompareTag(Tags.DText))
            {
                textObj.GetComponent<TextMeshPro>().outlineColor = color;
            }
        }

        /// <summary>
        /// Changes the outline thickness of a text.
        /// </summary>
        /// <param name="textObj">The object whose outline thickness should be changed.</param>
        /// <param name="thickness">The new outline thickness.</param>
        internal static void ChangeOutlineThickness(GameObject textObj, float thickness)
        {
            if (textObj.CompareTag(Tags.DText))
            {
                TextMeshPro textMesh = textObj.GetComponent<TextMeshPro>();
                textMesh.outlineWidth = thickness;
                textMesh.ForceMeshUpdate(true);
                GameTexter.RefreshMeshCollider(textObj);
            }
        }

        /// <summary>
        /// Refreshes the owning Mind Map node after a size-relevant text change.
        /// </summary>
        /// <param name="textObj">The text object that may belong to a Mind Map node.</param>
        private static void RefreshMindMapNode(GameObject textObj)
        {
            if (textObj.transform.parent.CompareTag(Tags.MindMapNode))
            {
                GameObject node = textObj.transform.parent.gameObject;
                GameMindMapNode.DisableTextAndBorderCollider(node);
                GameMindMap.ReDrawBorder(node);
            }
        }
    }
}
