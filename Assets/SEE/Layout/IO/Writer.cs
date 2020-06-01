using UnityEngine;
using System.Globalization;
using System.Xml;
using System.Collections.Generic;
using System;

namespace SEE.Layout.IO
{
    /// <summary>
    /// Writes layout information to a GVL file.
    /// </summary>
    public class Writer
    {
        /// <summary>
        /// Writes the layout information of all <paramref name="graphName"/> and their descendants tagged
        /// by Tags.Node to a new file named <paramref name="filename"/> in GVL format, where 
        /// <paramref name="graphName"/> is used as the value for attribute V of the layout (the 
        /// graph view).
        /// 
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="graphName"></param>
        /// <param name="gameNodes"></param>
        public static void Save(string filename, string graphName, ICollection<ILayoutNode> gameNodes)
        {
            XmlDocument doc = new XmlDocument();
            AppendXMLVersion(doc);
            AppendDOCType(doc);
            XmlElement layout = AppendLayout(doc, graphName);
            AppendVisualization(doc, layout);
            ICollection<ILayoutNode> roots = Roots(gameNodes);
            foreach (ILayoutNode root in roots)
            {
                AppendNode(doc, layout, root);
            }
            doc.Save(filename);
        }

        /// <summary>
        /// Returns all nodes in <paramref name="gameNodes"/> that do not have a parent.
        /// </summary>
        /// <param name="gameNodes">nodes to be queried</param>
        /// <returns>all root nodes in <paramref name="gameNodes"/></returns>
        private static ICollection<ILayoutNode> Roots(ICollection<ILayoutNode> gameNodes)
        {
            ICollection<ILayoutNode> result = new List<ILayoutNode>();
            foreach (ILayoutNode node in gameNodes)
            {
                if (node.Parent == null)
                {
                    result.Add(node);
                }
            }
            return result;
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
            Vector3 centerPosition = node.CenterPosition;
            Vector3 absoluteScale = node.AbsoluteScale;
            // absolute positions
            float x = centerPosition.x - absoluteScale.x / 2.0f;
            float y = centerPosition.z + absoluteScale.z / 2.0f;

            XmlElement xmlNode = doc.CreateElement(null, "Node", null);
            xmlNode.SetAttribute("Id", "L" + node.ID);

            ILayoutNode parent = node.Parent;
            if (parent != null)
            {
                // adjust positions relative to parent
                Vector3 parentPosition = parent.CenterPosition;
                Vector3 parentScale = parent.AbsoluteScale;

                float parentX = parentPosition.x - parentScale.x / 2.0f;
                float parentY = parentPosition.z + parentScale.z / 2.0f;

                x = x - parentX;
                y = parentY - y;

            }
            xmlNode.SetAttribute("X", FloatToString(x));
            xmlNode.SetAttribute("Y", FloatToString(y));

            xmlNode.SetAttribute("W", FloatToString(absoluteScale.x));
            xmlNode.SetAttribute("H", FloatToString(absoluteScale.z));

            xmlNode.SetAttribute("CS", FloatToString(14.4f));
            xmlNode.SetAttribute("Exp", "True");

            foreach (ILayoutNode child in node.Children())
            {
                AppendNode(doc, xmlNode, child);
            }

            xmlParent.AppendChild(xmlNode);
        }


        /// <summary>
        /// Implementation of delegate AsString for float values. The CultureInfo.InvariantCulture
        /// is used as expected in a GXL file.
        /// </summary>
        /// <param name="value">the value to be converted</param>
        /// <returns>value as a string</returns>
        private static string FloatToString(float value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

    }
}