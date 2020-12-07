using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;
using UnityEngine;

namespace SEE.DataModel.DG.IO
{
    /// <summary>
    /// Saves graphs in GXL format on disk.
    /// </summary>
    public static class GraphWriter
    {
        /// <summary>
        /// Saves given <paramref name="graph"/> in GXL format in a file with given <paramref name="filename"/>.
        /// The parent-child relation between nodes is stored as edges with the type <paramref name="hierarchicalEdgeType"/>.
        /// The attributes of the <paramref name="graph"/> itself are not stored to stay compatible
        /// with the GXL files by Axivion.
        /// </summary>
        /// <param name="filename">name of the file where to store the graph</param>
        /// <param name="graph">graph to be stored</param>
        /// <param name="hierarchicalEdgeType">edge type for the node hierarchy</param>
        public static void Save(string filename, Graph graph, string hierarchicalEdgeType)
        {
            XmlDocument doc = new XmlDocument();

            AppendXMLVersion(doc);
            AppendDOCType(doc);
            XmlElement gxl = AppendGXL(doc);
            XmlElement graphNode = AppendGraph(doc, gxl, graph);
            Dictionary<string, string> nodeIDs_To_GXLids = AppendNodes(doc, graphNode, graph);
            AppendDependencyEdges(doc, graphNode, graph, nodeIDs_To_GXLids);
            int hierarchicalEdgeCount = 1;
            AppendChildren(doc, graphNode, graph, nodeIDs_To_GXLids, hierarchicalEdgeType, ref hierarchicalEdgeCount);
            try
            {
                doc.Save(filename);
            }
            catch(Exception e)
            {
                Debug.LogErrorFormat("Could not save graph to GXL file '{0}' due to: {1}.\n", filename, e.Message);
                throw e;
            }
        }

        /// <summary>
        /// Emits <?xml version="1.0" encoding="UTF-8"?>
        /// </summary>
        /// <param name="doc">the XML document in which to create the XML node</param>
        private static void AppendXMLVersion(XmlDocument doc)
        {
            XmlDeclaration xmlDeclaration = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
            XmlElement root = doc.DocumentElement;
            doc.InsertBefore(xmlDeclaration, root);
        }

        /// <summary>
        /// Emits <!DOCTYPE gxl SYSTEM "http://www.gupro.de/GXL/gxl-1.0.dtd">
        /// </summary>
        /// <param name="doc">the XML document in which to create the XML node</param>
        private static void AppendDOCType(XmlDocument doc)
        {
            XmlDocumentType xmlDocumentType = doc.CreateDocumentType("gxl", null, "http://www.gupro.de/GXL/gxl-1.0.dtd", null);
            doc.AppendChild(xmlDocumentType);
        }

        /// <summary>
        /// Appends and returns an XML child
        ///    <gxl xmlns="http://www.w3.org/1999/xlink">
        ///    ...
        ///    </gxl>
        /// </summary>
        /// <param name="doc">the XML document in which to create the XML node</param>
        /// <returns>the created XML node</returns>
        private static XmlElement AppendGXL(XmlDocument doc)
        {
            XmlElement gxl = doc.CreateElement(null, "gxl", "http://www.w3.org/1999/xlink");
            doc.AppendChild(gxl);
            return gxl;
        }

        /// <summary>
        /// Appends and returns an XML child
        ///    <graph id="N" edgeids="true" xmlns="">
        ///    ...
        ///    </graph>
        /// where N = graph.Name.
        /// 
        /// Note: attributes of the graph are not emitted.
        /// </summary>
        /// <param name="doc">the XML document in which to create the XML node</param>
        /// <param name="gxl">the parent of the XML child to be created</param>
        /// <param name="graph">the graph whose name is to be emitted</param>
        /// <returns></returns>
        private static XmlElement AppendGraph(XmlDocument doc, XmlElement gxl, Graph graph)
        {
            XmlElement graphNode = doc.CreateElement("graph");
            graphNode.SetAttribute("id", graph.Name);
            graphNode.SetAttribute("edgeids", "true");
            gxl.AppendChild(graphNode);

            // TODO: Do we need to save the graph's attributes?
            return graphNode;
        }

        /// <summary>
        /// Appends all nodes of the given <paramref name="graph"/> to <paramref name="parentXMLNode"/>
        /// as XML nodes with all their attributes as XML children nodes.
        /// </summary>
        /// <param name="doc">the XML document in which to create the XML node</param>
        /// <param name="parentXMLNode"></param>
        /// <param name="graph"></param>
        /// <returns>a mapping from graph node ID onto node IDs used in the GXL file</returns>
        private static Dictionary<string, string> AppendNodes(XmlDocument doc, XmlElement parentXMLNode, Graph graph)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();

            int nodeCount = 1;
            foreach (Node node in graph.Nodes())
            {
                XmlElement xmlNode = doc.CreateElement("node");
                string ID = "N" + nodeCount.ToString();
                result[node.ID] = ID;
                xmlNode.SetAttribute("id", ID);

                AppendType(doc, xmlNode, node);
                AppendAttributes(doc, xmlNode, node);
                parentXMLNode.AppendChild(xmlNode);

                nodeCount++;
            }
            return result;
        }

        /// <summary>
        /// Appends all edges of the given <paramref name="graph"/> to <paramref name="parentXMLNode"/>
        /// as XML nodes with all their attributes as XML children nodes.
        /// </summary>
        /// <param name="doc">the XML document in which to create the XML node</param>
        /// <param name="parentXMLNode">the parent XML where to add the XML nodes generated here</param>
        /// <param name="graph">the graph containing the nodes whose parent-child relation is to be emitted</param>
        /// <param name="graphNodeIdsToGXLNodeIds">a mapping from graph node IDs onto node IDs used in the GXL file</param>
        private static void AppendDependencyEdges
            (XmlDocument doc,
            XmlElement parentXMLNode,
            Graph graph,
            Dictionary<string, string> graphNodeIdsToGXLNodeIds)
        {
            foreach (Edge edge in graph.Edges())
            {
                string type = edge.Type;
                string source = graphNodeIdsToGXLNodeIds[edge.Source.ID];
                string target = graphNodeIdsToGXLNodeIds[edge.Target.ID];

                XmlElement xmlNode = AppendEdge(doc, parentXMLNode, edge.ID, type, source, target);
                AppendAttributes(doc, xmlNode, edge);
            }
        }

        /// <summary>
        /// Appends an edge from <paramref name="source"/> to <paramref name="target"/> with the 
        /// given <paramref name="type"/> to <paramref name="parentXMLNode"/> as an XML node.
        /// No edge attributes are appended.
        /// </summary>
        /// <param name="doc">the XML document in which to create the XML node</param>
        /// <param name="parentXMLNode">the parent XML where to add the XML nodes generated here</param>
        /// <param name="id">the GXL index for the next edge to be added</param>
        /// <param name="type">the type name of the edge</param>
        /// <param name="source">GXL node ID of the source of the edge</param>
        /// <param name="target">GXL node ID of the target of the edge</param>
        private static XmlElement AppendEdge
            (XmlDocument doc,
             XmlElement parentXMLNode,
             string id,
             string type,
             string source,
             string target)
        {
            XmlElement xmlNode = doc.CreateElement("edge");
            xmlNode.SetAttribute("id", id);
            xmlNode.SetAttribute("from", source);
            xmlNode.SetAttribute("to", target);
            AppendType(doc, xmlNode, type);
            parentXMLNode.AppendChild(xmlNode);
            return xmlNode;
        }

        /// <summary>
        /// Appends the parent-child relation for node of the given <paramref name="graph"/> to <paramref name="parentXMLNode"/>
        /// as XML nodes.
        /// </summary>
        /// <param name="doc">the XML document in which to create the XML node</param>
        /// <param name="parentXMLNode">the parent XML where to add the XML nodes generated here</param>
        /// <param name="graph">the graph containing the nodes whose parent-child relation is to be emitted</param>
        /// <param name="graphNodeIDsToGXLNodeIDs">a mapping from node ID onto node IDs used in the GXL file</param>
        /// <param name="edgeCount">the GXL index for the next hierarchical edge to be added</param>
        private static void AppendChildren
            (XmlDocument doc,
            XmlElement parentXMLNode,
            Graph graph,
            Dictionary<string, string> graphNodeIDsToGXLNodeIDs,
            string hierarchicalEdgeType,
            ref int edgeCount)
        {
            foreach (Node child in graph.Nodes())
            {
                Node parent = child.Parent;
                if (parent != null)
                {
                    string source = graphNodeIDsToGXLNodeIDs[child.ID];
                    string target = graphNodeIDsToGXLNodeIDs[parent.ID];
                    XmlElement xmlNode = AppendEdge(doc, parentXMLNode, "E" + edgeCount.ToString(), hierarchicalEdgeType, source, target);
                    edgeCount++;
                }
            }
        }

        /// <summary>
        /// Emits <type xlink:href="T"/> where T is the type of <paramref name="graphElement"/>.
        /// </summary>
        /// <param name="doc">the XML document in which to create the XML node</param>
        /// <param name="xmlNode">the XML node this attribute description should be appended</param>
        /// <param name="graphElement">graph elements whose type is to be emitted</param>
        private static void AppendType(XmlDocument doc, XmlElement xmlNode, GraphElement graphElement)
        {
            AppendType(doc, xmlNode, graphElement.Type);
        }

        /// <summary>
        /// Emits <type xlink:href="T"/> where T is the given <paramref name="graphElementType"/>.
        /// </summary>
        /// <param name="doc">the XML document in which to create the XML node</param>
        /// <param name="xmlNode">the XML node this attribute description should be appended</param>
        /// <param name="graphElementType">graph elements whose type is to be emitted</param>
        private static void AppendType(XmlDocument doc, XmlElement xmlNode, string graphElementType)
        {
            XmlElement type = doc.CreateElement("type");
            XmlAttribute xlinkHref = doc.CreateAttribute("xlink", "href", "http://www.w3.org/1999/xlink");
            xlinkHref.Value = graphElementType;
            type.SetAttributeNode(xlinkHref);
            //Note: type.SetAttribute("xlink:href", graphElement.Type) does not work; it swallows the xlink
            xmlNode.AppendChild(type);
        }

        /// <summary>
        /// Appends all attributes of <paramref name="attributable"/> to <paramref name="xmlNode"/>.
        /// </summary>
        /// <param name="doc">the XML document in which to create the XML node</param>
        /// <param name="xmlNode">the XML node this attribute description should be appended</param>
        /// <param name="type">the type name of the attribute</param>
        /// <param name="attributable">the attributable whose attributes are to be appended to <paramref name="xmlNode"/></param>
        private static void AppendAttributes(XmlDocument doc, XmlElement xmlNode, Attributable attributable)
        {
            // Emits for each toggle attribute A:
            // <attr name="A">
            //   <enum/>
            // </attr>
            foreach (string attribute in attributable.ToggleAttributes)
            {
                XmlElement attr = doc.CreateElement("attr");
                attr.SetAttribute("name", attribute);
                XmlElement value = doc.CreateElement("enum");
                attr.AppendChild(value);
                xmlNode.AppendChild(attr);
            }
            AppendAttributes<string>(doc, xmlNode, "string", attributable.StringAttributes, StringToString);
            AppendAttributes<float>(doc, xmlNode, "float", attributable.FloatAttributes, FloatToString);
            AppendAttributes<int>(doc, xmlNode, "int", attributable.IntAttributes, IntToString);
        }

        /// <summary>
        /// A specification for delegates converting a value into a string.
        /// </summary>
        /// <typeparam name="T">the type of the value</typeparam>
        /// <param name="value">the value to be converted</param>
        /// <returns>value as a string</returns>
        private delegate string AsString<T>(T value);

        /// <summary>
        /// Implementation of delegate AsString for string values. No conversion is needed here.
        /// </summary>
        /// <param name="value">the value to be converted</param>
        /// <returns>value as a string</returns>
        private static string StringToString(string value)
        {
            return value;
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

        /// <summary>
        /// Implementation of delegate AsString for int values. The CultureInfo.InvariantCulture
        /// is used as expected in a GXL file.
        /// </summary>
        /// <param name="value">the value to be converted</param>
        /// <returns>value as a string</returns>
        private static string IntToString(int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Emits for each attribute A in <paramref name="attributes"/> with value V:
        ///   <attr name="A">
        ///      <T>V</T>
        ///   </attr>
        /// where T = <paramref name="type"/>.
        /// </summary>
        /// <typeparam name="V"></typeparam>
        /// <param name="doc">the XML document in which to create the XML node</param>
        /// <param name="xmlNode">the XML node this attribute description should be appended</param>
        /// <param name="type">the type name of the attribute</param>
        /// <param name="attributes">the attributes whose description is to be appended to <paramref name="xmlNode"/></param>
        /// <param name="AsString">the delegate to convert each attribute value into a string</param>
        private static void AppendAttributes<V>
            (XmlDocument doc,
            XmlElement xmlNode,
            string type,
            Dictionary<string, V> attributes,
            AsString<V> AsString)
        {
            foreach (KeyValuePair<string, V> attribute in attributes)
            {
                XmlElement attr = doc.CreateElement("attr");
                attr.SetAttribute("name", attribute.Key);
                XmlElement value = doc.CreateElement(type);
                value.InnerText = AsString(attribute.Value);
                attr.AppendChild(value);
                xmlNode.AppendChild(attr);
            }
        }
    }
}