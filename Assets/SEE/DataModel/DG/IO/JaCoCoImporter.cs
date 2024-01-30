using SEE.DataModel.DG.SourceRange;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;
using UnityEngine;

namespace SEE.DataModel.DG.IO
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
    public class JaCoCoImporter
    {
        private const string reportContext = "report";
        private const string classContext = "class";
        private const string packageContext = "package";
        private const string methodContext = "method";

        /// <summary>
        /// Starts reading a JaCoCo test report in XML given by <paramref name="filepath"/>.
        /// Test-Metrics will be added to nodes of graph.
        /// </summary>
        /// <param name="graph">Graph where to add the metrics</param>
        /// <param name="filepath">Path of the XM file</param>
        public static void StartReadingTestXML(Graph graph, string filepath)
        {
            // IDictionary<NodeKey, Node> nodeDictionary = GetAllNodes(graph);
            SourceRangeIndex index = new(graph);

            try
            {
                XmlReaderSettings settings = new() { DtdProcessing = DtdProcessing.Parse };
                using XmlReader xmlReader = XmlReader.Create(filepath, settings);

                // The fully qualified name of a class in JaCoCo's XML report, where
                // a foward slash / is used as a separator, e.g., CodeFacts/CountConsonants.
                string qualifiedClassName = null;

                // The name of the source-code file for a class in JaCoCo's report,
                // e.g. CountConsonants.java. This filename is apparently always a
                // non-qualified name, that is, the directories this file is contained in
                // is not part of this name.
                string sourceFilename = null;

                // Source line as retrieved from JaCoCo's XML report, -1 if not set.
                //
                // Note that classes do not have a source line in JaCoCo's XML report,
                // only methods have. Yet, we need a source line to look up the node
                // in the source-range index. That is why we do not reset the source
                // line whose processing has finished. Instead we keep its value and
                // re-use it to look up the class. Because methods are necessarily 
                // nested in a class, we can as well use this line to look up the
                // class. Note also that there is always at least one method
                // for each class -- even if the developer did not code one.
                // If a developer did not write a method, the artificially generated
                // constructor <init> will exist and will have a source line.
                //
                // That is, if the value of sourceLine is different from -1, it may relate
                // to the currently processed method or the method just processed last.
                int sourceLine = -1;

                // Type of the element in JaCoCo's report currently processed, that is, the
                // one to add the metrics, to. This can be any of "Report", "Package",
                // "Class", or "Method".
                string nodeType = null;

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
                            if (xmlReader.Name == reportContext
                                || xmlReader.Name == packageContext
                                || xmlReader.Name == classContext
                                || xmlReader.Name == methodContext)
                            {
                                if (xmlReader.Name == classContext)
                                {
                                    // Attribute sourcefilename consists of the simple filename including the
                                    // file extension ".java" but excluding the directories this file is
                                    // contained in.
                                    sourceFilename = xmlReader.GetAttribute("sourcefilename");
                                    // Attribute name consists of the fully qualified class name where
                                    // a slash / is used as a separator.
                                    qualifiedClassName = xmlReader.GetAttribute("name") + ".java";
                                }
                                // Sets the line where a method is found by JaCoCo
                                else if (xmlReader.Name == methodContext)
                                {
                                    // Attribute line refers to the line in which the opening curly bracket
                                    // of the method's body occurs within its source file.
                                    sourceLine = Int32.Parse(xmlReader.GetAttribute("line"));
                                }
                                // Here we assume that the XML clause's name corresponds to our
                                // graph node types where our node types' names start with a capital
                                // letter. E.g., XML clause "method" corresponds to node type "Method".
                                // "Report" will be pushed, too, even though we do not have such a
                                // node type. We will take of that below.
                                nodeTypeStack.Push(xmlReader.Name);
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

                                // The qualifiedClassName contains the name of the path prefixed by the
                                // packages it is contained in. In Java, the name of a source file is
                                // the name of the main class in this file, appended by the file extension ".java".
                                // A Java file, however, may have other classes - inner classes as well as classes
                                // at the top level of the file. JaCoCo will report both kinds of non-main classes
                                // with the same filename (obviously) as the filename of the main class. Only the
                                // qualified name of non-main classes will differ. For instance, if we have a main
                                // class C in package P, the filename will be C.java and the qualified name will
                                // be P/C. If there is another class D in C.java, that is not the main class, 
                                // the file of that class will be C.java, too, and its qualified name will be
                                // P/D. If there is another class I nested in class C, the file of I will again
                                // be C.java, but its qualified name will be P/D$I. The delimiter $ is used
                                // to separate inner classes from the classes they are nested in.
                                // To get the project-relative path, we thus need to replace the last word
                                // of the qualifiedClassName by the sourceFilename. That is whay GetPath() does.
                                try
                                {
                                    if (nodeType == packageContext)
                                    {
                                        // FIXME: We need to add metrics to the package node.
                                        // Package nodes do not have a source line.
                                    } else if (index.TryGetValue(GetPath(qualifiedClassName, sourceFilename), sourceLine, out Node nodeToAddMetric))
                                    {
                                        Debug.Log($"Adding metrics to node {nodeToAddMetric.ID} {qualifiedClassName}:{sourceLine} [{nodeType}].\n");

                                        float missed = float.Parse(xmlReader.GetAttribute("missed"), CultureInfo.InvariantCulture.NumberFormat);
                                        float covered = float.Parse(xmlReader.GetAttribute("covered"), CultureInfo.InvariantCulture.NumberFormat);
                                        string metricNamePrefix = "Metric." + xmlReader.GetAttribute("type");

                                        nodeToAddMetric.SetFloat(metricNamePrefix + "_missed", missed);
                                        nodeToAddMetric.SetFloat(metricNamePrefix + "_covered", covered);
                                        float percentage = covered + missed > 0 ? covered / (covered + missed) * 100 : 0;
                                        nodeToAddMetric.SetFloat(metricNamePrefix + "_percentage", percentage);
                                    }
                                    else
                                    {
                                        Debug.LogError($"No node found for: {qualifiedClassName}:{sourceLine}  [{nodeType}].\n");
                                    }
                                }
                                catch
                                {
                                    string position = "<unknown>";

                                    if (xmlReader is IXmlLineInfo xmlLineInfo && xmlLineInfo.HasLineInfo())
                                    {
                                        position = "line " + xmlLineInfo.LineNumber.ToString() + " and column " + xmlLineInfo.LinePosition.ToString();
                                    }

                                    Debug.LogError($"Error at: {position}.\n");
                                    throw;
                                }
                            }
                            break;

                        // set attributes to default when tag is closed
                        case XmlNodeType.EndElement:
                            if (xmlReader.Name == classContext)
                            {
                                qualifiedClassName = null;
                                sourceFilename = null;
                            }
                            else if (xmlReader.Name == methodContext)
                            {
                                // We do not reset sourceLine for the reasons stated above. We need it for
                                // for looking up the class whose method was just processed.
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
        /// A qualified name is a set of words separated by delimiter /.
        /// This method returns the qualified name given in <paramref name="qualifiedClassName"/>
        /// where its last word is replaced by <paramref name="sourceFilename"/>.
        /// For instance, let the qualified name be A/B and F be the source file name,
        /// then A/F is returned. If the qualified name were only A, just F would be returned.
        /// </summary>
        /// <param name="qualifiedClassName">qualified name to be processed</param>
        /// <param name="sourceFilename">source filename to be appended</param>
        /// <returns>qualified name whose last word is replaced by <paramref name="sourceFilename"/>
        /// </returns>
        /// <exception cref="ArgumentException">thrown in case <paramref name="qualifiedClassName"/>
        /// is null or empty</exception>
        private static string GetPath(string qualifiedClassName, string sourceFilename)
        {
            const char jacocoSeparator = '/';
            if (string.IsNullOrEmpty(qualifiedClassName))
            {
                throw new ArgumentException("The qualified name of a class must not be empty.");
            }

            int lastSeparatorPosition = qualifiedClassName.LastIndexOf(jacocoSeparator);
            if (lastSeparatorPosition == -1)
            {
                return sourceFilename;
            }
            else
            {
                return qualifiedClassName.Remove(lastSeparatorPosition) + jacocoSeparator + sourceFilename;
            }
        }
    }
}
