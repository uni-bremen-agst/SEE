using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable.ValueHolders;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TextConf = SEE.Game.Drawable.Configurations.TextConf;

namespace SEE.Game.Drawable
{
    /// <summary>
    /// This class manages the editing of a <see cref="DrawableType"/>.
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
        /// This method changes the primary color of a drawbale type.
        /// </summary>
        /// <param name="obj">The drawable type whose color should be changed.</param>
        /// <param name="color">The new color.</param>
        public static void ChangePrimaryColor(GameObject obj, Color color)
        {
            if (obj.CompareTag(Tags.Line))
            {
                LineRenderer renderer = obj.GetComponent<LineRenderer>();
                switch (obj.GetComponent<LineValueHolder>().GetColorKind())
                {
                    case GameDrawer.ColorKind.Monochrome:
                        renderer.startColor = renderer.endColor = Color.white;
                        renderer.material.color = color;
                        break;
                    case GameDrawer.ColorKind.Gradient:
                        renderer.material.color = Color.white;
                        renderer.startColor = color;
                        break;
                    case GameDrawer.ColorKind.TwoDashed:
                        renderer.material.color = color;
                        break;
                }
            }
        }

        /// <summary>
        /// This method changes the secondary color of a drawbale type.
        /// </summary>
        /// <param name="obj">The drawable type whose color should be changed.</param>
        /// <param name="color">The new color.</param>
        public static void ChangeSecondaryColor(GameObject obj, Color color)
        {
            if (obj.CompareTag(Tags.Line))
            {
                LineRenderer renderer = obj.GetComponent<LineRenderer>();
                switch (obj.GetComponent<LineValueHolder>().GetColorKind())
                {
                    case GameDrawer.ColorKind.Gradient:
                        renderer.material.color = Color.white;
                        renderer.endColor = color;
                        break;
                    case GameDrawer.ColorKind.TwoDashed:
                        renderer.materials[1].color = color;
                        break;
                    case GameDrawer.ColorKind.Monochrome:
                        renderer.startColor = renderer.endColor = Color.white;
                        break;
                }
            }
        }

        /// <summary>
        /// This method changes all editable values of a line at once.
        /// </summary>
        /// <param name="lineObj">The line whose values should be changed.</param>
        /// <param name="line">Contains the new values.</param>
        public static void ChangeLine(GameObject lineObj, LineConf line)
        {
            if (lineObj.CompareTag(Tags.Line))
            {
                ChangeThickness(lineObj, line.thickness);
                ChangeLayer(lineObj, line.orderInLayer);
                GameDrawer.ChangeColorKind(lineObj, line.colorKind, line);
                ChangePrimaryColor(lineObj, line.primaryColor);
                ChangeSecondaryColor(lineObj, line.secondaryColor);
                ChangeLoop(lineObj, line.loop);
                GameDrawer.ChangeLineKind(lineObj, line.lineKind, line.tiling);
            }
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
                int oldLayer = obj.GetComponent<OrderInLayerValueHolder>().GetOrderInLayer();
                if (newLayer - oldLayer > 0)
                {
                    GameLayerChanger.Increase(obj, newLayer, false);
                }
                else
                {
                    GameLayerChanger.Decrease(obj, newLayer, false);
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
            if (textObj.CompareTag(Tags.DText))
            {
                ChangeText(textObj, text.text);
                ChangeFontSize(textObj, text.fontSize);
                ChangeLayer(textObj, text.orderInLayer);
                ChangeFontStyles(textObj, text.fontStyles);
                ChangeFontColor(textObj, text.fontColor);
                GameTexter.ChangeOutlineStatus(textObj, text.outlineStatus);
                ChangeOutlineColor(textObj, text.outlineColor);
                ChangeOutlineThickness(textObj, text.outlineThickness);
                textObj.GetComponent<TextMeshPro>().ForceMeshUpdate(true);
                GameTexter.RefreshMeshCollider(textObj);
            }
        }

        /// <summary>
        /// This method changes the text of a drawable text.
        /// If the text object is a part of a mind map node, the border will refreshed.
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

            if (textObj.transform.parent.CompareTag(Tags.MindMapNode))
            {
                GameObject node = textObj.transform.parent.gameObject;
                GameMindMap.DisableTextAndBorderCollider(node);
                GameMindMap.ReDrawBorder(node);
            }
        }

        /// <summary>
        /// This method changes the font size of a text.
        /// If the text object is a part of a mind map node, the border will refreshed.
        /// </summary>
        /// <param name="textObj">The textObj whose text should be changed.</param>
        /// <param name="fontSize">The new font size.</param>
        public static void ChangeFontSize(GameObject textObj, float fontSize)
        {
            if (textObj.CompareTag(Tags.DText))
            {
                TextMeshPro tmp = textObj.GetComponent<TextMeshPro>();
                tmp.fontSize = fontSize;
                tmp.rectTransform.sizeDelta = GameTexter.CalculateWidthAndHeight(tmp.text, tmp.font,
                    fontSize, tmp.fontStyle);
                textObj.GetComponent<TextMeshPro>().ForceMeshUpdate(true);
                GameTexter.RefreshMeshCollider(textObj);
            }

            if (textObj.transform.parent.CompareTag(Tags.MindMapNode))
            {
                GameObject node = textObj.transform.parent.gameObject;
                GameMindMap.DisableTextAndBorderCollider(node);
                GameMindMap.ReDrawBorder(node);
            }
        }

        /// <summary>
        /// This method changes the font style of a text.
        /// If the text object is a part of a mind map node, the border will refreshed.
        /// </summary>
        /// <param name="textObj">The textObj whose text should be changed.</param>
        /// <param name="styles">The new font style.</param>
        public static void ChangeFontStyles(GameObject textObj, FontStyles styles)
        {
            if (textObj.CompareTag(Tags.DText))
            {
                TextMeshPro tmp = textObj.GetComponent<TextMeshPro>();
                tmp.fontStyle = styles;
                tmp.rectTransform.sizeDelta = GameTexter.CalculateWidthAndHeight(tmp.text, tmp.font,
                    tmp.fontSize, tmp.fontStyle);
                textObj.GetComponent<TextMeshPro>().ForceMeshUpdate(true);
                GameTexter.RefreshMeshCollider(textObj);
            }

            if (textObj.transform.parent.CompareTag(Tags.MindMapNode))
            {
                GameObject node = textObj.transform.parent.gameObject;
                GameMindMap.DisableTextAndBorderCollider(node);
                GameMindMap.ReDrawBorder(node);
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

        /// <summary>
        /// This method changes the image color of a image.
        /// </summary>
        /// <param name="imageObj">The image object whose image should be changed.</param>
        /// <param name="color">The new color for the image</param>
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
                ChangeLayer(imageObj, conf.orderInLayer);
                ChangeImageColor(imageObj, conf.imageColor);
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
                ChangeLine(GameFinder.FindChildWithTag(node, Tags.Line), conf.borderConf);
                ChangeText(GameFinder.FindChildWithTag(node, Tags.DText), conf.textConf);
                GameObject attachedObjects = GameFinder.GetAttachedObjectsObject(
                        GameFinder.GetDrawable(node));
                GameObject parent = GameFinder.FindChild(attachedObjects, conf.parentNode);
                GameMindMap.ChangeNodeKind(node, conf.nodeKind, conf.borderConf);
                GameMindMap.ChangeParent(node, parent);

                GameFinder.FindChildWithTag(node, Tags.Line).GetComponent<MeshCollider>().enabled = false;
                GameFinder.FindChildWithTag(node, Tags.DText).GetComponent<MeshCollider>().enabled = false;
                if (conf.branchLineToParent != "")
                {
                    GameObject branch = GameFinder.FindChild(attachedObjects, conf.branchLineToParent);
                    ChangeLine(branch, conf.branchLineConf);
                    branch.GetComponent<MeshCollider>().enabled = false;
                }
            }
        }
    }
}