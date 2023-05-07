using System.Globalization;
using System.Reflection;
using System.Xml;

namespace profiling2gxl
{
    /// <summary>
    /// Saves graphs in GXL format on disk.
    /// </summary>
    internal static class GXLWriter
    {
        /// <summary>
        /// Saves given <paramref name="functions"/> in GXL format in a file with given <paramref name="filename"/>.
        /// The parent-child relation between nodes is stored as edges with the type <paramref name="hierarchicalEdgeType"/>.
        /// </summary>
        /// <param name="filename">name of the file where to store the GXL file</param>
        /// <param name="functions">functions to be stored</param>
        /// <param name="graphname">id to set in the graph node</param>
        /// <param name="hierarchicalEdgeType">edge type for the node hierarchy</param>
        public static void Save(string filename, List<Function> functions, string graphname, string hierarchicalEdgeType = "Call")
        {
            XmlDocument doc = new();

            AppendXMLVersion(doc);
            AppendDOCType(doc);
            XmlElement gxl = AppendGXL(doc);
            XmlElement graphNode = AppendGraph(doc, gxl, graphname);
            Dictionary<string, string> nodeIDs_To_GXLids = AppendNodes(doc, graphNode, functions);
            var classes = functions.Where(f => !string.IsNullOrEmpty(f.Module)).ToList().Select(f => new Function() { Id = f.Module, Name = f.Module }).GroupBy(f => f.Id).Select(f => f.First()).ToList();
            Dictionary<string, string> classNodeIDs_To_GXLids = AppendNodes(doc, graphNode, classes, "Class", nodeIDs_To_GXLids.Count + 1);
            foreach (var kv in classNodeIDs_To_GXLids)
            {
                nodeIDs_To_GXLids[kv.Key] = kv.Value;
            }
            int hierarchicalEdgeCount = 1;
            foreach (Function primaryFunction in functions)
            {
                AppendChildren(doc, graphNode, primaryFunction, nodeIDs_To_GXLids, hierarchicalEdgeType, ref hierarchicalEdgeCount);
            }
            foreach (var kv in classNodeIDs_To_GXLids)
            {
                var classNode = new Function() { Id = kv.Key };
                functions.Where(f => f.Module == classNode.Id).ToList().ForEach(f => classNode.Children.Add(new(f.Id)));
                AppendChildren(doc, graphNode, classNode, nodeIDs_To_GXLids, "Belongs_To", ref hierarchicalEdgeCount);
            }
            try
            {
                doc.Save(filename);
            }
            catch (Exception e)
            {
                Console.WriteLine("Could not save graph to GXL file '{0}' due to: {1}.\n", filename, e.Message);
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
            XmlElement? root = doc.DocumentElement;
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
        /// where N = name.
        /// 
        /// Note: attributes of the graph are not emitted.
        /// </summary>
        /// <param name="doc">the XML document in which to create the XML node</param>
        /// <param name="gxl">the parent of the XML child to be created</param>
        /// <param name="name">the name who is to be emitted</param>
        /// <returns></returns>
        private static XmlElement AppendGraph(XmlDocument doc, XmlElement gxl, string name)
        {
            XmlElement graphNode = doc.CreateElement("graph");
            graphNode.SetAttribute("id", name);
            graphNode.SetAttribute("edgeids", "true");
            gxl.AppendChild(graphNode);

            // TODO: Do we need to save the graph's attributes?
            return graphNode;
        }

        /// <summary>
        /// Appends all <paramref name="functions"/> to <paramref name="parentXMLNode"/>
        /// as XML nodes with all their attributes as XML children nodes.
        /// </summary>
        /// <param name="doc">the XML document in which to create the XML node</param>
        /// <param name="parentXMLNode">the parent of the XML child to be created</param>
        /// <param name="functions">the functions to be stored</param>
        /// <param name="type">the type name of the node</param>
        /// <param name="nodeCount">the node counter to start from, will be used to create the id of the stored node</param>
        /// <returns>a mapping from graph node ID onto node IDs used in the GXL file</returns>
        private static Dictionary<string, string> AppendNodes(XmlDocument doc, XmlElement parentXMLNode, List<Function> functions, String type = "Method", int nodeCount = 1)
        {
            Dictionary<string, string> result = new ();

            foreach (Function node in functions)
            {
                XmlElement xmlNode = doc.CreateElement("node");
                string ID = "N" + nodeCount.ToString();
                result[node.Id] = ID;
                xmlNode.SetAttribute("id", ID);

                AppendType(doc, xmlNode, type);
                AppendAttributes(doc, xmlNode, node);
                parentXMLNode.AppendChild(xmlNode);

                nodeCount++;
            }
            return result;
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
        /// <param name="parent">the parent function whose parent-child relation is to be emitted</param>
        /// <param name="graphNodeIDsToGXLNodeIDs">a mapping from node ID onto node IDs used in the GXL file</param>
        /// <param name="hierarchicalEdgeType">the GXL index for the next hierarchical edge to be added</param>
        /// <param name="edgeCount">edge counter to start from, will be used to create the id of the stored edge</param>
        private static void AppendChildren
            (XmlDocument doc,
            XmlElement parentXMLNode,
            Function parent,
            Dictionary<string, string> graphNodeIDsToGXLNodeIDs,
            string hierarchicalEdgeType,
            ref int edgeCount)
        {
            string source = graphNodeIDsToGXLNodeIDs[parent.Id];
            foreach (var child in parent.Children)
            {
                string target = graphNodeIDsToGXLNodeIDs[child.Id];
                if (hierarchicalEdgeType == "Belongs_To")
                {
                    XmlElement xmlNode = AppendEdge(doc, parentXMLNode, "E" + edgeCount.ToString(), hierarchicalEdgeType, target, source);
                } else
                {
                    XmlElement xmlNode = AppendEdge(doc, parentXMLNode, "E" + edgeCount.ToString(), hierarchicalEdgeType, source, target);
                }
                edgeCount++;
            }
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
            xmlNode.AppendChild(type);
        }

        /// <summary>
        /// Appends all attributes of <paramref name="function"/> to <paramref name="xmlNode"/>.
        /// </summary>
        /// <param name="doc">the XML document in which to create the XML node</param>
        /// <param name="xmlNode">the XML node this attribute description should be appended</param>
        /// <param name="function">the function whose attributes are to be appended to <paramref name="xmlNode"/></param>
        private static void AppendAttributes(XmlDocument doc, XmlElement xmlNode, Function function)
        {
            Dictionary<string, string> stringAttributes = new ();
            Dictionary<string, float> floatAttributes = new ();
            Dictionary<string, int> intAttributes = new ();
            foreach (PropertyInfo property in function.GetType().GetProperties())
            {
                if (property.PropertyType == typeof(string))
                {
                    string value = (string)property.GetValue(function);
                    stringAttributes.Add($"Metric.{Helper.PascalToSnakeCase(property.Name)}", value);
                }
                if (property.PropertyType == typeof(float))
                {
                    float value = (float)property.GetValue(function);
                    floatAttributes.Add($"Metric.{Helper.PascalToSnakeCase(property.Name)}", value);
                }
                if (property.PropertyType == typeof(int))
                {
                    int value = (int)property.GetValue(function);
                    intAttributes.Add($"Metric.{Helper.PascalToSnakeCase(property.Name)}", value);
                }
            }

            stringAttributes.Add("Source.Name", function.Name);
            stringAttributes.Add("Linkage.Name", function.Id);
            stringAttributes.Add("Source.Path", function.Path);
            stringAttributes.Add("Source.File", function.Filename);

            AppendAttributes(doc, xmlNode, "string", stringAttributes, StringToString);
            AppendAttributes(doc, xmlNode, "float", floatAttributes, FloatToString);
            AppendAttributes(doc, xmlNode, "int", intAttributes, IntToString);
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