using SEE.DataModel;
using SEE.GO;
using SEE.Utils;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;
using UnityEngine;

namespace SEE.Layout.IO
{
    /// <summary>
    /// Writes layout information to a GVL file.
    /// </summary>
    public class GVLWriter
    {
        /// <summary>
        /// Writes the layout information of all <paramref name="graphName"/> and their descendants tagged
        /// by Tags.Node to a new file named <paramref name="filename"/> in GVL format, where 
        /// <paramref name="graphName"/> is used as the value for attribute V of the layout (the 
        /// graph view).
        /// 
        /// Note: This method is equivalent to Save(string, string, ICollection<GameObject>)
        /// but intended for ILayoutNodes rather than GameObjects.
        /// </summary>
        /// <param name="filename">name of the GVL file</param>
        /// <param name="graphName">name of the graph</param>
        /// <param name="gameNodes">the nodes whose layout is to be stored</param>
        public static void Save(string filename, string graphName, ICollection<ILayoutNode> gameNodes)
        {
            Header(graphName, out XmlDocument doc, out XmlElement layoutElement);
            foreach (ILayoutNode root in ILayoutNodeHierarchy.Roots(gameNodes))
            {
                AppendNode(doc, layoutElement, root);
            }
            doc.Save(filename);
        }

        /// <summary>
        /// Writes the layout information of all <paramref name="graphName"/> and their descendants tagged
        /// by Tags.Node to a new file named <paramref name="filename"/> in GVL format, where 
        /// <paramref name="graphName"/> is used as the value for attribute V of the layout (the 
        /// graph view).
        /// 
        /// Note: This method is equivalent to Save(string, string, ICollection<ILayoutNode>)
        /// but intended for GameObjects rather than ILayoutNodes.
        /// </summary>
        /// <param name="filename">name of the GVL file</param>
        /// <param name="graphName">name of the graph</param>
        /// <param name="gameNodes">the nodes whose layout is to be stored</param>
        public static void Save(string filename, string graphName, ICollection<GameObject> gameNodes)
        {
            Header(graphName, out XmlDocument doc, out XmlElement layoutElement);

            foreach (GameObject root in GameObjectHierarchy.Roots(gameNodes, Tags.Node))
            {
                AppendNode(doc, layoutElement, root);
            }
            doc.Save(filename);
        }

        private static void Header(string graphName, out XmlDocument doc, out XmlElement layoutElement)
        {
            doc = new XmlDocument();
            AppendXMLVersion(doc);
            AppendDOCType(doc);
            layoutElement = AppendLayout(doc, graphName);
            AppendVisualization(doc, layoutElement);
        }

        /// <summary>
        /// Emits <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        /// </summary>
        /// <param name="doc">the XML document in which to create the XML node</param>
        private static void AppendXMLVersion(XmlDocument doc)
        {
            XmlDeclaration xmlDeclaration = doc.CreateXmlDeclaration("1.0", "UTF-8", "yes");
            XmlElement root = doc.DocumentElement;
            doc.InsertBefore(xmlDeclaration, root);
        }

        /// <summary>
        /// Emits <!DOCTYPE Gravis2_Layout>
        /// </summary>
        /// <param name="doc">the XML document in which to create the XML node</param>
        private static void AppendDOCType(XmlDocument doc)
        {
            XmlDocumentType xmlDocumentType = doc.CreateDocumentType("Gravis2_Layout", null, null, null);
            doc.AppendChild(xmlDocumentType);
        }

        /// <summary>
        /// Appends and returns an XML child
        ///    <Gravis2_Layout W="Hierarchy" V="graphName">
        ///    ...
        ///    </Gravis2_Layout>
        /// </summary>
        /// <param name="doc">the XML document in which to create the XML node</param>
        /// <param name="graphName">the name of the graph view; this is the value for attribute "V"</param>
        /// <returns>the created XML node</returns>
        private static XmlElement AppendLayout(XmlDocument doc, string graphName)
        {
            XmlElement layout = doc.CreateElement(null, "Gravis2_Layout", null);
            layout.SetAttribute("W", "Hierarchy");
            layout.SetAttribute("V", graphName);
            doc.AppendChild(layout);
            return layout;
        }

        /// <summary>
        /// Appends
        ///   <Visualization>
        ///     <Zoom Factor = "1.0" Center_X="0.0" Center_Y="0.0"/>
        ///     <Option Name = "Mute_Edges" Value="False"/>
        ///     <Option Name = "Semi_Transparent_Edges" Value="False"/>
        ///     <Option Name = "Edges_Behind_Nodes" Value="False"/>
        ///     <Option Name = "Show_Lifted_Edges" Value="False"/>
        ///     <Option Name = "Semi_Transparent_Backgrounds" Value="True"/>
        ///     <Option Name = "Show_Node_Names" Value="True"/>
        ///     <Option Name = "Show_Attributes" Value="True"/>
        ///     <Hidden_Node_Types>
        ///     </Hidden_Node_Types>
        ///     <Hidden_Edge_Types>
        ///     </Hidden_Edge_Types>
        ///   </Visualization>
        /// </summary>
        /// <param name="doc">the XML document in which to create the XML node</param>
        /// <param name="layout">the containing layout XML element</param>
        private static void AppendVisualization(XmlDocument doc, XmlElement layout)
        {
            XmlElement viz = doc.CreateElement(null, "Visualization", null);
            {
                XmlElement zoom = doc.CreateElement(null, "Zoom", null);
                zoom.SetAttribute("Factor", "1.0");
                zoom.SetAttribute("Center_X", "0.0");
                zoom.SetAttribute("Center_Y", "0.0");
                viz.AppendChild(zoom);
            }
            AddOption(doc, viz, "Mute_Edges");
            AddOption(doc, viz, "Semi_Transparent_Edges");
            AddOption(doc, viz, "Edges_Behind_Nodes");
            AddOption(doc, viz, "Show_Lifted_Edges");
            AddOption(doc, viz, "Semi_Transparent_Backgrounds");
            AddOption(doc, viz, "Show_Node_Names");
            AddOption(doc, viz, "Show_Attributes");
            AddXMLElement(doc, viz, "Hidden_Node_Types");
            AddXMLElement(doc, viz, "Hidden_Edge_Types");
            layout.AppendChild(viz);
        }

        /// <summary>
        /// Appends
        ///    <Option Name = "name" Value="False"/>
        /// </summary>
        /// <param name="doc">the XML document in which to create the XML node</param>
        /// <param name="viz">the containing Visualization XML element</param>
        /// <param name="name">name of the option</param>
        private static void AddOption(XmlDocument doc, XmlElement viz, string name)
        {
            XmlElement option = doc.CreateElement(null, "Option", null);
            option.SetAttribute("Name", name);
            option.SetAttribute("Value", "False");
            viz.AppendChild(option);
        }

        /// <summary>
        /// Appends 
        ///    <element />
        /// </summary>
        /// <param name="doc">the XML document in which to create the XML node</param>
        /// <param name="parent">the containing XML element</param>
        /// <param name="element">the name of the new element</param>
        private static void AddXMLElement(XmlDocument doc, XmlElement parent, string element)
        {
            XmlElement child = doc.CreateElement(null, element, null);
            parent.AppendChild(child);
        }

        /// <summary>
        /// Appends
        ///   <Node Id="L*NAME*" X="*X*" Y="*Y*" W="*W* H="*H*" CS="14.14" Exp="True">
        ///     .... 
        ///   </Node>
        ///   where 
        ///     ... contains all descendants of <paramref name="node"/> tagged by Tag.Node
        ///     *NAME* is node.ID
        ///     *X* is the x co-ordinate of the left upper corner of the rectangle containing node
        ///     *Y* is the z co-ordinate of the left upper corner of the rectangle containing node
        ///     *W* is the width (x axis) of the rectangle containing node
        ///     *H* is the depth (z axis) of the rectangle containing node
        ///     and all emitted measures are in world space. If <paramref name="parent"/> = null, 
        ///     *X* and *Y* are the original values of <paramref name="node"/>; otherwise those values
        ///     are relative offsets to the left upper corner of <paramref name="parent"/>.
        /// </summary>
        /// <param name="doc">the XML document in which to create the XML node</param>
        /// <param name="xmlParent">the containing XML element</param>
        /// <param name="node">node whose layout information is to be emitted</param>
        private static void AppendNode(XmlDocument doc, XmlElement xmlParent, ILayoutNode node)
        {
            string ID = node.ID;
            ILayoutNode parent = node.Parent;
            bool isRoot = parent == null;
            Vector3 parentPosition = isRoot ? Vector3.zero : parent.CenterPosition;
            Vector3 parentScale = isRoot ? Vector3.zero : parent.AbsoluteScale;
            XmlElement xmlNode = AppendNode(doc, xmlParent,
                                            ID, node.CenterPosition, node.AbsoluteScale,
                                            isRoot, parentPosition, parentScale);

            foreach (ILayoutNode child in node.Children())
            {
                AppendNode(doc, xmlNode, child);
            }
        }

        /// <summary>
        /// Appends
        ///   <Node Id="L*NAME*" X="*X*" Y="*Y*" W="*W* H="*H*" CS="14.14" Exp="True">
        ///     .... 
        ///   </Node>
        ///   where 
        ///     ... contains all descendants of <paramref name="node"/> tagged by Tag.Node
        ///     *NAME* is node.ID
        ///     *X* is the x co-ordinate of the left upper corner of the rectangle containing node
        ///     *Y* is the z co-ordinate of the left upper corner of the rectangle containing node
        ///     *W* is the width (x axis) of the rectangle containing node
        ///     *H* is the depth (z axis) of the rectangle containing node
        ///     and all emitted measures are in world space. If <paramref name="parent"/> = null, 
        ///     *X* and *Y* are the original values of <paramref name="node"/>; otherwise those values
        ///     are relative offsets to the left upper corner of <paramref name="parent"/>.
        /// </summary>
        /// <param name="doc">the XML document in which to create the XML node</param>
        /// <param name="xmlParent">the containing XML element</param>
        /// <param name="node">node whose layout information is to be emitted</param>
        private static void AppendNode(XmlDocument doc, XmlElement xmlParent, GameObject node)
        {
            string ID = node.ID();
            GameObject parent = GameObjectHierarchy.Parent(node, Tags.Node);
            bool isRoot = parent == null;
            Vector3 parentPosition = isRoot ? Vector3.zero : parent.transform.position;
            Vector3 parentScale = isRoot ? Vector3.zero : parent.transform.lossyScale;
            XmlElement xmlNode = AppendNode(doc, xmlParent,
                                            ID, node.transform.position, node.transform.lossyScale,
                                            isRoot, parentPosition, parentScale);

            foreach (GameObject child in GameObjectHierarchy.Children(node, Tags.Node))
            {
                AppendNode(doc, xmlNode, child);
            }
        }

        /// <summary>
        /// Appends
        ///   <Node Id="L*NAME*" X="*X*" Y="*Y*" W="*W* H="*H*" CS="14.14" Exp="True" />
        ///   where 
        ///     *NAME* is <paramref name="ID"/>
        ///     *X* is the x co-ordinate of the left upper corner of the rectangle containing node
        ///     *Y* is the z co-ordinate of the left upper corner of the rectangle containing node
        ///     *W* is the width (x axis) of the rectangle containing node
        ///     *H* is the depth (z axis) of the rectangle containing node
        ///     and all emitted measures are in world space. If not <paramref name="isRoot"/>, 
        ///     *X* and *Y* are absolute values for the rectangle defined by 
        ///     <paramref name="nodeCenterPosition"/> and <paramref name="nodeAbsoluteScale"/>. 
        ///     Otherwise those values are relative offsets to the left upper corner of the 
        ///     rectangle defined by <paramref name="parentCenterPosition"/> and 
        ///     <paramref name="parentAbsoluteScale"/>.
        /// </summary>
        /// <param name="doc">the XML document in which to create the XML node</param>
        /// <param name="xmlParent">the containing XML element</param>
        /// <param name="ID">the unique identifier of the node</param>
        /// <param name="nodeCenterPosition">the center position of the node in world space</param>
        /// <param name="nodeAbsoluteScale">the scale of the node in world space</param>
        /// <param name="isRoot">whether the node is a root node, that is, has no parent</param>
        /// <param name="parentCenterPosition">the center position of the node's parent in world space;
        /// defined only if not <paramref name="isRoot"/></param>
        /// <param name="parentAbsoluteScale">the scale of the node's parent in world space;
        /// defined only if not <paramref name="isRoot"/></param>
        private static XmlElement AppendNode
            (XmlDocument doc,
            XmlElement xmlParent,
            string ID,
            Vector3 nodeCenterPosition,
            Vector3 nodeAbsoluteScale,
            bool isRoot,
            Vector3 parentCenterPosition,
            Vector3 parentAbsoluteScale)
        {
            //Debug.LogFormat("ID={0} nodeCenterPosition={1} nodeAbsoluteScale={2} isRoot={3} parentCenterPosition={4} parentAbsoluteScale={5}\n",
            //                ID, nodeCenterPosition.ToString("F4"), nodeAbsoluteScale.ToString("F4"), isRoot, parentCenterPosition.ToString("F4"), parentAbsoluteScale.ToString("F4"));

            XmlElement xmlNode = doc.CreateElement(null, "Node", null);
            xmlParent.AppendChild(xmlNode);
            xmlNode.SetAttribute("Id", "L" + ID);

            if (isRoot)
            {
                // Node is a root node.
                // assert: (X, Y) in GVL relates to absolute world space of the left upper corner,
                // whereas (nodeCenterPosition.x, nodeCenterPosition.z) relates to the center within the x/z plane.
                // Transform from center position of Unity to (X, Y) co-ordinate system in GVL.
                float X = nodeCenterPosition.x - nodeAbsoluteScale.x / 2.0f;

                // Gravis's Y is Unity's inverted z axis, i.e., we need to mirror Unity's z
                // as follows: Y = -z. By mirroring z, the left upper corner of a node
                // becomes its left lower corner.
                float Y = -nodeCenterPosition.z - nodeAbsoluteScale.z / 2.0f;

                xmlNode.SetAttribute("X", FloatToString(X));
                xmlNode.SetAttribute("Y", FloatToString(Y));
            }
            else
            {
                // Node is a nested node, that is, not at top-level.
                // (X, Y) must denote the offset of the nested node to its parent's left upper corner

                // Transform parent's center position to its left upper corner in Unity's co-ordinates.
                Vector3 parentCornerPosition = parentCenterPosition; // world space in Unity
                parentCornerPosition.x -= parentAbsoluteScale.x / 2.0f;
                parentCornerPosition.z += parentAbsoluteScale.z / 2.0f;

                // Transform node's center position to its left upper corner in Unity's co-ordinates.
                Vector3 nodeCornerPosition = nodeCenterPosition;
                nodeCornerPosition.x -= nodeAbsoluteScale.x / 2.0f;
                nodeCornerPosition.z += nodeAbsoluteScale.z / 2.0f;

                // Calculate the offset between the two corners.
                float X = nodeCornerPosition.x - parentCornerPosition.x;
                float Y = parentCornerPosition.z - nodeCornerPosition.z;

                xmlNode.SetAttribute("X", FloatToString(X));
                xmlNode.SetAttribute("Y", FloatToString(Y));

            }
            xmlNode.SetAttribute("W", FloatToString(nodeAbsoluteScale.x));
            xmlNode.SetAttribute("H", FloatToString(nodeAbsoluteScale.z));

            xmlNode.SetAttribute("CS", FloatToString(14.4f));
            xmlNode.SetAttribute("Exp", "True");
            return xmlNode;
        }


        /// <summary>
        /// Implementation of delegate AsString for float values. The CultureInfo.InvariantCulture
        /// is used as expected in a GXL file.
        /// </summary>
        /// <param name="value">the value to be converted</param>
        /// <returns>value as a string</returns>
        private static string FloatToString(float value)
        {
            return value.ToString(CultureInfo.InvariantCulture.NumberFormat);
        }
    }
}