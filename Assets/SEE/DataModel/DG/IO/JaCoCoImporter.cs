using SEE.DataModel.DG;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;
using UnityEngine;

namespace Assets.SEE.DataModel.DG.IO
{
    /// <summary>
    /// Structur to define a searchKey in a dictionary to find nodes.
    /// </summary>
	public struct NodeKey
    {
        public string NodeType { get; }
        public string FileName { get; }
        public int SourceLine { get; }


        public NodeKey(string nodeType, string fileName, int sourceline)
        {
            NodeType = nodeType;
            FileName = fileName;
            SourceLine = sourceline;
        }

        public override bool Equals(object obj)
        {
            if (obj is not NodeKey other)
                return false;

            return NodeType == other.NodeType &&
                   FileName == other.FileName &&
                   SourceLine == other.SourceLine;
        }
    }

    /// <summary>
    /// This class implements everything that is necessary to read a JaCoCo testreport in xml-format and that the metrics to the graph nodes.
    /// </summary>
    public class JaCoCoImporter : IDisposable
    {
        /// <summary>
        /// Starts Reading a JaCoCo-Test in XML-format given under named filepath. Test-Metrics will be added to Nodes of graph.
        /// </summary>
        /// <param name="graph"></param> Graph where to add the metrics
        /// <param name="filepath"></param> Filepath where the XML-File is found
        public static void StartReadingTestXML(Graph graph, string filepath)
        {
            Dictionary<NodeKey, Node> nodeDictionary = GetAllNodes(graph); // Dictionary with all Nodes

            try
            {
                XmlReaderSettings settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Parse };

                using XmlReader xmlReader = XmlReader.Create(filepath, settings);
                NodeKey currentKey = default;
                Node NodeToAddMetric = null;

                string XMLClassPath = null; // Class-Path named in XML, e.g. CodeFacts/CountConsonants
                string XMLClassFile = null; // Class-File named in XML, e.g. CountConsonants.java
                string nodeType = null; // Type of element to add the metric, e.g. package
                int XMLMethodLine = -1; // Sourceline named in the XML, -1 if not set
                string XMLpackageName = null; // Name of the current package

                Stack<string> nodeTypeStack = new(); // Stack to store the nodeType
                bool inSourcefile = false; // indicator for sourcefile-tag in the xml to avoid setting this counters

                // Starts reading the XML tag by tag and checks what NodeType the tag has
                while (xmlReader.Read())
                {
                    switch (xmlReader.NodeType)
                    {
                        case XmlNodeType.Element:
                            if (xmlReader.Name != "counter" && xmlReader.Name != "sourcefile" && xmlReader.Name != "line" && xmlReader.Name != "sessioninfo")
                            {
                                // Sets the name for the current package
                                if (xmlReader.Name == "package")
                                {
                                    XMLpackageName = xmlReader.GetAttribute("name");
                                }
                                // Sets the classfile and path for the current class
                                else if (xmlReader.Name == "class")
                                {
                                    XMLClassFile = xmlReader.GetAttribute("sourcefilename");
                                    XMLClassPath = xmlReader.GetAttribute("name") + ".java";
                                }
                                // Sets the line where a method is found by JaCoCo
                                else if (xmlReader.Name == "method")
                                {
                                    XMLMethodLine = Int32.Parse(xmlReader.GetAttribute("line"));
                                }
                                nodeTypeStack.Push(FirstCharUpper(xmlReader.Name));
                            }
                            // skip sourcefile and its counter
                            else if (xmlReader.Name == "sourcefile")
                            {
                                inSourcefile = true;
                                continue;
                            }
                            // read metrics-counter and create NodeKey dependiing on nodeType
                            else if (!inSourcefile && xmlReader.Name == "counter")
                            {
                                nodeType = nodeTypeStack.Peek();
                                currentKey = nodeType switch
                                {
                                    "Report" => new NodeKey(null, null, -1),
                                    "Package" => new NodeKey(nodeType, XMLpackageName, -1),
                                    "Class" => new NodeKey(nodeType, XMLClassPath, -1),
                                    _ => new NodeKey(nodeType, XMLClassPath, XMLMethodLine) // method
                                };

                                try
                                {
                                    // find Node with before created NodeKey and set metrics directly or calculate percentage and then set
                                    NodeToAddMetric = nodeDictionary[currentKey];
                                    NodeToAddMetric.SetFloat("Metric." + xmlReader.GetAttribute("type") + "_missed", float.Parse(xmlReader.GetAttribute("missed"), CultureInfo.InvariantCulture.NumberFormat));
                                    NodeToAddMetric.SetFloat("Metric." + xmlReader.GetAttribute("type") + "_covered", float.Parse(xmlReader.GetAttribute("covered"), CultureInfo.InvariantCulture.NumberFormat));
                                    float percentage = float.Parse(xmlReader.GetAttribute("covered")) / (float.Parse(xmlReader.GetAttribute("covered")) + float.Parse(xmlReader.GetAttribute("missed"))) * 100;
                                    NodeToAddMetric.SetFloat("Metric." + xmlReader.GetAttribute("type") + "_percentage", percentage);
                                }
                                catch
                                {
                                    Debug.Log($"Setting metric failed for: {currentKey.NodeType + currentKey.FileName + currentKey.SourceLine}");
                                }
                            }
                            break;

                        // set attributes to default when tag is closed
                        case XmlNodeType.EndElement:
                            if (xmlReader.Name == "class")
                            {
                                XMLClassPath = null;
                                XMLClassFile = null;
                            }
                            else if (xmlReader.Name == "method")
                            {
                                XMLMethodLine = -1;
                            }
                            else if (xmlReader.Name == "sourcefile" || xmlReader.Name == "line" || xmlReader.Name == "sessioninfo")
                            {
                                if (xmlReader.Name == "sourcefile")
                                {
                                    inSourcefile = false;
                                }
                                continue;
                            }
                            nodeTypeStack.Pop();
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Log($"An error occurred: {ex.Message}");
            }
        }

        /// <summary>
        /// Collect all Nodes of Graph.
        /// </summary>
        /// <param name="graph"></param> Graph where to add Metrics later.
        /// <returns>Returns all Nodes in a Dictionary.</returns>
        protected static Dictionary<NodeKey, Node> GetAllNodes(Graph graph)
        {
            Dictionary<NodeKey, Node> NodeDictionary = new(); // Dictionary for all Nodes
            NodeKey key;
            int methodStart; // Sourceline where a method starts
            int methodEnd; // Sourceline where a method ends

            // iterate over every node in given graph and store it with key in dictionary
            foreach (Node node in graph.Nodes())
            {
                try
                {
                    if (node.Filename() != null && !NodeDictionary.ContainsValue(node))
                    {
                        if (node.Type == "Class")
                        {
                            key = new NodeKey(node.Type, CreateUniquePath(node.Path()) + node.SourceFile, -1);
                            NodeDictionary.Add(key, node);
                        }
                        else if (node.Type == "Method")
                        {
                            // because of shift in method-lines between Test-XML and graph there is a entry for each line in the range between start and end of method
                            methodStart = (int)node.SourceLine();
                            methodEnd = methodStart + (int)node.SourceLength();

                            for (int i = methodStart; i < methodEnd; i++)
                            {
                                key = new NodeKey(node.Type, CreateUniquePath(node.Path()) + node.SourceFile, i);
                                NodeDictionary.Add(key, node);
                            }
                        }
                    }
                    else if (node.Type == "Package")
                    {
                        key = new NodeKey(node.Type, node.SourceName, -1);
                        NodeDictionary.Add(key, node);
                    }
                    else if (node.Level == 0) // root of the project
                    {
                        key = new NodeKey(null, null, -1);
                        NodeDictionary.Add(key, node);
                    }
                }
                catch
                {
                    if(NodeDictionary.ContainsValue(node))
                    {
                        Debug.Log($"Node was already added. {node}");
                    }
                    else if (node.Filename() == null)
                    {
                        Debug.Log($"Filename was null and Node type was neither package nor was it the root. {node}");
                    }
                    else if (node.Type == "Method" && node.SourceLine() == null)
                    {
                        Debug.Log($"Method-Node but Sourceline was empty.{node}");
                    }
                    else { Debug.Log($"Node can't be added to Dictionary. {node}"); }

                }

            }
            return NodeDictionary;
        }

        /// <summary>
        /// Manipulate given String so the first Char is in Uppercase.
        /// </summary>
        /// <param name="input"></param> String that needs a uppercase first char
        /// <returns>Manipulated string</returns>
        public static string FirstCharUpper(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }
            return $"{input[0].ToString().ToUpper()}{input.Substring(1)}";
        }

        /// <summary>
        /// Delete "src/main/java/" in given string to make in comperable with Filepath in XML-Report.
        /// </summary>
        /// <param name="path"></param> Path where the "src/main/java/" needs to be deleted
        /// <returns>Manipulated string</returns>
		public static string CreateUniquePath(string path)
        {
            string inputString = path;
            string result;
            string pathToRemove = "src/main/java/";

            result = inputString.Replace(pathToRemove, "");
            return result;
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
