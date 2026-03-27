using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;
using SEE.Utils;
using UnityEngine;
using Stream = System.IO.Stream;
using XmlElement = System.Xml.XmlElement;

namespace SEE.DataModel.DG.IO
{
    /// <summary>
    /// Saves graphs in GXL format on disk.
    /// </summary>
    public class GraphWriter: GraphIO
    {
        /// <summary>
        /// Saves given <paramref name="graph"/> in GXL format in a file with given <paramref name="filename"/>.
        /// The parent-child relation between nodes is stored as edges with the type <paramref name="hierarchicalEdgeType"/>.
        /// The attributes of the <paramref name="graph"/> itself are not stored to stay compatible
        /// with the GXL files by Axivion.
        /// </summary>
        /// <param name="filename">Name of the file where to store the graph.</param>
        /// <param name="graph">Graph to be stored.</param>
        /// <param name="hierarchicalEdgeType">Edge type for the node hierarchy.</param>
        public static void Save(string filename, Graph graph, string hierarchicalEdgeType)
        {
            XmlDocument doc = new();

            AppendXMLVersion(doc);
            AppendDOCType(doc);
            XmlElement gxl = AppendGXL(doc);
            XmlElement graphNode = AppendGraph(doc, gxl, graph);
            Dictionary<string, string> nodeIDsToGxlIds = AppendNodes(doc, graphNode, graph);
            AppendDependencyEdges(doc, graphNode, graph, nodeIDsToGxlIds);
            int hierarchicalEdgeCount = 1;
            AppendChildren(doc, graphNode, graph, nodeIDsToGxlIds, hierarchicalEdgeType, ref hierarchicalEdgeCount);
            try
            {
                WriteFile(filename, doc);
            }
            catch (Exception e)
            {
                Debug.LogError($"Could not save graph to GXL file '{filename}' due to: {e.Message}.\n");
                throw;
            }
        }

        /// <summary>
        /// Writes the GXL file represented by <paramref name="doc"/> to the file with
        /// the given <paramref name="filename"/>.
        /// Note that this may compress the file via LZMA, depending on the extension indicated
        /// by <paramref name="filename"/>.
        /// </summary>
        /// <param name="filename">Name of the file to save to.</param>
        /// <param name="doc">GXL content to save.</param>
        private static void WriteFile(string filename, XmlDocument doc)
        {
            // Transfer XMLDocument to a stream.
            using Stream source = new MemoryStream();
            doc.Save(source);
            source.Flush();
            source.Position = 0;

            Compressor.Save(filename, source);
        }

        /// <summary>
        /// Emits <?xml version="1.0" encoding="UTF-8"?>
        /// </summary>
        /// <param name="doc">The XML document in which to create the XML node.</param>
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
            XmlDocumentType xmlDocumentType = doc.CreateDocumentType("gxl", null, "https://www.gupro.de/GXL/gxl-1.0.dtd", null);
            doc.AppendChild(xmlDocumentType);
        }

        /// <summary>
        /// Appends and returns an XML child
        ///    <gxl xmlns="http://www.w3.org/1999/xlink">
        ///    ...
        ///    </gxl>
        /// </summary>
        /// <param name="doc">The XML document in which to create the XML node.</param>
        /// <returns>The created XML node.</returns>
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
        /// <param name="doc">The XML document in which to create the XML node.</param>
        /// <param name="gxl">The parent of the XML child to be created.</param>
        /// <param name="graph">The graph whose name is to be emitted.</param>
        /// <returns>.</returns>
        private static XmlElement AppendGraph(XmlDocument doc, XmlNode gxl, Graph graph)
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
        /// <param name="doc">The XML document in which to create the XML node.</param>
        /// <param name="parentXMLNode">.</param>
        /// <param name="graph">.</param>
        /// <returns>A mapping from graph node ID onto node IDs used in the GXL file.</returns>
        private static Dictionary<string, string> AppendNodes(XmlDocument doc, XmlNode parentXMLNode, Graph graph)
        {
            Dictionary<string, string> result = new();

            int nodeCount = 1;
            foreach (Node node in graph.Nodes())
            {
                XmlElement xmlNode = doc.CreateElement("node");
                string id = $"N{nodeCount}";
                result[node.ID] = id;
                xmlNode.SetAttribute("id", id);

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
        /// <param name="doc">The XML document in which to create the XML node.</param>
        /// <param name="parentXMLNode">The parent XML where to add the XML nodes generated here.</param>
        /// <param name="graph">The graph containing the nodes whose parent-child relation is to be emitted.</param>
        /// <param name="graphNodeIdsToGXLNodeIds">A mapping from graph node IDs onto node IDs used in the GXL file.</param>
        private static void AppendDependencyEdges(XmlDocument doc,
                                                  XmlNode parentXMLNode,
                                                  Graph graph,
                                                  IReadOnlyDictionary<string, string> graphNodeIdsToGXLNodeIds)
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
        /// <param name="doc">The XML document in which to create the XML node.</param>
        /// <param name="parentXMLNode">The parent XML where to add the XML nodes generated here.</param>
        /// <param name="id">The GXL index for the next edge to be added.</param>
        /// <param name="type">The type name of the edge.</param>
        /// <param name="source">GXL node ID of the source of the edge.</param>
        /// <param name="target">GXL node ID of the target of the edge.</param>
        private static XmlElement AppendEdge(XmlDocument doc,
                                             XmlNode parentXMLNode,
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
        /// <param name="doc">The XML document in which to create the XML node.</param>
        /// <param name="parentXMLNode">The parent XML where to add the XML nodes generated here.</param>
        /// <param name="graph">The graph containing the nodes whose parent-child relation is to be emitted.</param>
        /// <param name="graphNodeIDsToGXLNodeIDs">A mapping from node ID onto node IDs used in the GXL file.</param>
        /// <param name="edgeCount">The GXL index for the next hierarchical edge to be added.</param>
        private static void AppendChildren(XmlDocument doc,
                                           XmlNode parentXMLNode,
                                           Graph graph,
                                           IReadOnlyDictionary<string, string> graphNodeIDsToGXLNodeIDs,
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
                    XmlElement xmlNode = AppendEdge(doc, parentXMLNode, $"E{edgeCount}", hierarchicalEdgeType, source, target);
                    edgeCount++;
                }
            }
        }

        /// <summary>
        /// Emits <type xlink:href="T"/> where T is the type of <paramref name="graphElement"/>.
        /// </summary>
        /// <param name="doc">The XML document in which to create the XML node.</param>
        /// <param name="xmlNode">The XML node this attribute description should be appended.</param>
        /// <param name="graphElement">Graph elements whose type is to be emitted.</param>
        private static void AppendType(XmlDocument doc, XmlElement xmlNode, GraphElement graphElement)
        {
            AppendType(doc, xmlNode, graphElement.Type);
        }

        /// <summary>
        /// Emits <type xlink:href="T"/> where T is the given <paramref name="graphElementType"/>.
        /// </summary>
        /// <param name="doc">The XML document in which to create the XML node.</param>
        /// <param name="xmlNode">The XML node this attribute description should be appended.</param>
        /// <param name="graphElementType">Graph elements whose type is to be emitted.</param>
        private static void AppendType(XmlDocument doc, XmlNode xmlNode, string graphElementType)
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
        /// <param name="doc">The XML document in which to create the XML node.</param>
        /// <param name="xmlNode">The XML node this attribute description should be appended.</param>
        /// <param name="type">The type name of the attribute.</param>
        /// <param name="attributable">The attributable whose attributes are to be appended to <paramref name="xmlNode"/>.</param>
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
            Dictionary<string, int> intAttributes = new(attributable.IntAttributes);
            // In SEE, we use the SourceRange attribute to denote ranges, which works with an explicit end line.
            // In the Axivion Suite, the Source.Region_Length and Source.Region_Start attributes are used instead.
            // We hence need to convert the attributes to be Axivion-compatible here.
            // We will not consider character ranges, as they are not used in the Axivion Suite.
            if (attributable.TryGetRange(GraphElement.SourceRangeAttribute, out Range range))
            {
                intAttributes.Add(RegionStartAttribute, range.StartLine);
                intAttributes.Add(RegionLengthAttribute, range.Lines);
                // We remove these two attributes from the intAttributes dictionary to avoid duplication.
                attributable.IntAttributes.Remove(GraphElement.SourceRangeAttribute + Attributable.RangeStartLineSuffix);
                attributable.IntAttributes.Remove(GraphElement.SourceRangeAttribute + Attributable.RangeEndLineSuffix);
            }

            AppendAttributes(doc, xmlNode, "string", attributable.StringAttributes, StringToString);
            AppendAttributes(doc, xmlNode, "float", attributable.FloatAttributes, FloatToString);
            AppendAttributes(doc, xmlNode, "int", intAttributes, IntToString);
        }

        /// <summary>
        /// Implementation of delegate AsString for string values. No conversion is needed here.
        /// </summary>
        /// <param name="value">The value to be converted.</param>
        /// <returns>Value as a string.</returns>
        private static string StringToString(string value)
        {
            return value;
        }

        /// <summary>
        /// Implementation of delegate AsString for float values. The CultureInfo.InvariantCulture
        /// is used as expected in a GXL file.
        /// </summary>
        /// <param name="value">The value to be converted.</param>
        /// <returns>Value as a string.</returns>
        private static string FloatToString(float value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Implementation of delegate AsString for int values. The CultureInfo.InvariantCulture
        /// is used as expected in a GXL file.
        /// </summary>
        /// <param name="value">The value to be converted.</param>
        /// <returns>Value as a string.</returns>
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
        /// <param name="doc">The XML document in which to create the XML node.</param>
        /// <param name="xmlNode">The XML node this attribute description should be appended.</param>
        /// <param name="type">The type name of the attribute.</param>
        /// <param name="attributes">The attributes whose description is to be appended to <paramref name="xmlNode"/>.</param>
        /// <param name="asString">The delegate to convert each attribute value into a string.</param>
        private static void AppendAttributes<V>(XmlDocument doc,
                                                XmlNode xmlNode,
                                                string type,
                                                Dictionary<string, V> attributes,
                                                Func<V, string> asString)
        {
            foreach ((string key, V value) in attributes)
            {
                XmlElement attr = doc.CreateElement("attr");
                attr.SetAttribute("name", key);
                XmlElement valueElement = doc.CreateElement(type);
                valueElement.InnerText = asString(value);
                attr.AppendChild(valueElement);
                xmlNode.AppendChild(attr);
            }
        }
    }
}
