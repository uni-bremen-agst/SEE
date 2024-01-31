using SEE.DataModel.DG.SourceRange;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;
using UnityEngine;

namespace SEE.DataModel.DG.IO
{
    /// <summary>
    /// This class implements everything that is necessary to read a JaCoCo test report in
    /// XML and adds the metrics to the graph nodes.
    /// </summary>
    internal class JaCoCoImporter
    {
        /// <summary>
        /// A report XML node is currently processed.
        /// </summary>
        private const string reportContext = "report";
        /// <summary>
        /// A package XML node is currently processed.
        /// </summary>
        private const string packageContext = "package";
        /// <summary>
        /// A class XML node is currently processed.
        /// </summary>
        private const string classContext = "class";
        /// <summary>
        /// A method XML node is currently processed.
        /// </summary>
        private const string methodContext = "method";

        /// <summary>
        /// Loads a JaCoCo test report from the given JaCoCo XML <paramref name="filepath"/>.
        /// The retrieved coverage metrics will be added to nodes of <paramref name="graph"/>.
        /// </summary>
        /// <param name="graph">Graph where to add the metrics</param>
        /// <param name="filepath">Path of the XML file</param>
        public static void Load(Graph graph, string filepath)
        {
            if (graph == null)
            {
                throw new ArgumentNullException("Graph must not be null.");
            }
            if (graph.GetRoots().Count == 0)
            {
                // graph is empty. Nothing to do.
                return;
            }
            if (string.IsNullOrEmpty(filepath))
            {
                throw new ArgumentException("File path must neither be null nor empty.");
            }
            if (!File.Exists(filepath))
            {
                Debug.LogError($"The JaCoCo XML file named {filepath} does not exist.\n");
                return;
            }

            SourceRangeIndex index = new(graph);

            try
            {
                XmlReaderSettings settings = new() { DtdProcessing = DtdProcessing.Parse };
                using XmlReader xmlReader = XmlReader.Create(filepath, settings);

                // The fully qualified name of the package currently processed.
                // The name is retrieved from JaCoCo's XML report, where
                // a forward slash / is used as a separator, e.g., mypackage/mysubpackage.
                // A package name may be empty (in cases where the default package was
                // intended by a developer).
                string packageName = string.Empty;

                // The fully qualified name of the currently processed class in JaCoCo's XML report,
                // where a foward slash / is used as a separator, e.g., CodeFacts/CountConsonants.
                //
                // The qualifiedClassName contains the name of the path prefixed by the
                // packages it is contained in. In Java, the name of a source file is
                // the name of the main class in this file, appended by the file extension ".java".
                // A Java file, however, may have other classes - inner classes as well as classes
                // at the top level of the file. JaCoCo will report both kinds of non-main classes
                // with the same filename (obviously) as the filename of the main class. Only the
                // qualified name of non-main classes will differ. For instance, if we have a main
                // class C in package P, the filename will be C.java and the qualified name will
                // be P/C. If there is another class D in C.java that is not the main class,
                // the file of that class will be C.java, too, and its qualified name will be
                // P/D. If there is another class I nested in class C, the file of I will again
                // be C.java, but its qualified name will be P/D$I. The delimiter $ is used
                // to separate inner classes from the classes they are nested in.
                string qualifiedClassName = null;

                // The name of the source-code file for a class in JaCoCo's report,
                // e.g. CountConsonants.java. This filename is apparently always a
                // non-qualified name, that is, the directories this file is contained in
                // are not part of this name.
                string sourceFilename = null;

                // Source line as retrieved from JaCoCo's XML report, -1 if not set.
                //
                // Note that classes do not have a source line in JaCoCo's XML report,
                // only methods have.
                int sourceLine = -1;

                // Type of the XML node in JaCoCo's report currently processed, that is, the
                // one to add the metrics, to. This can be any of reportContext, packageContext,
                // classContext, or methodContext.
                string nodeType = null;

                // Note: A report clause is the outermost XML node and may have immediate
                // counter clauses itself. E.g.:
                //  <counter type = "INSTRUCTION" missed = "1395" covered = "494" />
                //  <counter type = "BRANCH" missed = "110" covered = "22" />
                //  <counter type = "LINE" missed = "351" covered = "100" />
                //  <counter type = "COMPLEXITY" missed = "102" covered = "37" />
                //  <counter type = "METHOD" missed = "47" covered = "26" />
                //  <counter type = "CLASS" missed = "5" covered = "7" />

                // Stack to store the XML node type, that is, the values of nodeType.
                // The inner-most XML node type is at the top. This gives us the context
                // we are currently working in. The XML nodes may be nested deeply.
                Stack<string> nodeTypeStack = new();

                // True if the currently processed XML node type is "sourcefile". If true, the
                // read metrics will be ignored.
                bool inSourcefile = false;

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
                                    // Attribute 'name' consists of the fully qualified class name where
                                    // a slash / is used as a separator.
                                    qualifiedClassName = xmlReader.GetAttribute("name");
                                }
                                else if (xmlReader.Name == methodContext)
                                {
                                    // Attribute line refers to the line in which the opening curly bracket
                                    // of the method's body occurs within its source file.
                                    sourceLine = Int32.Parse(xmlReader.GetAttribute("line"));
                                }
                                else if (xmlReader.Name == packageContext)
                                {
                                    packageName = xmlReader.GetAttribute("name");
                                    if (string.IsNullOrWhiteSpace(packageName))
                                    {
                                        Debug.LogWarning($"{XMLSourcePosition(filepath, xmlReader)}: "
                                             + "Data for the default Java package (without name) were given. These will be ignored.\n");
                                    }
                                }
                                nodeTypeStack.Push(xmlReader.Name);
                            }
                            // skip sourcefile and its counter XML nodes
                            else if (xmlReader.Name == "sourcefile")
                            {
                                // From now on we are in the section where the executed lines are listed
                                // in the XML report.
                                inSourcefile = true;
                                continue;
                            }
                            else if (!inSourcefile && xmlReader.Name == "counter")
                            {
                                // An XML node 'counter' may occur both nested in a sourcefile clause and
                                // in a class clause. When we arrive here, the counter is one in
                                // a class clause. These counters are processed and added to a graph node.
                                nodeType = nodeTypeStack.Peek();

                                try
                                {
                                    if (nodeType == packageContext)
                                    {
                                        if (string.IsNullOrWhiteSpace(packageName))
                                        {
                                            // There is no chance to add metrics if we have an empty link name.
                                            // It is not an error, though, because a developer could have nested
                                            // a class within the default package, which does not have a name.
                                            // If we encounter this case, we have already reported it above.
                                        }
                                        else
                                        {
                                            // Packages have no source range and, hence, are not represented in the
                                            // source-range index. We need to use a different approach to retrieve their
                                            // node from the graph.
                                            // We do that via the unique ID of a package node, which is assumed to be
                                            // the fully qualified name of the packages where individual packages are
                                            // separated by a period.
                                            AddMetricsToClassOrPackage(graph, xmlReader, packageName);
                                        }
                                    }
                                    else if (nodeType == classContext)
                                    {
                                        // JaCoCo uses a similar way to represent the qualified name of a class
                                        // as how our graph does for unique IDs for classes - except that JaCoCo
                                        // uses / as a delimiter between simple names and our graph uses a period
                                        // as a delimiter. Also inner classes are named equally: both JaCoCo and
                                        // our unique IDs for graph nodes uses $ to separate the name of the inner
                                        // class from its nesting class. That allows us to retrieve classes directly
                                        // from the graph without the need for source positions.
                                        // Note also that non-main classes in Java (top-level classes declared non-public
                                        // in a file which already declares a public top-level class) will not cause any
                                        // problem. For instance, if a non-main class C were declared in a file X.java
                                        // which declares a main class X contained in package P, there cannot be another
                                        // file declaring a main class C as a sibling to X within P in the package hierarchy.
                                        // Both would have the name P.C in our graph. Yet, that would be illegal Java
                                        // code and, hence, cannot happen.
                                        AddMetricsToClassOrPackage(graph, xmlReader, qualifiedClassName);
                                    }
                                    else if (nodeType == reportContext)
                                    {
                                        // We add all metrics reported at the report level to the root of the graph.
                                        // A non-empty graph has always a root node.
                                        // Note that we might override the values of another node -- happened to be the
                                        // root -- that we processed previously and for which we added metrics.
                                        AddMetrics(xmlReader, graph.GetRoots()[0]);
                                    }
                                    else if (index.TryGetValue(GetPath(qualifiedClassName, sourceFilename),
                                                               sourceLine, out Node nodeToAddMetrics))
                                    {
                                        AddMetrics(xmlReader, nodeToAddMetrics);
                                    }
                                    else
                                    {
                                        Debug.LogError($"{XMLSourcePosition(filepath, xmlReader)}: "
                                            + $"No node found for: {qualifiedClassName}:{sourceLine}  [{nodeType}].\n");
                                    }
                                }
                                catch (Exception e)
                                {
                                    Debug.LogError($"{XMLSourcePosition(filepath, xmlReader)}: {e.Message}.\n");
                                    throw;
                                }
                            }
                            break;

                        // re-sets attributes to default when tag is closed
                        case XmlNodeType.EndElement:
                            if (xmlReader.Name == reportContext
                                || xmlReader.Name == packageContext
                                || xmlReader.Name == classContext
                                || xmlReader.Name == methodContext)
                            {
                                // Only for the XML nodes listed in the condition above, we pushed a context.
                                nodeTypeStack.Pop();

                                if (xmlReader.Name == classContext)
                                {
                                    qualifiedClassName = null;
                                    sourceFilename = null;
                                }
                                else if (xmlReader.Name == methodContext)
                                {
                                    sourceLine = -1;
                                }
                                else if (xmlReader.Name == packageContext)
                                {
                                    packageName = string.Empty;
                                }
                            }
                            else if (xmlReader.Name == "sourcefile")
                            {
                                inSourcefile = false;
                            }
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"An error occurred: {ex.Message}.\n");
            }

            // Retrieves the counter metrics from xmlReader and adds them to nodeToAddMetrics
            static void AddMetrics(XmlReader xmlReader, Node nodeToAddMetrics)
            {
                int missed = int.Parse(xmlReader.GetAttribute("missed"), CultureInfo.InvariantCulture.NumberFormat);
                int covered = int.Parse(xmlReader.GetAttribute("covered"), CultureInfo.InvariantCulture.NumberFormat);

                string metricNamePrefix = "Metric." + xmlReader.GetAttribute("type");

                nodeToAddMetrics.SetInt(metricNamePrefix + "_missed", missed);
                nodeToAddMetrics.SetInt(metricNamePrefix + "_covered", covered);

                float percentage = covered + missed > 0 ? covered / (covered + missed) * 100 : 0;
                nodeToAddMetrics.SetFloat(metricNamePrefix + "_percentage", percentage);
            }

            // Retrieves the counter metrics from xmlReader and adds them to a package or class
            // node retrieved from the given graph having the given uniqueID.
            // Note: the actually used node ID is uniqueID where every / is replaced by a period.
            void AddMetricsToClassOrPackage(Graph graph, XmlReader xmlReader, string uniqueID)
            {
                // JaCoCo uses "/" as a separator for packages and classes while our graph is
                // assumed to use a period "." to separate package/class names in unique IDs.
                Node packageOrClassNode = graph.GetNode(uniqueID.Replace("/", "."));
                if (packageOrClassNode != null)
                {
                    AddMetrics(xmlReader, packageOrClassNode);
                }
                else
                {
                    Debug.LogError($"{XMLSourcePosition(filepath, xmlReader)}: No node found for package/class {uniqueID}.\n");
                }
            }
        }

        /// <summary>
        /// Returns the source position of the XML code currently processed by <paramref name="xmlReader"/>.
        /// The position is reported as <paramref name="filepath"/>:line:column. In case no source position
        /// can be retrieved <paramref name="filepath"/>:<unknown> will returned.
        /// </summary>
        /// <param name="filepath">name of the XML file currently processed</param>
        /// <param name="xmlReader">the XML reader processing the XML file</param>
        /// <returns>the source position of the XML file</returns>
        private static string XMLSourcePosition(string filepath, XmlReader xmlReader)
        {
            string position = "<unknown>";

            if (xmlReader is IXmlLineInfo xmlLineInfo && xmlLineInfo.HasLineInfo())
            {
                position = xmlLineInfo.LineNumber.ToString() + ":"
                    + xmlLineInfo.LinePosition.ToString();
            }

            return $"{filepath}:{position}";
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
