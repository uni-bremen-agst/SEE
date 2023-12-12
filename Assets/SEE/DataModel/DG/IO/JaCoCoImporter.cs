using SEE.DataModel.DG;
using System;
using System.Xml;

namespace Assets.SEE.DataModel.DG.IO
{
    //TODO Find Node to add the Metrics


    /// <summary>
    /// Reads a testreport from a xml file and add information to GraphElement/Nodes. This class should be similar to GraphReader.
    /// </summary>
    public class JaCoCoImporter : IDisposable
    {
        // graph where to add the XML-Testreport information
        private Graph graph;

        // current used Graphelement to add the attribute information
        private GraphElement currentElement;



        // Filepath of the xml-file which has the testreport
        private String filepath;

        /// <summary>
        /// Constructor for JaCoCoImporter
        /// </summary>
        /// <param name="filepath"></param>
        /// <param name="graph"></param>
        public JaCoCoImporter(string filepath, Graph graph)
        {
            this.filepath = filepath;
            this.graph = graph;
        }

        /// <summary>
        /// Reading the given xml to include Test-Metrics in SEE
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="filepath"></param>
        protected void StartReading(Graph graph, String filepath )
        {
            try
            {
                // Add XmlReaderSettings with active DTD-Processing
                XmlReaderSettings settings = new XmlReaderSettings();
                settings.DtdProcessing = DtdProcessing.Parse;

                // XMLReader for parsing named file
                using (XmlReader xmlReader = XmlReader.Create(filepath, settings))
                {
                    string currentClassName = null;
                    string currentMethodName = null;
                    string classCounterType = null;
                    int currentMethodLine = -1;

                    // Iterate over file
                    while (xmlReader.Read())
                    {
                        switch (xmlReader.NodeType)
                        {
                            case XmlNodeType.Element:
                                // find class
                                if (xmlReader.Name == "class")
                                {
                                    // store class name
                                    currentClassName = xmlReader.GetAttribute("name");
                                    //Console.WriteLine($"Class: {currentClassName}");
                                }
                                // find method
                                else if (currentClassName != null && xmlReader.Name == "method")
                                {
                                    // store method name
                                    currentMethodName = xmlReader.GetAttribute("name");
                                    currentMethodLine = Int32.Parse(xmlReader.GetAttribute("line")) - 1;
                                }
                                else if (currentClassName != null && xmlReader.Name == "counter")
                                {
                                    // Depth 3 is within report, package, class
                                    if (xmlReader.Depth == 3)
                                    {
                                        // Counter for class
                                        classCounterType = xmlReader.GetAttribute("type");
                                        string missed = xmlReader.GetAttribute("missed");
                                        string covered = xmlReader.GetAttribute("covered");
                                        Console.WriteLine($"    Class Counter für {currentClassName} Type: {classCounterType}, Missed: {missed}, Covered: {covered}");
                                        //TODO instead of ConsoleWrite find node with given attributes (class name - which contans package in xml), use node.setFloat for setting the Metric values
                                    }
                                    // depth 4 is within report, package, class, method
                                    else if (xmlReader.Depth == 4)
                                    {
                                        // Counter for methods in classes
                                        string counterType = xmlReader.GetAttribute("type");
                                        string missed = xmlReader.GetAttribute("missed");
                                        string covered = xmlReader.GetAttribute("covered");
                                        Console.WriteLine($"      Method Counter für {currentMethodName} in Line {currentMethodLine} Type: {counterType}, Missed: {missed}, Covered: {covered}");
                                        //TODO instead of ConsoleWrite find node with given attributes (class name - which contans package in xml + Line), use node.setFloat for setting the Metric values
                                    }
                                }
                                break;

                            case XmlNodeType.EndElement:
                                if (xmlReader.Name == "class")
                                {
                                    // leave class-Element and reset values
                                    currentClassName = null;
                                    classCounterType = null;
                                }
                                else if (xmlReader.Name == "method")
                                {
                                    // leave class-Element and reset values
                                    currentMethodName = null;
                                }
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"A Mistake appeard: {ex.Message}");
            }

        }

        /// <summary>
        /// Find Node where to add the metrics
        /// </summary>
        /// <param name="graph"></param> Graph which contains the node
        /// <param name="classPath"></param> Given classpath from xml
        /// <param name="methodeName"></param> Given methodeName from xml
        /// <param name="SourceLine"></param> Given SourceLine from method from xml
        /// <returns></returns>
        protected Node GetNode(Graph graph, String classPath, String methodeName, String SourceLine)
        {
            //TODO logic needs to implemented
            Node nodeToAddTestMetric = null;
            return nodeToAddTestMetric;
        }


        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}