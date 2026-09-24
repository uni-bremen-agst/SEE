using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable.Editing;
using SEE.Game.Drawable.MindMap;
using SEE.Game.Drawable.ValueHolders;
using SEE.GO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static SEE.Game.Drawable.ActionHelpers.LineCapPointsCalculator;
using TextConf = SEE.Game.Drawable.Configurations.TextConf;

namespace SEE.Game.Drawable
{
    /// <summary>
    /// This class manages the editing of a <see cref="DrawableType"/>.
    /// </summary>
    public static class GameEdit
    {
        /// <summary>
        /// This method changes the thickness of a shape.
        /// </summary>
        /// <param name="shape">The shape whose thickness should be changed.</param>
        /// <param name="thickness">The new thickness.</param>
        public static void ChangeThickness(GameObject shape, float thickness)
        {
            GameLineEdit.ChangeThickness(shape, thickness);
        }

        /// <summary>
        /// This method changes the loop state of a line.
        /// </summary>
        /// <param name="line">The line whose loop should be changed.</param>
        /// <param name="loop">The new loop state.</param>
        public static void ChangeLoop(GameObject line, bool loop)
        {
            GameLineEdit.ChangeLoop(line, loop);
        }

        /// <summary>
        /// Changes the line caps of a line.
        /// </summary>
        /// <param name="line">The line whose line caps should be changed.</param>
        /// <param name="currentConf">The current line configuration.</param>
        /// <param name="start">The starting line cap.</param>
        /// <param name="end">The ending line cap.</param>
        public static void ChangeLineCaps(
            GameObject line,
            LineConf currentConf,
            LineCap start,
            LineCap end)
        {
            GameLineEdit.ChangeLineCaps(line, currentConf, start, end);
        }

        /// <summary>
        /// Changes the visual style of one line cap of the given line.
        /// If the thickness changes, the line caps are recreated so that the geometric
        /// size and the connection point of the cap are updated accordingly.
        /// </summary>
        /// <param name="line">The line whose line cap style should be changed.</param>
        /// <param name="isStartCap">
        /// True if the start cap should be changed, false if the end cap should be changed.
        /// </param>
        /// <param name="capConf">The new visual configuration of the line cap.</param>
        public static void ChangeLineCapStyle(
            GameObject line,
            bool isStartCap,
            LineCapConf capConf)
        {
            GameLineEdit.ChangeLineCapStyle(line, isStartCap, capConf);
        }

        /// <summary>
        /// This method changes all editable values of a line at once.
        /// </summary>
        /// <param name="lineObj">The line whose values should be changed.</param>
        /// <param name="line">Contains the new values.</param>
        public static void ChangeLine(GameObject lineObj, LineConf line)
        {
            GameLineEdit.ChangeLine(lineObj, line);
        }

        /// <summary>
        /// This method changes the order in layer of a <see cref="DrawableType"/>.
        /// </summary>
        /// <param name="obj">The <see cref="DrawableType"/> whose order should be changed.</param>
        /// <param name="newLayer">The new order in layer.</param>
        public static void ChangeLayer(GameObject obj, int newLayer)
        {
            if (Tags.DrawableTypes.Contains(obj.tag))
            {
                int oldLayer = obj.GetComponent<OrderInLayerValueHolder>().OrderInLayer;
                if (newLayer - oldLayer > 0)
                {
                    GameLayerChanger.ChangeOrderInLayer(obj, newLayer, GameLayerChanger.LayerChangerStates.Increase, false);
                }
                else
                {
                    GameLayerChanger.ChangeOrderInLayer(obj, newLayer, GameLayerChanger.LayerChangerStates.Decrease, false);
                }
            }
        }

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
            if (imageObj.CompareTag(Tags.Image))
            {
                imageObj.GetComponent<Image>().color = color;
            }
        }

        /// <summary>
        /// This method changes all editable values of a drawable image at once.
        /// </summary>
        /// <param name="imageObj">The image object whose values should be changed.</param>
        /// <param name="conf">The configuration which holds the necessary values.</param>
        public static void ChangeImage(GameObject imageObj, ImageConf conf)
        {
            if (imageObj.CompareTag(Tags.Image))
            {
                ChangeLayer(imageObj, conf.OrderInLayer);
                ChangeImageColor(imageObj, conf.ImageColor);
                GameMoveRotator.SetRotateY(imageObj, conf.EulerAngles.y);
            }
        }

        /// <summary>
        /// This method changes all editable values of a drawable mind map node at once.
        /// </summary>
        /// <param name="node">The node object whose values should be changed.</param>
        /// <param name="conf">The configuration which holds the necessary values.</param>
        public static void ChangeMindMapNode(GameObject node, MindMapNodeConf conf)
        {
            if (node.CompareTag(Tags.MindMapNode))
            {
                GameMindMap.ChangeNodeKind(node, conf.NodeKind, conf.BorderConf);
                ChangeLine(node.FindDescendantWithTag(Tags.Line), conf.BorderConf);
                ChangeText(node.FindDescendantWithTag(Tags.DText), conf.TextConf);
                GameObject attachedObjects = GameFinder.GetAttachedObjectsObject(
                        GameFinder.GetDrawableSurface(node));
                GameObject parent = GameFinder.FindAttachedOrLocalDescendant(attachedObjects, conf.ParentNode);
                GameMindMapBranch.ChangeParent(node, parent);

                GameMindMapNode.ChangeBoxSize(node);

                node.FindDescendantWithTag(Tags.Line).GetComponent<MeshCollider>().enabled = false;
                node.FindDescendantWithTag(Tags.DText).GetComponent<MeshCollider>().enabled = false;
                if (conf.BranchLineToParent != "")
                {
                    GameObject branch = GameFinder.FindAttachedOrLocalDescendant(attachedObjects, conf.BranchLineToParent);
                    ChangeLine(branch, conf.BranchLineConf);
                    branch.GetComponent<MeshCollider>().enabled = false;
                }
                ChangeLayer(node, conf.OrderInLayer);
            }
        }
    }
}