using SEE.Game.Drawable.ActionHelpers;
using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable.Line;
using SEE.Game.Drawable.ValueHolders;
using SEE.GO;
using SEE.Utils;
using System.Linq;
using TMPro;
using UnityEngine;

namespace SEE.Game.Drawable.MindMap
{
    /// <summary>
    /// Provides creation, recreation, appearance, and collider handling
    /// for individual Mind Map nodes.
    /// </summary>
    public static class GameMindMapNode
    {
        /// <summary>
        /// Sets up a Mind Map node including its text, border, collider, and value holders.
        /// </summary>
        /// <param name="surface">The drawable surface on which the node should be displayed.</param>
        /// <param name="name">The ID of the node.</param>
        /// <param name="prefix">The ID prefix.</param>
        /// <param name="writtenText">The displayed text of the node.</param>
        /// <param name="position">The position for the node.</param>
        /// <param name="associatedPage">The associated surface page for this object.</param>
        /// <param name="node">The created node.</param>
        private static void Setup(GameObject surface, string name, string prefix, string writtenText,
            Vector3 position, int associatedPage, out GameObject node)
        {
            if (name.Length > prefix.Length)
            {
                node = new(name);
            }
            else
            {
                node = new("");

                name = prefix + node.GetInstanceID() + RandomStrings.GetRandomString(4);
                while (GameFinder.FindAttachedOrLocalDescendant(surface, name) != null)
                {
                    name = prefix + node.GetInstanceID() + RandomStrings.GetRandomString(4);
                }
                node.name = name;
            }

            DrawableSetupManager.Setup(surface, out GameObject _, out GameObject attachedObjects);

            node.tag = Tags.MindMapNode;

            node.transform.SetParent(attachedObjects.transform);
            node.transform.rotation = attachedObjects.transform.rotation;
            node.transform.position = position;

            GameObject text = CreateText(surface, position, writtenText, prefix);
            GameObject border = CreateMindMapBorder(surface, position, text, prefix);

            text.transform.SetParent(node.transform);
            border.transform.SetParent(node.transform);

            node.transform.position = position - node.transform.forward * ValueHolder.DistanceToDrawable.z *
                            border.GetComponent<OrderInLayerValueHolder>().OrderInLayer;
            node.AddComponent<OrderInLayerValueHolder>()
                .OrderInLayer = border.GetComponent<OrderInLayerValueHolder>().OrderInLayer;

            border.transform.localPosition = Vector3.zero;
            border.GetComponent<OrderInLayerValueHolder>().OrderInLayer = 0;

            // TextMeshPro does not inherit the order in layer from the node.
            text.GetComponent<TextMeshPro>().sortingOrder =
                node.GetComponent<OrderInLayerValueHolder>().OrderInLayer;

            // Text and border must only be editable through the Mind Map node.
            text.GetComponent<MeshCollider>().enabled = false;
            border.GetComponent<MeshCollider>().enabled = false;

            BoxCollider box = node.AddComponent<BoxCollider>();
            box.size = GetBoxSize(border);

            node.AddComponent<MMNodeValueHolder>();
            node.AddComponent<AssociatedPageHolder>().AssociatedPage = associatedPage;
        }

        /// <summary>
        /// Creates the text for a Mind Map node.
        /// </summary>
        /// <param name="surface">The drawable surface on which the node should be displayed.</param>
        /// <param name="position">The position for the text.</param>
        /// <param name="writtenText">The displayed text of the node.</param>
        /// <param name="prefix">The ID prefix determining the initial text appearance.</param>
        /// <returns>The created text.</returns>
        private static GameObject CreateText(GameObject surface, Vector3 position, string writtenText, string prefix)
        {
            FontStyles fontStyles = FontStyles.Normal;
            float fontSize = 0.7f;

            if (prefix == ValueHolder.MindMapThemePrefix)
            {
                fontStyles = FontStyles.Bold | FontStyles.Underline;
                fontSize = 1f;
            }
            else if (prefix == ValueHolder.MindMapLeafPrefix)
            {
                fontSize = 0.5f;
            }

            GameObject text = GameTexter.WriteText(surface, writtenText, position, Color.black, Color.clear, false,
                ValueHolder.StandardTextOutlineThickness, fontSize, 0, fontStyles);

            return text;
        }

        /// <summary>
        /// Creates the border of a Mind Map node.
        /// Themes receive an ellipse, subthemes a rectangle, and leaves an invisible ellipse.
        /// </summary>
        /// <param name="surface">The drawable surface on which the node should be displayed.</param>
        /// <param name="position">The position for the border.</param>
        /// <param name="text">The text object used to determine the border dimensions.</param>
        /// <param name="prefix">The ID prefix determining the border appearance.</param>
        /// <returns>The created border.</returns>
        private static GameObject CreateMindMapBorder(GameObject surface, Vector3 position, GameObject text,
            string prefix)
        {
            GameObject shape;
            LineKind lineKind = LineKind.Solid;
            Color lineColor = Color.black;
            bool ellipse = false;

            switch (prefix)
            {
                case ValueHolder.MindMapThemePrefix:
                    ellipse = true;
                    break;
                case ValueHolder.MindMapLeafPrefix:
                    lineKind = LineKind.Dashed;
                    lineColor = Color.clear;
                    ellipse = true;
                    break;
            }

            Vector3 convertedHitPoint = GameLineGeometry.GetConvertedPosition(surface, position);
            Vector3[] positions = GetBorderPositions(ellipse, convertedHitPoint, text);

            shape = GameLineDrawer.DrawLine(surface, "", positions, ColorKind.Monochrome,
                        lineColor, ValueHolder.CurrentSecondaryColor, ValueHolder.StandardLineThickness, true,
                        lineKind, ValueHolder.StandardLineTiling, increaseCurrentOrder: false);

            shape = GameLineGeometry.SetPivotShape(shape, convertedHitPoint);
            return shape;
        }

        /// <summary>
        /// Calculates the positions forming the border of a Mind Map node.
        /// </summary>
        /// <param name="ellipse">Whether an elliptical border should be created.</param>
        /// <param name="position">The position of the border.</param>
        /// <param name="text">The text object used to determine the border dimensions.</param>
        /// <returns>The calculated border positions.</returns>
        private static Vector3[] GetBorderPositions(bool ellipse, Vector3 position, GameObject text)
        {
            if (ellipse)
            {
                return ShapePointsCalculator.Ellipse(position,
                    text.GetComponent<RectTransform>().rect.width,
                    text.GetComponent<RectTransform>().rect.height);
            }
            else
            {
                return ShapePointsCalculator.MindMapRectangle(position,
                    text.GetComponent<RectTransform>().rect.width + 0.05f,
                    text.GetComponent<RectTransform>().rect.height + 0.05f);
            }
        }

        /// <summary>
        /// Redraws the border of the given Mind Map node and updates its collider.
        /// </summary>
        /// <param name="node">The node whose border should be redrawn.</param>
        internal static void UpdateBorder(GameObject node)
        {
            MMNodeValueHolder valueHolder = node.GetComponent<MMNodeValueHolder>();
            bool ellipse = valueHolder.NodeKind != MindMapNodeKind.Subtheme;
            GameObject nodeText = node.FindDescendantWithTag(Tags.DText);

            Vector3[] positions = GetBorderPositions(ellipse, Vector3.zero, nodeText);
            GameLineDrawer.Drawing(node.FindDescendantWithTag(Tags.Line), positions);

            ChangeBoxSize(node);
        }

        /// <summary>
        /// Calculates the box collider size for a Mind Map node based on its border.
        /// </summary>
        /// <param name="shape">The Mind Map node border.</param>
        /// <returns>The size of the box collider.</returns>
        private static Vector3 GetBoxSize(GameObject shape)
        {
            LineRenderer renderer = shape.GetComponent<LineRenderer>();
            Vector3[] rendererPos = new Vector3[renderer.positionCount];
            renderer.GetPositions(rendererPos);

            float[] xFloats = ConvertVector3ArrayToFloatArray(rendererPos, true);
            float x = Mathf.Abs(xFloats.Min()) + xFloats.Max() + 0.02f;

            float[] yFloats = ConvertVector3ArrayToFloatArray(rendererPos, false);
            float y = Mathf.Abs(yFloats.Min()) + yFloats.Max() + 0.01f;

            float z = Mathf.Abs(shape.transform.localPosition.z);
            return new Vector3(x, y, z);
        }

        /// <summary>
        /// Updates the box collider size of the given Mind Map node to match its border.
        /// </summary>
        /// <param name="node">The Mind Map node whose box collider should be updated.</param>
        public static void ChangeBoxSize(GameObject node)
        {
            if (node.CompareTag(Tags.MindMapNode))
            {
                BoxCollider box = node.GetComponent<BoxCollider>();
                GameObject border = node.FindDescendantWithTag(Tags.Line);
                box.size = GetBoxSize(border);
            }
        }

        /// <summary>
        /// Converts either the x or y coordinates of the given positions to a float array.
        /// </summary>
        /// <param name="positions">The positions whose coordinates should be converted.</param>
        /// <param name="xValue">
        /// Whether the x coordinates should be returned instead of the y coordinates.
        /// </param>
        /// <returns>The selected coordinates as a float array.</returns>
        private static float[] ConvertVector3ArrayToFloatArray(Vector3[] positions, bool xValue)
        {
            float[] arr = new float[positions.Length];

            if (xValue)
            {
                for (int i = 0; i < positions.Length; i++)
                {
                    arr[i] = positions[i].x;
                }
            }
            else
            {
                for (int i = 0; i < positions.Length; i++)
                {
                    arr[i] = positions[i].y;
                }
            }
            return arr;
        }

        /// <summary>
        /// Creates a Mind Map node.
        /// </summary>
        /// <param name="surface">The drawable surface on which the node should be displayed.</param>
        /// <param name="prefix">The ID prefix for the node.</param>
        /// <param name="writtenText">The displayed text of the node.</param>
        /// <param name="position">The position for the node.</param>
        /// <returns>The created node.</returns>
        public static GameObject Create(GameObject surface, string prefix, string writtenText, Vector3 position)
        {
            Setup(surface, "", prefix, writtenText, position,
                surface.GetComponent<DrawableHolder>().CurrentPage, out GameObject node);

            return node;
        }

        /// <summary>
        /// Applies the appearance associated with the given node kind to a Mind Map node.
        /// </summary>
        /// <param name="node">The node whose appearance should be changed.</param>
        /// <param name="newNodeKind">The node kind whose appearance should be applied.</param>
        /// <param name="borderConf">
        /// The previous border configuration that should be preserved when applicable.
        /// </param>
        internal static void ApplyNodeKindAppearance(GameObject node, MindMapNodeKind newNodeKind,
            LineConf borderConf = null)
        {
            GameObject nodeText = node.FindDescendantWithTag(Tags.DText);
            GameObject nodeBorder = node.FindDescendantWithTag(Tags.Line);

            bool ellipse = false;

            switch (newNodeKind)
            {
                case MindMapNodeKind.Theme:
                    ellipse = true;
                    GameEdit.ChangeFontStyles(nodeText, FontStyles.Bold | FontStyles.Underline);
                    GameEdit.ChangeFontSize(nodeText, 1.0f);
                    GameLineAppearance.ChangeLineKind(
                        nodeBorder, LineKind.Solid, ValueHolder.StandardLineTiling);
                    GameLineAppearance.ChangePrimaryColor(nodeBorder, Color.black);
                    GameLineAppearance.ChangeSecondaryColor(nodeBorder, Color.black);
                    break;

                case MindMapNodeKind.Subtheme:
                    GameEdit.ChangeFontStyles(nodeText, FontStyles.Normal);
                    GameEdit.ChangeFontSize(nodeText, 0.7f);
                    GameLineAppearance.ChangeLineKind(
                        nodeBorder, LineKind.Solid, ValueHolder.StandardLineTiling);
                    GameLineAppearance.ChangePrimaryColor(nodeBorder, Color.black);
                    GameLineAppearance.ChangeSecondaryColor(nodeBorder, Color.black);
                    break;

                case MindMapNodeKind.Leaf:
                    ellipse = true;
                    GameEdit.ChangeFontStyles(nodeText, FontStyles.Normal);
                    GameEdit.ChangeFontSize(nodeText, 0.5f);
                    GameLineAppearance.ChangeLineKind(
                        nodeBorder, LineKind.Dashed25, ValueHolder.StandardLineTiling);
                    GameLineAppearance.ChangePrimaryColor(nodeBorder, Color.clear);
                    GameLineAppearance.ChangeSecondaryColor(nodeBorder, Color.clear);
                    break;
            }

            ChangeName(node, newNodeKind);

            // Appearance changes may reactivate the child colliders.
            DisableTextAndBorderCollider(node);

            Vector3[] positions = GetBorderPositions(ellipse, Vector3.zero, nodeText);
            GameLineDrawer.Drawing(nodeBorder, positions);

            ChangeBoxSize(node);

            // Preserve an existing visible border appearance where the target kind permits it.
            if (newNodeKind != MindMapNodeKind.Leaf && borderConf != null
                && borderConf.PrimaryColor != Color.clear)
            {
                GameEdit.ChangeLine(nodeBorder, borderConf);
            }
        }

        /// <summary>
        /// Changes the prefix of the given Mind Map node to match its new node kind.
        /// </summary>
        /// <param name="node">The node whose prefix should be changed.</param>
        /// <param name="newNodeKind">The new node kind determining the prefix.</param>
        private static void ChangeName(GameObject node, MindMapNodeKind newNodeKind)
        {
            MindMapNodeKind oldNodeKind = node.GetComponent<MMNodeValueHolder>().NodeKind;
            node.name = node.name.Replace(
                GetPrefix(oldNodeKind),
                GetPrefix(newNodeKind));
        }

        /// <summary>
        /// Returns the ID prefix corresponding to the given Mind Map node kind.
        /// </summary>
        /// <param name="nodeKind">The node kind whose prefix should be returned.</param>
        /// <returns>The corresponding node prefix.</returns>
        internal static string GetPrefix(MindMapNodeKind nodeKind)
        {
            return nodeKind switch
            {
                MindMapNodeKind.Theme => ValueHolder.MindMapThemePrefix,
                MindMapNodeKind.Subtheme => ValueHolder.MindMapSubthemePrefix,
                MindMapNodeKind.Leaf => ValueHolder.MindMapLeafPrefix,
                _ => "",
            };
        }

        /// <summary>
        /// Disables the colliders of the text and border belonging to the given Mind Map node.
        /// </summary>
        /// <param name="node">The node whose child colliders should be disabled.</param>
        public static void DisableTextAndBorderCollider(GameObject node)
        {
            node.FindDescendantWithTag(Tags.Line).GetComponent<Collider>().enabled = false;
            node.FindDescendantWithTag(Tags.DText).GetComponent<Collider>().enabled = false;
        }

        /// <summary>
        /// Recreates a Mind Map node based on the given configuration.
        /// </summary>
        /// <param name="surface">The drawable surface on which the node should be displayed.</param>
        /// <param name="conf">The node configuration to restore.</param>
        /// <returns>The recreated Mind Map node.</returns>
        internal static GameObject Restore(GameObject surface, MindMapNodeConf conf)
        {
            DrawableHolder holder = surface.GetComponent<DrawableHolder>();
            if (conf.OrderInLayer >= holder.OrderInLayer
                && conf.AssociatedPage == holder.CurrentPage)
            {
                holder.OrderInLayer = conf.OrderInLayer + 1;
            }
            if (conf.AssociatedPage >= holder.MaxPageSize)
            {
                holder.MaxPageSize = conf.AssociatedPage + 1;
            }
            if (conf.OrderInLayer >= ValueHolder.MaxOrderInLayer)
            {
                ValueHolder.MaxOrderInLayer = conf.OrderInLayer + 1;
            }

            GameObject createdNode;

            if (GameFinder.FindAttachedOrLocalDescendant(surface, conf.ID) != null)
            {
                createdNode = GameFinder.FindAttachedOrLocalDescendant(surface, conf.ID);
            }
            else
            {
                Setup(surface, conf.ID, GetPrefix(conf.NodeKind), conf.TextConf.Text,
                    surface.transform.TransformPoint(conf.Position), conf.AssociatedPage,
                    out GameObject node);

                // Setup creates default children that are replaced by the restored configuration.
                Destroyer.Destroy(node.FindDescendantWithTag(Tags.Line));
                Destroyer.Destroy(node.FindDescendantWithTag(Tags.DText));

                createdNode = node;
            }

            GameObject border = GameLineDrawer.ReDrawLine(surface, conf.BorderConf);

            conf.TextConf.OrderInLayer = conf.OrderInLayer;
            GameObject text = GameTexter.ReWriteText(surface, conf.TextConf);
            text.GetComponent<OrderInLayerValueHolder>().OrderInLayer = 0;

            border.transform.SetParent(createdNode.transform);
            text.transform.SetParent(createdNode.transform);

            // Text and border are edited through their owning Mind Map node.
            text.GetComponent<MeshCollider>().enabled = false;
            border.GetComponent<MeshCollider>().enabled = false;

            border.transform.localPosition = Vector3.zero;
            text.transform.localPosition = Vector3.zero;

            BoxCollider box = createdNode.GetComponent<BoxCollider>();
            box.size = GetBoxSize(border);

            createdNode.transform.localScale = conf.Scale;
            createdNode.transform.localEulerAngles = conf.EulerAngles;
            createdNode.transform.localPosition = conf.Position;
            createdNode.GetComponent<OrderInLayerValueHolder>().OrderInLayer = conf.OrderInLayer;

            createdNode.GetComponent<AssociatedPageHolder>().AssociatedPage = conf.AssociatedPage;
            border.GetComponent<AssociatedPageHolder>().AssociatedPage = conf.AssociatedPage;
            text.GetComponent<AssociatedPageHolder>().AssociatedPage = conf.AssociatedPage;

            if (conf.AssociatedPage != surface.GetComponent<DrawableHolder>().CurrentPage)
            {
                createdNode.SetActive(false);
            }

            return createdNode;
        }
    }
}
