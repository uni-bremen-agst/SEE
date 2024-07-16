using Cysharp.Threading.Tasks;
using SEE.Utils.Paths;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;
using SEE.DataModel.DG.GraphIndex;
using UnityEngine;

namespace SEE.DataModel.DG.IO
{
    /// <summary>
    /// This class implements everything that is necessary to read a JaCoCo test report in
    /// XML and adds the metrics to the graph nodes.
    /// </summary>
    internal static class JaCoCoImporter
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
        /// Loads a JaCoCo test report from the given <paramref name="path"/> assumed to
        /// conform to the JaCoCo coverage report syntax.
        /// The retrieved coverage metrics will be added to nodes of <paramref name="graph"/>.
        /// </summary>
        /// <param name="graph">graph for which node metrics are to be imported</param>
        /// <param name="path">path to a data file containing JaCoCo data from which to import node metrics</param>
        /// <exception cref="ArgumentNullException">if <paramref name="graph"/> is null</exception>
        /// <exception cref="ArgumentException">if <paramref name="path"/> is null or empty</exception>
        public static async UniTask LoadAsync(Graph graph, DataPath path)
        {
            if (string.IsNullOrEmpty(path.Path))
            {
                throw new ArgumentException("Data path must neither be null nor empty.");
            }
            Stream stream = await path.LoadAsync();
            await LoadAsync(graph, stream, path.Path);
        }

        /// <summary>
        /// Loads a JaCoCo test report from the given <paramref name="stream"/>.
        /// The retrieved coverage metrics will be added to nodes of <paramref name="graph"/>.
        /// </summary>
        /// <param name="graph">Graph where to add the metrics</param>
        /// <param name="stream">where to read the JaCoCo input data from</param>
        /// <param name="jaCoCoFilename">the name of the original JaCoCo file; used only for reporting</param>
        /// <exception cref="ArgumentNullException">if <paramref name="graph"/> is null</exception>
        private static async UniTask LoadAsync(Graph graph, Stream stream, string jaCoCoFilename)
        {
            if (graph == null)
            {
                throw new ArgumentNullException(nameof(graph));
            }
            if (graph.GetRoots().Count == 0)
            {
                // graph is empty. Nothing to do.
                return;
            }

            SourceRangeIndex index = new(graph, IndexPath);

            XmlReaderSettings settings = new()
            {
                CloseInput = true,
                IgnoreWhitespace = true,
                IgnoreComments = true,
                Async = true,
                DtdProcessing = DtdProcessing.Parse
            };
            using XmlReader xmlReader = XmlReader.Create(stream, settings);

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

            // Name of the currently processed method. This value is used only for error messages.
            string methodName = string.Empty;

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
            string nodeType;

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

            while (await xmlReader.ReadAsync())
            {
                switch (xmlReader.NodeType)
                {
                    case XmlNodeType.Element:
                        if (xmlReader.Name is reportContext or packageContext or classContext or methodContext)
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
                                sourceLine = int.Parse(xmlReader.GetAttribute("line"));
                                methodName = xmlReader.GetAttribute("name");
                            }
                            else if (xmlReader.Name == packageContext)
                            {
                                packageName = xmlReader.GetAttribute("name");
                                if (string.IsNullOrWhiteSpace(packageName))
                                {
                                    Debug.LogWarning($"{XMLSourcePosition(jaCoCoFilename, xmlReader)}: "
                                         + "Data for the default Java package (without name) were given. These will be ignored.\n");
                                }
                            }
                            if (!xmlReader.IsEmptyElement)
                            {
                                // This is not a self-closing (empty) element, e.g., <item/>.
                                // Note: A corresponding EndElement node is not generated for empty elements.
                                // That is why we push a context onto the context stack only if the element is
                                // not self-closing.
                                nodeTypeStack.Push(xmlReader.Name);
                            }
                            else
                            {
                                Debug.LogWarning($"{XMLSourcePosition(jaCoCoFilename, xmlReader)}: "
                                       + "Report does not provide coverage data for this entity.\n");
                            }

                        }
                        // skip sourcefile and its counter XML nodes
                        else if (xmlReader.Name == "sourcefile")
                        {
                            // From now on we are in the section where the executed lines are listed
                            // in the XML report.
                            inSourcefile = true;
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
                                        AddMetricsToClassOrPackage(graph, xmlReader, packageName, jaCoCoFilename);
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
                                    AddMetricsToClassOrPackage(graph, xmlReader, qualifiedClassName, jaCoCoFilename);
                                }
                                else if (nodeType == reportContext)
                                {
                                    // We add all metrics reported at the report level to the root of the graph.
                                    // A non-empty graph has always a root node.
                                    // Note that we might override the values of another node -- happened to be the
                                    // root -- that we processed previously and for which we added metrics.
                                    AddMetrics(xmlReader, graph.GetRoots()[0]);
                                }
                                else if (index.TryGetValue(MainTypeName(AsJavaQualifiedName(qualifiedClassName), sourceFilename),
                                                           sourceLine, out Node nodeToAddMetrics))
                                {
                                    AddMetrics(xmlReader, nodeToAddMetrics);
                                }
                                else
                                {
                                    // We are in a method context.
                                    Debug.LogError($"{XMLSourcePosition(jaCoCoFilename, xmlReader)}: "
                                        + $"No node found for {nodeType} {AsJavaQualifiedName(qualifiedClassName)}.{methodName}:{sourceLine} "
                                        + $"using key {MainTypeName(AsJavaQualifiedName(qualifiedClassName), sourceFilename)}.\n");
                                }
                            }
                            catch (Exception e)
                            {
                                Debug.LogError($"{XMLSourcePosition(jaCoCoFilename, xmlReader)}: {e.Message}.\n");
                                throw;
                            }
                        }
                        break;

                    // re-sets attributes to default when tag is closed
                    case XmlNodeType.EndElement:
                        if (xmlReader.Name is reportContext or packageContext or classContext or methodContext)
                        {
                            // Only for the XML nodes listed in the condition above, we pushed a context.
                            nodeTypeStack.Pop();

                            if (xmlReader.Name == classContext)
                            {
                                qualifiedClassName = null;
                                sourceFilename = null;
                                methodName = string.Empty;
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
            return;

            // Retrieves the counter metrics from xmlReader and adds them to nodeToAddMetrics
            static void AddMetrics(XmlReader xmlReader, Node nodeToAddMetrics)
            {
                int missed = int.Parse(xmlReader.GetAttribute("missed")!, CultureInfo.InvariantCulture.NumberFormat);
                int covered = int.Parse(xmlReader.GetAttribute("covered")!, CultureInfo.InvariantCulture.NumberFormat);

                string metricNamePrefix = JaCoCo.Prefix + xmlReader.GetAttribute("type");

                nodeToAddMetrics.SetInt(metricNamePrefix + "_missed", missed);
                nodeToAddMetrics.SetInt(metricNamePrefix + "_covered", covered);

                float percentage = covered + missed > 0 ? (float)covered / (covered + missed) * 100 : 0;
                nodeToAddMetrics.SetFloat(metricNamePrefix + "_percentage", percentage);
            }

            // Retrieves the counter metrics from xmlReader and adds them to a package or class
            // node retrieved from the given graph having the given uniqueID.
            // Note: the actually used node ID is uniqueID where every / is replaced by a period.
            static void AddMetricsToClassOrPackage(Graph graph, XmlReader xmlReader, string uniqueID, string jaCoCoFilename)
            {
                // JaCoCo uses "/" as a separator for packages and classes while our graph is
                // assumed to use a period "." to separate package/class names in unique IDs.
                if (uniqueID == null)
                {
                    Debug.LogError($"{XMLSourcePosition(jaCoCoFilename, xmlReader)}: uniqueID is null.\n");
                }
                Node packageOrClassNode = graph.GetNode(uniqueID.Replace("/", "."));
                if (packageOrClassNode != null)
                {
                    AddMetrics(xmlReader, packageOrClassNode);
                }
                else
                {
                    Debug.LogError($"{XMLSourcePosition(jaCoCoFilename, xmlReader)}: No node found for package/class {uniqueID}.\n");
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
                position = $"{xmlLineInfo.LineNumber}:{xmlLineInfo.LinePosition}";
            }

            return $"{filepath}:{position}";
        }

        /// <summary>
        /// The character used to separate elements in a path. This separator is used
        /// both in the JaCoCo XML report for qualified class names and the prefix.
        /// </summary>
        private const char jacocoSeparator = '/';

        /// <summary>
        /// A qualified name in Java is a set of words separated by a period as a delimiter.
        /// JaCoCo, however, uses a forward slash as a delimiter. This method returns
        /// <paramref name="qualifiedJaCoCoTypeName"/> where each forward slash has
        /// been replaced by a period.
        /// </summary>
        /// <param name="qualifiedJaCoCoTypeName">qualified name in JaCoCo syntax to be converted</param>
        /// <returns><paramref name="qualifiedJaCoCoTypeName"/> where each forward slash was replaced
        /// by a period</returns>
        /// <exception cref="ArgumentException">thrown in case <paramref name="qualifiedJaCoCoTypeName"/>
        /// is null or empty</exception>
        private static string AsJavaQualifiedName(string qualifiedJaCoCoTypeName)
        {
            if (string.IsNullOrEmpty(qualifiedJaCoCoTypeName))
            {
                throw new ArgumentException("The qualified name of a class must not be empty.");
            }
            return qualifiedJaCoCoTypeName.Replace(jacocoSeparator, '.');
        }

        /// <summary>
        /// The name of all node types representing types in Java.
        /// </summary>
        private static readonly HashSet<string> typeNodeTypes
            = new() { "Class", "Interface", "Class_Template", "Interface_Template"};

        /// <summary>
        /// Yields the path name of <paramref name="node"/>.
        ///
        /// If <paramref name="node"/> is a type, that is, its <see cref="GraphElement.Type"/>
        /// is contained in <see cref="typeNodeTypes"/>, the fully qualified name of the
        /// main type corresponding to this type is returned.
        ///
        /// The fully qualified name of a main type is the name of the type including
        /// all the packages it is contained in, e.g., org.uni-bremen.mypackage.myclass
        /// for a class named myclass declared in package org.uni-bremen.mypackage. A
        /// period is used as a delimiter.
        ///
        /// What is a main type corresponding to a type? Java allows a file to declare multiple
        /// types in the same file even at the top level. Only one of those declared at top
        /// level may be public, however.
        /// If there is only one type declared at the top level, that type is the main type.
        /// If there are multiple types declared at the top level, the one declared as
        /// public is the main type.
        /// Other type declarations may be nested in types in Java. For inner (nested, not at
        /// the top level) types, the main type is the main type corresponding to the
        /// outer-most (top-level) type the inner type is contained in.
        ///
        /// For instance, if we have a file T.java with a main class T in package p
        /// and a non-main class Z which in turn contains a nested class W, then the
        /// result for T would be p.T, the result for Z would be p.T, too, the result for
        /// W would again be p.T.
        ///
        /// Note: The filename for a main type T must be T.java in Java. This fact allows
        /// us to distinguish the main type from other top-level types in a file.
        ///
        /// If <paramref name="node"/> is a method with <see cref="Node.Parent"/> p (obviously
        /// a type in which the method is declared), then <see cref="IndexPath(Node)"/> applied
        /// to p is returned. For instance, if W had a method m in the example above,
        /// then again p.T would be returned.
        ///
        /// For all other node types, null is returned.
        /// </summary>
        /// <param name="node">node whose fully qualified name is to be retrieved</param>
        /// <returns>fully qualified name of the main type for the given <paramref name="node"/></returns>
        private static string IndexPath(Node node)
        {
            if (node.Type == "Method")
            {
                // A Java method can be declared only within a type, thus, its
                // parent node must be a type.
                return IndexPath(node.Parent);
            }
            else if (typeNodeTypes.Contains(node.Type))
            {
                return MainTypeName(node.ID, node.Filename);
            }
            else
            {
                // Node types different from a type and method will be ignored.
                return null;
            }
        }

        /// <summary>
        /// Returns a fully qualified Java name for the main type corresponding to the
        /// given <paramref name="qualifiedJavaTypeName"/>.
        /// </summary>
        /// <param name="qualifiedJavaTypeName">fully qualified Java name of a type</param>
        /// <param name="filename">the Java filename in which the type is declared</param>
        /// <returns>fully qualified Java name for main type corresponding to the
        /// given <paramref name="qualifiedJavaTypeName"/></returns>
        private static string MainTypeName(string qualifiedJavaTypeName, string filename)
        {
            // The ID (Linkage.Name) of a main type Y declared in package p
            // is p.Y.
            //
            // Likewise, the ID of a type Z declared at top level, but
            // different from the main type, is p.Z where p is the package
            // the corresponding main type is declared in. Whether a top-level
            // type is the main type can be determined by checking the
            // source filename. A main type T is contained in a file named
            // T.java; if that is not the case, the type is not a main type.
            //
            // The ID of an inner type W nested in a type Z declared in a
            // package p is p.Z$W. The delimiter $ is used to separate inner
            // types from their containing type.

            string outerMostType = OuterMostType(qualifiedJavaTypeName);
            // outerMostType could denote a main type or another top-level
            // type that is not a main type. The two can be distinguished
            // using the source filename.
            (string parentName, string simpleName) = SimpleName(outerMostType);
            string typeAccordingToFilename = Path.GetFileNameWithoutExtension(filename);
            if (simpleName == typeAccordingToFilename)
            {
                // It is a main type.
                return outerMostType;
            }
            else
            {
                // It is a top-level type that is not the main type. The filename
                // gives us the name of the main type this type corresponds to.
                return parentName.Length == 0 ?
                    typeAccordingToFilename : parentName + "." + typeAccordingToFilename;
            }

            // If id does not contain the delimiter $, id is returned.
            // Otherwise the substring from the first character of id
            // until (and excluding) the first occurrence of the delimiter $
            // is returned.
            static string OuterMostType(string id)
            {
                // First occurrence of the delimiter for a nested type.
                int i = id.IndexOf('$');
                if (i == -1)
                {
                    // This type is already an outer-most type.
                    return id;
                }
                // Note: i == 0 is impossible; otherwise the type's name were only $.
                return id[..i];
            }

            // Returns the last name in the given qualified name.
            // The first element of the result is the fully qualified name
            // of the parent and the second element is the last simple name.
            static (string, string) SimpleName(string qualifiedName)
            {
                int i = qualifiedName.LastIndexOf(".", StringComparison.Ordinal);
                if (i == -1)
                {
                    return (string.Empty, qualifiedName);
                }
                else
                {
                    return (qualifiedName[..i], qualifiedName[..^i]);
                }
            }
        }
    }
}
