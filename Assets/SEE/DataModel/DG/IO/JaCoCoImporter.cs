using SEE.DataModel.DG;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;
using UnityEngine;

namespace Assets.SEE.DataModel.DG.IO
{
    /// <summary>
    /// Structure to define a searchKey in a dictionary to find nodes.
    /// </summary>
	public readonly struct NodeKey
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

        public override readonly bool Equals(object obj)
        {
            if (obj is not NodeKey other)
            {
                return false;
            }

            return NodeType == other.NodeType &&
                   FileName == other.FileName &&
                   SourceLine == other.SourceLine;
        }

        public override readonly string ToString()
        {
            return $"{NodeType}@{FileName}:{SourceLine}";
        }

        public override readonly int GetHashCode()
        {
            return ToString().GetHashCode();
        }
    }

    /// <summary>
    /// This class implements everything that is necessary to read a JaCoCo test report in
    /// XML and adds the metrics to the graph nodes.
    /// </summary>
    public class JaCoCoImporter : IDisposable
    {
        /// <summary>
        /// Starts reading a JaCoCo test report in XML given by <paramref name="filepath"/>.
        /// Test-Metrics will be added to nodes of graph.
        /// </summary>
        /// <param name="graph">Graph where to add the metrics</param>
        /// <param name="filepath">Path of the XM file</param>
        public static void StartReadingTestXML(Graph graph, string filepath)
        {
            IDictionary<NodeKey, Node> nodeDictionary = GetAllNodes(graph);

            try
            {
                XmlReaderSettings settings = new() { DtdProcessing = DtdProcessing.Parse };

                using XmlReader xmlReader = XmlReader.Create(filepath, settings);
                Node NodeToAddMetric = null;

                string XMLClassPath = null; // Class-Path named in XML, e.g. CodeFacts/CountConsonants
                string XMLClassFile = null; // Class-File named in XML, e.g. CountConsonants.java
                string nodeType = null; // Type of element to add the metric, e.g. package
                int XMLMethodLine = -1; // Sourceline named in the XML, -1 if not set
                string XMLpackageName = null; // Name of the current package

                // Note: A report clause is the outermost XML clause and may have immediate
                // counter clauses itself. E.g.:
                //  <counter type = "INSTRUCTION" missed = "1395" covered = "494" />
                //  <counter type = "BRANCH" missed = "110" covered = "22" />
                //  <counter type = "LINE" missed = "351" covered = "100" />
                //  <counter type = "COMPLEXITY" missed = "102" covered = "37" />
                //  <counter type = "METHOD" missed = "47" covered = "26" />
                //  <counter type = "CLASS" missed = "5" covered = "7" />

                // Stack to store the node type. This can be any of Report, Package, Class, or Method.
                // The inner-most node type is at the top.
                Stack<string> nodeTypeStack = new();

                bool inSourcefile = false; // indicator for sourcefile-tag in the xml to avoid setting this counters

                // Starts reading the XML tag by tag and checks what NodeType the tag has
                while (xmlReader.Read())
                {
                    switch (xmlReader.NodeType)
                    {
                        case XmlNodeType.Element:
                            if (xmlReader.Name == "report"
                                || xmlReader.Name == "package"
                                || xmlReader.Name == "class"
                                || xmlReader.Name == "method")
                            {
                                // Sets the name for the current package
                                if (xmlReader.Name == "package")
                                {
                                    XMLpackageName = xmlReader.GetAttribute("name");
                                }
                                // Sets the classfile and path for the current class
                                else if (xmlReader.Name == "class")
                                {
                                    // Attribute sourcefilename consists of the simple filename including the
                                    // file extension ".java" but excluding the directories this file is
                                    // contained in.
                                    XMLClassFile = xmlReader.GetAttribute("sourcefilename");
                                    // Attribute name consists of the fully qualified class name where
                                    // a slash / is used as a separator.
                                    XMLClassPath = xmlReader.GetAttribute("name") + ".java";
                                }
                                // Sets the line where a method is found by JaCoCo
                                else if (xmlReader.Name == "method")
                                {
                                    // Attribute line refers to the line in which the opening curly bracket
                                    // of the method's body occurs within its source file.
                                    XMLMethodLine = Int32.Parse(xmlReader.GetAttribute("line"));
                                }
                                // Here we assume that the XML clause's name corresponds to our
                                // graph node types where our node types' names start with a capital
                                // letter. E.g., XML clause "method" corresponds to node type "Method".
                                // "Report" will be pushed, too, even though we do not have such a
                                // node type. We will take of that below.
                                nodeTypeStack.Push(FirstCharUpper(xmlReader.Name));
                            }
                            // skip sourcefile and its counter
                            else if (xmlReader.Name == "sourcefile")
                            {
                                // From now on we are in the section where the executed lines are listed
                                // in the XML report.
                                inSourcefile = true;
                                continue;
                            }
                            // read metrics-counter and create NodeKey dependiing on nodeType
                            else if (!inSourcefile && xmlReader.Name == "counter")
                            {
                                // A clause counter may occur both nested in a sourcefile clause and
                                // in a class clause. When we arrive here, the counter is one in
                                // a class clause.
                                nodeType = nodeTypeStack.Peek();

                                NodeKey currentKey = nodeType switch
                                {
                                    "Report" => new NodeKey(null, null, -1),
                                    "Package" => new NodeKey(nodeType, XMLpackageName, -1),
                                    "Class" => new NodeKey(nodeType, XMLClassPath, -1),
                                    "Method" => new NodeKey(nodeType, XMLClassPath, XMLMethodLine),
                                    _ => throw new NotImplementedException($"Unexpected node type {nodeType}")
                                };

                                // find Node with before created NodeKey and set metrics directly or calculate percentage and then set
                                if (nodeDictionary.TryGetValue(currentKey, out NodeToAddMetric))
                                {
                                    float missed = float.Parse(xmlReader.GetAttribute("missed"), CultureInfo.InvariantCulture.NumberFormat);
                                    float covered = float.Parse(xmlReader.GetAttribute("covered"), CultureInfo.InvariantCulture.NumberFormat);
                                    string metricNamePrefix = "Metric." + xmlReader.GetAttribute("type");

                                    NodeToAddMetric.SetFloat(metricNamePrefix + "_missed", missed);
                                    NodeToAddMetric.SetFloat(metricNamePrefix + "_covered", covered);
                                    float percentage = covered + missed > 0 ? covered / (covered + missed) * 100 : 0;
                                    NodeToAddMetric.SetFloat(metricNamePrefix + "_percentage", percentage);
                                }
                                else
                                {
                                    Debug.LogError($"Setting metric failed for: {currentKey}.\n");
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
                Debug.LogError($"An error occurred: {ex.Message}.\n");
            }
        }

        /// <summary>
        /// Collects all nodes of <paramref name="graph"/> and adds them to the
        /// resulting dictionary.
        /// </summary>
        /// <param name="graph">Graph where to add metrics later.</param>
        /// <returns>Returns all nodes in a dictionary.</returns>
        private static IDictionary<NodeKey, Node> GetAllNodes(Graph graph)
        {
            Dictionary<NodeKey, Node> result = new();

            // iterate over every node in given graph and store it with key in dictionary
            foreach (Node node in graph.Nodes())
            {
                try
                {
                    if (node.Filename() != null && !result.ContainsValue(node))
                    {
                        if (node.Type == "Class")
                        {
                            result.Add(new NodeKey(node.Type, CreateUniquePath(node.Path()) + node.SourceFile, -1), node);
                        }
                        else if (node.Type == "Method")
                        {
                            // The SourceLine of a graph node refers to the point where its identifier
                            // occurs in the declaration. The line attribute in the test XML report
                            // refers to the point of the opening curly brackets of the method's body.
                            // Because of this shift we add an entry for each line in the range between
                            // the start and end of the method.
                            // FIXME: I am not sure whether this is actually a good idea.
                            // What will happen for a huge project?
                            int  methodStart = (int)node.SourceLine();
                            int methodEnd = methodStart + (int)node.SourceLength();

                            for (int i = methodStart; i < methodEnd; i++)
                            {
                                result.Add(new NodeKey(node.Type, CreateUniquePath(node.Path()) + node.SourceFile, i), node);
                            }
                        }
                    }
                    else if (node.Type == "Package")
                    {
                        result.Add(new NodeKey(node.Type, node.SourceName, -1), node);
                    }
                    else if (node.IsRoot()) // root of the project
                    {
                        result.Add(new NodeKey(null, null, -1), node);
                    }
                }
                catch (Exception ex)
                {
                    if (result.ContainsValue(node))
                    {
                        Debug.Log($"Node {node} was already added.\n");
                    }
                    else if (node.Filename() == null)
                    {
                        Debug.Log($"Filename of {node} was null and node type was neither a package nor was it the root (Report).\n");
                    }
                    else if (node.Type == "Method" && node.SourceLine() == null)
                    {
                        Debug.Log($"Method {node} has an undefined Sourceline.\n");
                    }
                    else
                    {
                        Debug.Log($"Node {node} can't be added to Dictionary; {ex.Message}.\n");
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Returns <paramref name="input"/> with its first character in upper case.
        /// If <paramref name="input"/> is <c>null</c> or empty, the empty string
        /// is returned.
        /// </summary>
        /// <param name="input">String that needs a uppercase first char</param>
        /// <returns>Capitalized string</returns>
        public static string FirstCharUpper(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }
            return input[0].ToString().ToUpper() + input[1..];
        }

        /// <summary>
        /// Delete "src/main/java/" in given string to make in comperable with Filepath in XML-Report.
        /// </summary>
        /// <param name="path">Path where the "src/main/java/" needs to be deleted</param>
        /// <returns>Manipulated string</returns>
        public static string CreateUniquePath(string path)
        {
            return path.Replace("src/main/java/", "");
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}