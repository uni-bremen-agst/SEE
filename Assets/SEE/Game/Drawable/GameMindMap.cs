using SEE.Game.Drawable.ActionHelpers;
using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable.ValueHolders;
using SEE.GO;
using SEE.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using static SEE.Game.Drawable.GameDrawer;

namespace SEE.Game.Drawable
{
    /// <summary>
    /// This class provides methods to create or recover mind map nodes.
    /// </summary>
    public static class GameMindMap
    {
        /// <summary>
        /// The different kinds of a mind map node.
        /// </summary>
        [Serializable]
        public enum NodeKind
        {
            Theme,
            Subtheme,
            Leaf
        }

        /// <summary>
        /// Returns the list of the different node kinds.
        /// </summary>
        /// <returns>A list of the node kinds.</returns>
        public static List<NodeKind> GetNodeKinds()
        {
            return Enum.GetValues(typeof(NodeKind)).Cast<NodeKind>().ToList();
        }

        /// <summary>
        /// Sets up a mind map node.
        /// It adds the <see cref="Tags.MindMapNode"/> to the node.
        /// It also creates the border and the text for the node and disables the collider of that.
        /// The box collider of the node will be calculated on the border size.
        /// Additionally, the node receives an <see cref="MMNodeValueHolder"/> component.
        /// </summary>
        /// <param name="surface">The drawable surface on which the node should be displayed.</param>
        /// <param name="name">The ID of the node.</param>
        /// <param name="prefix">The ID prefix.</param>
        /// <param name="writtenText">The displayed text of the node.</param>
        /// <param name="position">The position for the node.</param>
        /// <param name="associatedPage">The assoiated surface page for this object.</param>
        /// <param name="node">The created node.</param>
        private static void Setup(GameObject surface, string name, string prefix, string writtenText,
            Vector3 position, int associatedPage, out GameObject node)
        {
            /// If the object has been created earlier, it already has a name,
            /// and this name is taken from the parameters <paramref name="name"/>.
            if (name.Length > prefix.Length)
            {
                node = new(name);
            }
            else
            {
                /// Otherwise, a name for the node will be generated.
                /// For this, the node prefix <paramref name="prefix"/> is concatenated with
                /// the object ID along with a random string consisting of four characters.
                node = new("");

                name = prefix + node.GetInstanceID() + RandomStrings.GetRandomString(4);
                /// Check if the name is already in use. If so, generate a new name.
                while (GameFinder.FindChild(surface, name) != null)
                {
                    name = prefix + node.GetInstanceID() + RandomStrings.GetRandomString(4);
                }
                node.name = name;
            }

            /// Sets up the drawable holder <see cref="DrawableSetupManager"/>.
            DrawableSetupManager.Setup(surface, out GameObject _, out GameObject attachedObjects);

            /// Assign the mind map node tag to the node object.
            node.tag = Tags.MindMapNode;

            /// Add the node object to the hierarchy below the attached objects - object of the drawable.
            node.transform.SetParent(attachedObjects.transform);
            /// Adopt the rotation of the attached object.
            node.transform.rotation = attachedObjects.transform.rotation;
            /// Sets the node position to the hit point position.
            node.transform.position = position;

            /// Creates the text for the mind map node.
            GameObject text = CreateText(surface, position, writtenText, prefix);
            /// Creates the border for the mind map node.
            GameObject border = CreateMindMapBorder(surface, position, text, prefix);

            /// Sets the text and border as child of the node.
            text.transform.SetParent(node.transform);
            border.transform.SetParent(node.transform);

            /// Ensures adherence to the order in layer distance.
            node.transform.position = position - node.transform.forward * ValueHolder.DistanceToDrawable.z *
                            border.GetComponent<OrderInLayerValueHolder>().OrderInLayer;
            /// Sets the order in layer to a new created order in layer value holder for the node.
            node.AddComponent<OrderInLayerValueHolder>()
                .OrderInLayer = border.GetComponent<OrderInLayerValueHolder>().OrderInLayer;
            /// Sets the border to the middle point of the node. And sets his order to zero.
            border.transform.localPosition = Vector3.zero;
            border.GetComponent<OrderInLayerValueHolder>().OrderInLayer = 0;
            /// Sets the order to the <see cref="TextMeshPro"/>, as the text otherwise does not inherit the order.
            text.GetComponent<TextMeshPro>().sortingOrder =
                node.GetComponent<OrderInLayerValueHolder>().OrderInLayer;

            /// Disables the Mesh Collider for the text and border, because they should only be editable through the node.
            /// This prevents inappropriate line operations on the border, such as <see cref="LineSplitAction"/>.
            text.GetComponent<MeshCollider>().enabled = false;
            border.GetComponent<MeshCollider>().enabled = false;

            /// Adds a box collider to the node and calculates the size based on the borders.
            BoxCollider box = node.AddComponent<BoxCollider>();
            box.size = GetBoxSize(border);

            /// Adds a <see cref="MMNodeValueHolder"/> component.
            /// It is needed to manage necessary Mind Map Node data.
            node.AddComponent<MMNodeValueHolder>();

            /// Adds a <see cref="AssociatedPageHolder"/> component.
            /// And sets the associated page to the used page of the surface.
            node.AddComponent<AssociatedPageHolder>().AssociatedPage = associatedPage;
        }

        /// <summary>
        /// Creates the text for the mind map node (description).
        /// </summary>
        /// <param name="surface">The drawable surface on which the node should be displayed.</param>
        /// <param name="position">The position for the text.</param>
        /// <param name="writtenText">The text (description) for the node.</param>
        /// <param name="prefix">The ID prefix, necessary for the font size.</param>
        /// <returns>The created text.</returns>
        private static GameObject CreateText(GameObject surface, Vector3 position, string writtenText, string prefix)
        {
            /// Style for Subthemes and Leaves
            FontStyles fontStyles = FontStyles.Normal;
            /// Size for Subthemes
            float fontSize = 0.7f;

            /// For themes the text is bold and underlined.
            /// And the size is bigger.
            if (prefix == ValueHolder.MindMapThemePrefix)
            {
                fontStyles = FontStyles.Bold | FontStyles.Underline;
                fontSize = 1f;
            }
            else if (prefix == ValueHolder.MindMapLeafPrefix)
            {
                /// If the node is a Leaf it has a smaller font size.
                fontSize = 0.5f;
            }

            /// Create the text. The initial color is black. It can be changed with the <see cref="EditAction"/>.
            GameObject text = GameTexter.WriteText(surface, writtenText, position, Color.black, Color.clear, false,
                ValueHolder.StandardTextOutlineThickness, fontSize, 0, fontStyles);

            return text;
        }

        /// <summary>
        /// Creates the border of the mind map node.
        /// Themes receive an ellipse shape, sub-themes a rectangle, and leaves receive an invisible ellipse shape.
        /// </summary>
        /// <param name="surface">The drawable surface on which the node should be displayed.</param>
        /// <param name="position">The position for the border.</param>
        /// <param name="text">The text object, necessary for the width/height calculation.</param>
        /// <param name="prefix">The ID prefix.</param>
        /// <returns>The created border.</returns>
        private static GameObject CreateMindMapBorder(GameObject surface, Vector3 position, GameObject text, string prefix)
        {
            GameObject shape;
            /// Themes and Subthemes are solid.
            LineKind lineKind = LineKind.Solid;
            Color lineColor = Color.black;
            bool ellipse = false;
            /// If the node is a Theme or a Leaf, it will use an ellipse shape.
            /// Subthemes have a rectangle shape.
            switch (prefix)
            {
                case ValueHolder.MindMapThemePrefix:
                    ellipse = true;
                    break;
                case ValueHolder.MindMapLeafPrefix:
                    /// <see cref="NodeKind.Leaf"/> has an invisible dashed line.
                    lineKind = LineKind.Dashed;
                    lineColor = Color.clear;
                    ellipse = true;
                    break;
            }
            /// Convert the hit point to a local position of the drawable.
            Vector3 convertedHitPoint = GetConvertedPosition(surface, position);
            /// Gets the shape positions.
            Vector3[] positions = GetBorderPositions(ellipse, convertedHitPoint, text);
            /// Draws the border.
            shape = DrawLine(surface, "", positions, ColorKind.Monochrome,
                        lineColor, ValueHolder.CurrentSecondaryColor, ValueHolder.StandardLineThickness, true,
                        lineKind, ValueHolder.StandardLineTiling, increaseCurrentOrder: false);
            /// Sets the pivot to the middle.
            shape = SetPivotShape(shape, convertedHitPoint);
            return shape;
        }

        /// <summary>
        /// Calculates the border positions.
        /// </summary>
        /// <param name="ellipse">True, if the node is a theme or a leaf.</param>
        /// <param name="position">The position of the border.</param>
        /// <param name="text">The text object, necessary for the width/height calculation.</param>
        /// <returns>The calculated positions.</returns>
        private static Vector3[] GetBorderPositions(bool ellipse, Vector3 position, GameObject text)
        {
            /// If the node has the <see cref="NodeKind.Theme"/> or <see cref="NodeKind.Leaf"/>.
            if (ellipse)
            {
                return ShapePointsCalculator.Ellipse(position,
                    text.GetComponent<RectTransform>().rect.width,
                    text.GetComponent<RectTransform>().rect.height);
            }
            else
            {
                /// For the <see cref="NodeKind.Subtheme"/>.
                return ShapePointsCalculator.MindMapRectangle(position,
                    text.GetComponent<RectTransform>().rect.width + 0.05f,
                    text.GetComponent<RectTransform>().rect.height + 0.05f);
            }
        }

        /// <summary>
        /// Redraws the mind map node border.
        /// </summary>
        /// <param name="node">The node whose border should be redrawn.</param>
        public static void ReDrawBorder(GameObject node)
        {
            if (node.CompareTag(Tags.MindMapNode))
            {
                MMNodeValueHolder valueHolder = node.GetComponent<MMNodeValueHolder>();
                bool ellipse = valueHolder.NodeKind != NodeKind.Subtheme;
                GameObject nodeText = node.FindDescendantWithTag(Tags.DText);
                /// Gets the new border positions.
                Vector3[] positions = GetBorderPositions(ellipse, Vector3.zero, nodeText);
                /// Re-draws the border.
                Drawing(node.FindDescendantWithTag(Tags.Line), positions);
                /// Renew the size of the BoxCollider.
                ChangeBoxSize(node);
                /// Re-draws the branch lines, because changes to the border might necessitate adjustments.
                ReDrawBranchLines(node);
            }
        }

        /// <summary>
        /// Gets the box collider size for the mind map node.
        /// </summary>
        /// <param name="shape">The mind map border.</param>
        /// <returns>The Vector3 that represents the size for the box collider.</returns>
        private static Vector3 GetBoxSize(GameObject shape)
        {
            LineRenderer renderer = shape.GetComponent<LineRenderer>();
            Vector3[] rendererPos = new Vector3[renderer.positionCount];
            renderer.GetPositions(rendererPos);
            /// Calculates the x size.
            /// The 0.02 is added for the necessary distance.
            float[] xFloats = ConvertVector3ArrayToFloatArray(rendererPos, true);
            float x = Mathf.Abs(xFloats.Min()) + xFloats.Max() + 0.02f;

            /// Calculates the y size.
            /// The 0.01 is added for the necessary distance.
            float[] yFloats = ConvertVector3ArrayToFloatArray(rendererPos, false);
            float y = Mathf.Abs(yFloats.Min()) + yFloats.Max() + 0.01f;

            /// Calculates the z size.
            float z = Mathf.Abs(shape.transform.localPosition.z);
            return new Vector3(x, y, z);
        }

        /// <summary>
        /// Renew the size of the BoxCollider, adjusted to the border size.
        /// </summary>
        /// <param name="node">The mind map node.</param>
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
        /// Converts an axis of a Vector3 array to a float array.
        /// The z axis will be ignored because it is always zero.
        /// </summary>
        /// <param name="positions">The vector3 array which holds the positions.</param>
        /// <param name="xValue">True, if the x axis should be converted. Otherwise it will convert the y axis.</param>
        /// <returns>The float array with the chosen converted axis.</returns>
        private static float[] ConvertVector3ArrayToFloatArray(Vector3[] positions, bool xValue)
        {
            float[] arr = new float[positions.Length];
            /// True, if the x values should be converted.
            if (xValue)
            {
                /// Block for converting the x values.
                for (int i = 0; i < positions.Length; i++)
                {
                    arr[i] = positions[i].x;
                }
            }
            else
            {
                /// Block for converting the y values.
                for (int i = 0; i < positions.Length; i++)
                {
                    arr[i] = positions[i].y;
                }
            }
            return arr;
        }

        /// <summary>
        /// Creates a mind map node.
        /// </summary>
        /// <param name="surface">The drawable surface on which the node should be displayed.</param>
        /// <param name="prefix">The ID prefix for the node.</param>
        /// <param name="writtenText">The text (description) of the node.</param>
        /// <param name="position">The position for the node.</param>
        /// <returns>The created node.</returns>
        public static GameObject Create(GameObject surface, string prefix, string writtenText, Vector3 position)
        {
            Setup(surface, "", prefix, writtenText, position, surface.GetComponent<DrawableHolder>().CurrentPage, out GameObject node);
            return node;
        }

        /// <summary>
        /// Returns the ID of a drawable type name.
        /// </summary>
        /// <param name="name">The drawable type name from which the ID should be extracted.</param>
        /// <returns>The extracted ID.</returns>
        public static string GetIDofName(string name)
        {
            return name.Split('-')[1];
        }

        /// <summary>
        /// Creates/Recreates a branch line between the node and the parent.
        /// If the parameter name is not empty, an attempt is made to redraw the branch line.
        /// The order of the branch line is a sequence lower than the lower sequence of both nodes (node/parent).
        /// The mesh collider of the branch line will be deactivated.
        /// In the <see cref="MMNodeValueHolder"/> component of the node,
        /// the branch line is added as the parent branch line and the parent as parent.
        /// The <see cref="MMNodeValueHolder"/> component of the parent, adds the node and the branch line as children.
        /// </summary>
        /// <param name="child">The child node.</param>
        /// <param name="parent">The parent node.</param>
        /// <param name="name">The branch line ID, empty if it's a new branch line.</param>
        /// <returns>The created branch line.</returns>
        public static GameObject CreateBranchLine(GameObject child, GameObject parent, string name = "")
        {
            /// Calculates the end point of the branch line.
            /// Depending on the parent node and the node position.
            Vector3 endPoint = NearestPoints.GetNearestPoint(parent, child.transform.position);

            /// Calculates the start point of the branch line.
            /// Depending on the node and the end point.
            Vector3 startPoint = NearestPoints.GetNearestPoint(child, endPoint);

            /// Array that contains the start and end point.
            Vector3[] positions = new Vector3[2];
            positions[0] = startPoint;
            positions[1] = endPoint;
            /// Convert the positions to local space.
            child.GetRootParent().transform.InverseTransformPoints(positions);

            GameObject surface = GameFinder.GetDrawableSurface(child);
            /// If no name was chosen, use <see cref="ValueHolder.MindMapBranchLine"/> - ParentID - NodeID.
            if (name == "")
            {
                name = ValueHolder.MindMapBranchLine + "-" + GetIDofName(parent.name) + "-" + GetIDofName(child.name);
            }
            /// Creates the branch line.
            GameObject branchLine = DrawLine(surface, name, positions, ColorKind.Monochrome,
                        Color.black, ValueHolder.CurrentSecondaryColor, ValueHolder.StandardLineThickness, true,
                        LineKind.Solid, ValueHolder.StandardLineTiling, increaseCurrentOrder: false);

            /// Calculates the order.
            /// An order lower than the lower order (parent or node).
            /// But <0 will be 0.
            int order = GetBranchLineOrder(child, parent);
            GameLayerChanger.ChangeOrderInLayer(branchLine, order, GameLayerChanger.LayerChangerStates.Decrease, false);

            /// Adds the node and their branch line as a child/branch line pair to the parent holder.
            MMNodeValueHolder parentValueHolder = parent.GetComponent<MMNodeValueHolder>();
            parentValueHolder.AddChild(child, branchLine);

            /// Enter the data in the own node holder.
            MMNodeValueHolder nodeValueHolder = child.GetComponent<MMNodeValueHolder>();
            nodeValueHolder.SetParent(parent, branchLine);
            //nodeValueHolder.Layer = parentValueHolder.Layer + 1;
            MindMapLayer(nodeValueHolder, parentValueHolder);

            /// Disable the Mesh Collider of the branch line.
            /// It has the same reason as for the border.
            branchLine.GetComponent<MeshCollider>().enabled = false;
            return branchLine;
        }

        /// <summary>
        /// Recursively updates the layer of the given mind map node and all of its children.
        /// Each child node's layer is set to its parent's layer + 1.
        /// </summary>
        /// <param name="holder">The node whose layer will be updated.</param>
        /// <param name="parent">The parent node used to determine the new layer for <paramref name="holder"/>.</param>
        private static void MindMapLayer(MMNodeValueHolder holder, MMNodeValueHolder parent)
        {
            int oldLayer = holder.Layer;
            holder.Layer = parent.Layer + 1;

            if (holder.Layer != oldLayer && holder.GetChildren().Count > 0)
            {
                foreach(GameObject child in holder.GetChildren().Keys)
                {
                    MMNodeValueHolder childHolder = child.GetComponent<MMNodeValueHolder>();
                    MindMapLayer(childHolder, holder);
                }
            }
        }

        /// <summary>
        /// Calculates the order for the branch line.
        /// The order of the branch line is a sequence lower than the lower sequence of both nodes (node/parent).
        /// </summary>
        /// <param name="child">The child node.</param>
        /// <param name="parent">The parent node.</param>
        /// <returns>The calculated order.</returns>
        private static int GetBranchLineOrder(GameObject child, GameObject parent)
        {
            int order;
            if (child.GetComponent<OrderInLayerValueHolder>().OrderInLayer >
                parent.GetComponent<OrderInLayerValueHolder>().OrderInLayer)
            {
                /// Block for: Parent has a lower order.
                order = parent.GetComponent<OrderInLayerValueHolder>().OrderInLayer - 1;
            }
            else
            {
                /// Block for: Node has a lower order.
                order = child.GetComponent<OrderInLayerValueHolder>().OrderInLayer - 1;
            }

            /// If the order would be lower then 0 it's set to 0.
            if (order < 0)
            {
                order = 0;
            }
            return order;
        }

        /// <summary>
        /// Redraws the branch line to the parent node.
        /// </summary>
        /// <param name="node">The node whose parent branch line should be redrawn.</param>
        public static void ReDrawParentBranchLine(GameObject node)
        {
            if (node.CompareTag(Tags.MindMapNode))
            {
                MMNodeValueHolder valueHolder = node.GetComponent<MMNodeValueHolder>();
                if (valueHolder.GetParentBranchLine() != null)
                {
                    GameObject parent = valueHolder.GetParent();
                    CreateBranchLine(node, parent, valueHolder.GetParentBranchLine().name);
                }
            }
        }

        /// <summary>
        /// Redraws the branch lines of a node.
        /// Includes the parent branch line and
        /// the branch lines to the children of the given node.
        /// </summary>
        /// <param name="node">The node whose branch lines should be redrawn.</param>
        /// <returns>True, if the given node has a <see cref="Tags.MindMapNode"/> and the redraw was successful.</returns>
        public static bool ReDrawBranchLines(GameObject node)
        {
            if (node.CompareTag(Tags.MindMapNode))
            {
                MMNodeValueHolder valueHolder = node.GetComponent<MMNodeValueHolder>();
                /// Re-draws the parent branch line.
                ReDrawParentBranchLine(node);
                /// Block for re-drawing the branch lines of the children.
                if (valueHolder.GetChildren().Count > 0)
                {
                    foreach (KeyValuePair<GameObject, GameObject> pair in valueHolder.GetChildren())
                    {
                        CreateBranchLine(pair.Key, node, pair.Value.name);
                    }
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Provides the changing of the parent.
        /// If the newly chosen parent is different from the previous one,
        /// and if the validity check returns a positive result, the following will happen:
        /// - It removes the node of the children list of the old parent
        ///   and destroys the old parent branch line.
        /// - It creates a new branch line to the new chosen parent.
        ///   (<see cref="CreateBranchLine"/>)
        /// </summary>
        /// <param name="child">The child node.</param>
        /// <param name="parent">The newly chosen parent node.</param>
        public static void ChangeParent(GameObject child, GameObject parent)
        {
            if (child.CompareTag(Tags.MindMapNode) && parent != null
                && parent.CompareTag(Tags.MindMapNode))
            {
                MMNodeValueHolder nodeValueHolder = child.GetComponent<MMNodeValueHolder>();
                if (nodeValueHolder.GetParent() != parent && ParentChangeIsValid(child, parent))
                {
                    /// Remove the node from the list of children of the old parent.
                    if (nodeValueHolder.GetParent() != null)
                    {
                        nodeValueHolder.GetParent().GetComponent<MMNodeValueHolder>().RemoveChild(child);
                    }

                    LineConf oldBranchLine = null;

                    /// Block for saving the branch configuration.
                    /// Will be needed for restore the branch line appearance.
                    if (nodeValueHolder.GetParentBranchLine() != null)
                    {
                        oldBranchLine = LineConf.GetLine(nodeValueHolder.GetParentBranchLine());
                    }

                    /// Destroys the old branch line.
                    Destroyer.Destroy(nodeValueHolder.GetParentBranchLine());

                    /// Creates the new branch line.
                    GameObject newBranchLine = CreateBranchLine(child, parent, "");

                    /// Restores the branch line appearance.
                    if (oldBranchLine != null)
                    {
                        GameEdit.ChangeLine(newBranchLine, oldBranchLine);
                    }
                }
            }
        }

        /// <summary>
        /// Validity check for the change of parent.
        /// The check prevents the formation of a cycle.
        /// </summary>
        /// <param name="child">The child node.</param>
        /// <param name="parent">The parent.</param>
        /// <param name="result">Iteration variable, for the result.</param>
        /// <returns>If the parent change is possible or not.</returns>
        public static bool ParentChangeIsValid(GameObject child, GameObject parent, bool result = true)
        {
            if (child.CompareTag(Tags.MindMapNode) && parent.CompareTag(Tags.MindMapNode))
            {
                MMNodeValueHolder valueHolder = child.GetComponent<MMNodeValueHolder>();
                if (child == parent)
                {
                    result = false;
                }
                /// Check to prevent cycles.
                foreach (KeyValuePair<GameObject, GameObject> pair in valueHolder.GetChildren())
                {
                    result = result && ParentChangeIsValid(pair.Key, parent, result);
                }
                return result;
            }
            return false;
        }

        /// <summary>
        /// Provides the changing of the node kind.
        /// If the newly chosen node kind is different from the previous one,
        /// and if the validity check returns a positive result, the following will happen:
        /// - If the new node kind is a theme, the old parent branch line will
        ///   be deleted and the parent will be set to null.
        /// - It adjusts the font size, font styles, border shape, and line kind of the borders.
        /// - If the node kind will be switched from leaf to another node kind, the border will be black.
        /// - The prefix of the node ID will change to the newly selected node kind prefix.
        /// - The branch lines will be redrawn.
        /// </summary>
        /// <param name="node">The node that should change the node kind.</param>
        /// <param name="newNodeKind">The new node kind for the node.</param>
        /// <param name="borderConf">Optional parameter: To make the border look like the old one.</param>
        /// <returns>The new node kind of the node.</returns>
        public static NodeKind ChangeNodeKind(GameObject node, NodeKind newNodeKind, LineConf borderConf = null)
        {
            MMNodeValueHolder nodeValueHolder = node.GetComponent<MMNodeValueHolder>();
            GameObject nodeText = node.FindDescendantWithTag(Tags.DText);
            GameObject nodeBorder = node.FindDescendantWithTag(Tags.Line);

            if (nodeValueHolder.NodeKind != newNodeKind
                && CheckValidNodeKindChange(node, newNodeKind, nodeValueHolder.NodeKind))
            {
                bool ellipse = false;
                switch (newNodeKind)
                {
                    /// Block to change the <see cref="NodeKind"/> to <see cref="NodeKind.Theme"/>.
                    case NodeKind.Theme:
                        /// Remove the node from the list of children of the old parent.
                        if (nodeValueHolder.GetParent() != null)
                        {
                            nodeValueHolder.GetParent().GetComponent<MMNodeValueHolder>().RemoveChild(node);
                        }
                        /// Destroys the old parent branch line.
                        Destroyer.Destroy(nodeValueHolder.GetParentBranchLine());
                        /// Sets the parent and the branch line to them to null.
                        nodeValueHolder.SetParent(null, null);

                        /// Change the node appearance to the appearance of a Theme.
                        ellipse = true;
                        GameEdit.ChangeFontStyles(nodeText, FontStyles.Bold | FontStyles.Underline);
                        GameEdit.ChangeFontSize(nodeText, 1.0f);
                        ChangeLineKind(nodeBorder, LineKind.Solid, ValueHolder.StandardLineTiling);
                        GameEdit.ChangePrimaryColor(nodeBorder, Color.black);
                        GameEdit.ChangeSecondaryColor(nodeBorder, Color.black);
                        break;

                    /// Block for change the <see cref="NodeKind"/> to <see cref="NodeKind.Subtheme"/>.
                    case NodeKind.Subtheme:
                        /// Change the node appearance to the appearance of a Subtheme.
                        GameEdit.ChangeFontStyles(nodeText, FontStyles.Normal);
                        GameEdit.ChangeFontSize(nodeText, 0.7f);
                        ChangeLineKind(nodeBorder, LineKind.Solid, ValueHolder.StandardLineTiling);
                        GameEdit.ChangePrimaryColor(nodeBorder, Color.black);
                        GameEdit.ChangeSecondaryColor(nodeBorder, Color.black);
                        break;

                    /// Block for change the <see cref="NodeKind"/> to <see cref="NodeKind.Leaf"/>.
                    case NodeKind.Leaf:
                        /// Change the node appearance to the appearance of a Leaf.
                        ellipse = true;
                        GameEdit.ChangeFontStyles(nodeText, FontStyles.Normal);
                        GameEdit.ChangeFontSize(nodeText, 0.5f);
                        ChangeLineKind(nodeBorder, LineKind.Dashed25, ValueHolder.StandardLineTiling);
                        GameEdit.ChangePrimaryColor(nodeBorder, Color.clear);
                        GameEdit.ChangeSecondaryColor(nodeBorder, Color.clear);
                        break;
                }
                /// Changes the prefix of the node.
                ChangeName(node, newNodeKind);

                /// Disables the text and border collider. The changes before can activate them.
                DisableTextAndBorderCollider(node);

                /// Calculates the new border positions.
                Vector3[] positions = GetBorderPositions(ellipse, Vector3.zero, nodeText);

                /// Refreshes the border line.
                Drawing(nodeBorder, positions);

                /// Renew the size of the BoxCollider.
                ChangeBoxSize(node);

                /// Restores the old border appearance, if the new node kind is not a Leaf.
                if (newNodeKind != NodeKind.Leaf && borderConf != null
                    && borderConf.PrimaryColor != Color.clear)
                {
                    GameEdit.ChangeLine(nodeBorder, borderConf);
                }
                /// Sets the new node kind to the <see cref="MMNodeValueHolder"/>.
                nodeValueHolder.NodeKind = newNodeKind;

                /// At least refresh the branch lines.
                ReDrawBranchLines(node);
            }
            return nodeValueHolder.NodeKind;
        }

        /// <summary>
        /// Changes the prefix of a node.
        /// </summary>
        /// <param name="node">The node whose ID should be changed.</param>
        /// <param name="newNodeKind">The new node kind whose prefix should be used.</param>
        private static void ChangeName(GameObject node, NodeKind newNodeKind)
        {
            NodeKind old = node.GetComponent<MMNodeValueHolder>().NodeKind;
            node.name = node.name.Replace(GetPrefix(old), GetPrefix(newNodeKind));
        }

        /// <summary>
        /// Gets the prefix of a given node kind.
        /// </summary>
        /// <param name="nodeKind">The chosen node kind which prefix should be returned.</param>
        /// <returns>The node kind prefix.</returns>
        private static string GetPrefix(NodeKind nodeKind)
        {
            return nodeKind switch
            {
                NodeKind.Theme => ValueHolder.MindMapThemePrefix,
                NodeKind.Subtheme => ValueHolder.MindMapSubthemePrefix,
                NodeKind.Leaf => ValueHolder.MindMapLeafPrefix,
                _ => "",
            };
        }

        /// <summary>
        /// Disables the text and border mesh collider of a node.
        /// </summary>
        /// <param name="node">The node whose text and border collider should be disabled.</param>
        public static void DisableTextAndBorderCollider(GameObject node)
        {
            node.FindDescendantWithTag(Tags.Line).GetComponent<Collider>().enabled = false;
            node.FindDescendantWithTag(Tags.DText).GetComponent<Collider>().enabled = false;
        }

        /// <summary>
        /// Checks the validity and possibility <see cref="ChangeIsPossible"/> of the node kind change.
        /// - A leaf can be transformed into any other node kind at any time.
        /// - A theme can only be transformed if a suitable parent is present.
        ///     For the transformation into a leaf, it additionally must not have any child nodes.
        /// - A subtheme can be transformed into a theme at any time.
        ///     It can only be transformed into a leaf if it has no children.
        /// </summary>
        /// <param name="node">The node whose node kind should be changed.</param>
        /// <param name="newNodeKind">The new node kind.</param>
        /// <param name="oldNodeKind">The old node kind.</param>
        /// <returns>The result of the check.</returns>
        public static bool CheckValidNodeKindChange(GameObject node, NodeKind newNodeKind, NodeKind oldNodeKind)
        {
            MMNodeValueHolder valueHolder = node.GetComponent<MMNodeValueHolder>();
            if (oldNodeKind == NodeKind.Theme)
            {
                return (newNodeKind == NodeKind.Leaf && valueHolder.GetChildren().Count == 0
                            || newNodeKind == NodeKind.Subtheme) && ChangeIsPossible(node);
            }
            if (oldNodeKind == NodeKind.Subtheme)
            {
                return newNodeKind == NodeKind.Theme
                    || newNodeKind == NodeKind.Leaf && valueHolder.GetChildren().Count == 0;
            }
            return true;
        }

        /// <summary>
        /// Checks if a node kind change from theme to subtheme is possible.
        /// For this, another theme node must exist on the drawable
        /// that is considered as a new parent.
        /// </summary>
        /// <param name="selectedNode">The selected node.</param>
        /// <returns>True if change is possible.</returns>
        private static bool ChangeIsPossible(GameObject selectedNode)
        {
            GameObject attacheds = GameFinder.GetAttachedObjectsObject(selectedNode);
            foreach (GameObject node in attacheds.FindAllDescendantsWithTag(Tags.MindMapNode))
            {
                if (node.GetComponent<MMNodeValueHolder>().NodeKind == NodeKind.Theme
                    && ParentChangeIsValid(selectedNode, node))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Re-creates a mind map node
        /// </summary>
        /// <param name="surface">The drawable surface on which the node should be displayed.</param>
        /// <param name="parent">The parent mind map node.</param>
        /// <param name="name">The ID of the node.</param>
        /// <param name="textConf">The text configuration for the text (description) of the node.</param>
        /// <param name="borderConf">The line configuration for the node border.</param>
        /// <param name="position">The position for the node.</param>
        /// <param name="scale">The scale for the node.</param>
        /// <param name="eulerAngles">The euler angles for the node.</param>
        /// <param name="order">The order for the node.</param>
        /// <param name="nodeKind">The node kind for the node.</param>
        /// <param name="branchToParentName">The branch line name.</param>
        /// <returns>The created mind map node.</returns>
        private static GameObject ReCreate(GameObject surface, GameObject parent, string name,
            TextConf textConf, LineConf borderConf, Vector3 position, Vector3 scale,
            Vector3 eulerAngles, int order, NodeKind nodeKind, string branchToParentName, int associatedPage)
        {
            /// Adjusts the current order in the layer if the
            /// order in layer for the line is greater than or equal to it.
            DrawableHolder holder = surface.GetComponent<DrawableHolder>();
            if (order >= holder.OrderInLayer && associatedPage == holder.CurrentPage)
            {
                holder.OrderInLayer = order + 1;
            }
            if (associatedPage >= holder.MaxPageSize)
            {
                holder.MaxPageSize = associatedPage + 1;
            }
            if (order >= ValueHolder.MaxOrderInLayer)
            {
                ValueHolder.MaxOrderInLayer = order + 1;
            }
            GameObject createdNode;

            /// Try to find the node.
            if (GameFinder.FindChild(surface, name) != null)
            {
                createdNode = GameFinder.FindChild(surface, name);
            }
            else
            {
                /// Creates the node.
                Setup(surface, name, GetPrefix(nodeKind), textConf.Text,
                    surface.transform.TransformPoint(position), associatedPage, out GameObject node);
                /// Destroyes the text and border, because the originals will be restored below.
                Destroyer.Destroy(node.FindDescendantWithTag(Tags.Line));
                Destroyer.Destroy(node.FindDescendantWithTag(Tags.DText));
                createdNode = node;
            }

            /// Restores the border.
            GameObject border = ReDrawLine(surface, borderConf);

            /// Restores the text and sets the order.
            textConf.OrderInLayer = order;
            GameObject text = GameTexter.ReWriteText(surface, textConf);
            text.GetComponent<OrderInLayerValueHolder>().OrderInLayer = 0;

            /// Assigns the border and the text to the node.
            border.transform.SetParent(createdNode.transform);
            text.transform.SetParent(createdNode.transform);

            /// Disables the colliders. For the reason look in <see cref="Setup"/>.
            text.GetComponent<MeshCollider>().enabled = false;
            border.GetComponent<MeshCollider>().enabled = false;

            /// Sets the position to the middle of the node.
            border.transform.localPosition = Vector3.zero;
            text.transform.localPosition = Vector3.zero;

            /// Adds and calculates the box collider size.
            BoxCollider box = createdNode.GetComponent<BoxCollider>();
            box.size = GetBoxSize(border);

            /// Restores the old values.
            createdNode.transform.localScale = scale;
            createdNode.transform.localEulerAngles = eulerAngles;
            createdNode.transform.localPosition = position;
            createdNode.GetComponent<OrderInLayerValueHolder>().OrderInLayer = order;

            createdNode.GetComponent<AssociatedPageHolder>().AssociatedPage = associatedPage;
            border.GetComponent<AssociatedPageHolder>().AssociatedPage = associatedPage;
            text.GetComponent<AssociatedPageHolder>().AssociatedPage = associatedPage;

            if (associatedPage != surface.GetComponent<DrawableHolder>().CurrentPage)
            {
                createdNode.SetActive(false);
            }

            /// Create the branch line, if the node has a parent.
            if (parent != null)
            {
                CreateBranchLine(createdNode, parent, branchToParentName);
            }
            return createdNode;
        }

        /// <summary>
        /// Recreates a mind map node based on <paramref name="conf"/>.
        /// </summary>
        /// <param name="surface">The drawable surface on which the node should be displayed.</param>
        /// <param name="conf">The node configuration for restore.</param>
        /// <returns>The mind map node.</returns>
        public static GameObject ReCreate(GameObject surface, MindMapNodeConf conf)
        {
            GameObject parent = null;

            /// Try to find the parent of the configuration.
            if (GameFinder.GetAttachedObjectsObject(surface) != null)
            {
                parent = GameFinder.FindChild(GameFinder.GetAttachedObjectsObject(surface),
                    conf.ParentNode);
            }

            return ReCreate(surface,
                parent,
                conf.ID,
                conf.TextConf,
                conf.BorderConf,
                conf.Position,
                conf.Scale,
                conf.EulerAngles,
                conf.OrderInLayer,
                conf.NodeKind,
                conf.BranchLineToParent,
                conf.AssociatedPage);
        }

        /// <summary>
        /// Renames the mind map nodes and branchlines.
        /// </summary>
        /// <param name="config">The drawable configuration that holds the nodes and branch lines.</param>
        /// <param name="attachedObject">The attached objects object where the drawable types should be placed.</param>
        public static void RenameMindMap(DrawableConfig config, GameObject attachedObject)
        {
            if (attachedObject != null)
            {
                Dictionary<string, string> nameDictionary = new();
                Dictionary<string, string> idDictionary = new();
                /// Block to rename the nodes.
                foreach (MindMapNodeConf node in config.MindMapNodeConfigs)
                {
                    RenameNode(node, attachedObject, nameDictionary, idDictionary);
                }
                /// Block to rename the branch lines.
                foreach (LineConf branchLine in config.LineConfigs)
                {
                    if (branchLine.ID.StartsWith(ValueHolder.MindMapBranchLine))
                    {
                        RenameBranchLine(branchLine, idDictionary);
                    }
                }
            }
        }

        /// <summary>
        /// Renames a mind map node.
        /// </summary>
        /// <param name="conf">The node configuration that should be renamed.</param>
        /// <param name="attachedObjects">The attached objects object where the drawable types should be placed.</param>
        /// <param name="nameDictionary">The dictionary that holds the old name and the new name.</param>
        /// <param name="idDictionary">Dictionary that holds the old IDs and the new names.</param>
        private static void RenameNode(MindMapNodeConf conf, GameObject attachedObjects,
            Dictionary<string, string> nameDictionary, Dictionary<string, string> idDictionary)
        {
            string prefix = GetPrefix(conf.NodeKind);

            if (GameFinder.FindChild(attachedObjects, conf.ID) != null)
            {
                /// Gets a new id for the object based on a random string.
                string id = RandomStrings.GetRandomString(8);
                string newName = prefix + id;

                /// Check if the name is already in use. If so, generate a new name.
                while (GameFinder.FindChild(attachedObjects, newName) != null)
                {
                    id = RandomStrings.GetRandomString(8);
                    newName = prefix + id;
                }

                /// Adds the old and the new name to a dictionary.
                nameDictionary.Add(conf.ID, newName);

                /// Adds a pair of the old id and the new name in a other dictionary.
                idDictionary.Add(GetIDofName(conf.ID), newName);

                /// Change the names to the new name.
                conf.ID = newName;
                conf.BorderConf.ID = ValueHolder.LinePrefix + id;
                conf.TextConf.ID = ValueHolder.TextPrefix + id;

                /// If the node has a parent, replace the old parent name with the new one.
                if (conf.ParentNode != "")
                {
                    conf.ParentNode = nameDictionary[conf.ParentNode];
                    /// Rename the branch line with the new id's of the parent and the node.
                    conf.BranchLineToParent = ValueHolder.MindMapBranchLine + "-"
                        + GetIDofName(conf.ParentNode) + "-" + GetIDofName(conf.ID);
                    if (conf.BranchLineConf != null)
                    {
                        conf.BranchLineConf.ID = conf.BranchLineToParent;
                    }
                }
            }
        }

        /// <summary>
        /// Renames a branch line.
        /// </summary>
        /// <param name="conf">The branch line config.</param>
        /// <param name="idDictionary">Dictionary that holds the old IDs and the new names.</param>
        private static void RenameBranchLine(LineConf conf, Dictionary<string, string> idDictionary)
        {
            string prefix = ValueHolder.MindMapBranchLine;

            /// Splits the old name into three parts.
            string[] splitOfOldName = conf.ID.Split("-");

            /// Get the new parent id.
            string newParentID = splitOfOldName[1];
            if (idDictionary.TryGetValue(newParentID, out string nPID))
            {
                newParentID = GetIDofName(nPID);
            }

            /// Get the new node id.
            string newChildID = splitOfOldName[2];
            if (idDictionary.TryGetValue(newChildID, out string nCID))
            {
                newChildID = GetIDofName(nCID);
            }

            /// Rename the branch line.
            conf.ID = prefix + "-" + newParentID + "-" + newChildID;
        }

        /// <summary>
        /// Summerizes the selected node, including children and branch lines, into a DrawableConfig.
        /// </summary>
        /// <param name="node">The selected node.</param>
        /// <returns>A drawable configuration that only contains the selected node with children and branch lines.</returns>
        public static DrawableConfig SummarizeSelectedNodeIncChildren(GameObject node)
        {
            if (node.CompareTag(Tags.MindMapNode))
            {
                DrawableConfig conf = DrawableConfigManager.GetDrawableConfig(GameFinder.GetDrawableSurface(node));
                conf.TextConfigs.Clear();
                conf.ImageConfigs.Clear();
                List<LineConf> selectedBranchLines = new();
                List<MindMapNodeConf> selectedNodes = new()
                {
                    /// Adds the selected node to the list.
                    MindMapNodeConf.GetNodeConf(node)
                };
                MMNodeValueHolder valueHolder = node.GetComponent<MMNodeValueHolder>();
                /// Adds the all children nodes and their branch lines to the list.
                foreach (KeyValuePair<GameObject, GameObject> pair in valueHolder.GetAllChildren())
                {
                    selectedNodes.Add(MindMapNodeConf.GetNodeConf(pair.Key));
                    selectedBranchLines.Add(LineConf.GetLine(pair.Value));
                }
                /// Sets the branch line list to the line configurations of the drawable configuration.
                conf.LineConfigs = selectedBranchLines;
                /// Sets the mind map nodes list to the mind map node configurations of the drawable configuration.
                conf.MindMapNodeConfigs = selectedNodes;
                return conf;
            }
            return null;
        }
    }
}