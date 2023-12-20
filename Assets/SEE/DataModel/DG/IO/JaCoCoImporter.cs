using SEE.DataModel.DG;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;

namespace Assets.SEE.DataModel.DG.IO
{
    //TODO Find Node to add the Metrics


    /// <summary>
    /// Reads a testreport from a xml file and add information to GraphElement/Nodes. This class should be similar to GraphReader.
    /// </summary>
    public class JaCoCoImporter : IDisposable
    {

        /// <summary>
        /// Reading the given xml to include Test-Metrics in SEE
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="filepath"></param>
        public static void StartReading(Graph graph, String filepath )
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
					string currentClassFile = null;
					string currentName = null;
					string nodeType = null;
					int currentMethodLine = -1;
					Stack<Node> myStack = new Stack<Node>();
					bool inSource = false;

					// Iterate over file
					while (xmlReader.Read())
					{
						switch (xmlReader.NodeType)
						{
							case XmlNodeType.Element:
								if (xmlReader.Name != "counter" && xmlReader.Name != "sourcefile" && xmlReader.Name != "line" && xmlReader.Name != "sessioninfo")
								{
									if (xmlReader.Name == "class")
									{
										currentClassFile = xmlReader.GetAttribute("sourcefilename");
										currentClassName = xmlReader.GetAttribute("name");
									}
									if (xmlReader.Name == "method")
									{
										currentMethodLine = Int32.Parse(xmlReader.GetAttribute("line"));
									}
									nodeType = xmlReader.Name;
									currentName = xmlReader.GetAttribute("name");
									// hier den Node suchen
									myStack.Push(GetNode(graph, nodeType, currentClassName, currentClassFile, currentMethodLine));
								}
								else if (xmlReader.Name == "sourcefile")
								{
									inSource = true;
									continue;
								}

								else if (!inSource && xmlReader.Name == "counter")
								{
									// setFloat on Node
									if(myStack.Peek() != null)
                                    {
										myStack.Peek().SetFloat("Metric." + xmlReader.GetAttribute("type") + "_missed", float.Parse(xmlReader.GetAttribute("missed"), CultureInfo.InvariantCulture.NumberFormat));
										myStack.Peek().SetFloat(xmlReader.GetAttribute("type") + "_covered", float.Parse(xmlReader.GetAttribute("covered"), CultureInfo.InvariantCulture.NumberFormat));
										//Console.WriteLine("Metrik für " + myStack.Peek() + xmlReader.GetAttribute("type") + " missed: " + xmlReader.GetAttribute("missed") + " covered: " + xmlReader.GetAttribute("covered"));
									}
								}
								break;

							case XmlNodeType.EndElement:
								if (xmlReader.Name == "class")
								{
									currentClassName = null;
								}
								if (xmlReader.Name == "method")
								{
									currentMethodLine = -1;
								}
								if (xmlReader.Name == "sourcefile" || xmlReader.Name == "line" || xmlReader.Name == "sessioninfo")
								{
									if (xmlReader.Name == "sourcefile")
									{
										inSource = false;
									}
									continue;
								}
								myStack.Pop();
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
		/// Find Node to add Test-Metrics
		/// </summary>
		/// <param name="graph"></param>
		/// <param name="nodeType"></param>
		/// <param name="currentClassName"></param>
		/// <param name="currentClassFile"></param>
		/// <param name="currentMethodLine"></param>
		/// <returns></returns>
        protected static Node GetNode(Graph graph, string nodeType, string currentClassName, string currentClassFile, int currentMethodLine)
        {
			// TODO Filename needs to change to a unique indifier
			foreach (var node in graph.Nodes())
            {
				if (nodeType == "class" && currentMethodLine == -1)
				{
                    if (node.Type == "Class")
                    {
						if (currentClassFile.Equals(node.Filename()))
						{
							return node;
						}
                    }
				}
				if (nodeType == "method")
				{
					if (node.Type == "Method")
					{
						if (currentClassFile.Equals(node.Filename()) && currentMethodLine.Equals(node.SourceLine()))
						{
							return node;
						}
					}
				}
				if (nodeType == "package")
				{
					if (node.Type == "Package")
					{
						// needs to be implemented
						return null;
					}
				}
				if (nodeType == "")
				{
					if (node.Type == "report")
					{
						return null;
					}
				}

			}
            return null;
        }


        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}