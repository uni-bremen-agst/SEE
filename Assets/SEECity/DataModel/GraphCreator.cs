using System;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.DataModel
{
    /// <summary>
    /// Reads a graph from a GXL file and returns it as Graph. Every graph entity (Graph, Node, Edge)
    /// is nested in an otherwise empty GameObject. All of those GameObjects have the same transform.
    /// </summary>
    public class GraphCreator : GXLParser
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="filename">the name of the GXL file</param>
        /// <param name="graph">the graph to which the entities found in the GXL are to be added</param>
        /// <param name="hierarchicalEdgeTypes">the set of edge-type names for edges considered to represent nesting</param>
        /// <param name="logger">the logger used for messages; if null, no messages are emitted</param>
        public GraphCreator(string filename, HashSet<string> hierarchicalEdgeTypes, ILogger logger = null) : base(filename, logger)
        {
            this.hierarchicalEdgeTypes = hierarchicalEdgeTypes;
        }

        /// <summary>
        /// Returns the graph after it was loaded. Load() must have been called before.
        /// </summary>
        /// <returns>loaded graph</returns>
        public Graph GetGraph()
        {
            return graph;
        }

        // Number of errors detected.
        private int errors = 0;

        /// <summary>
        /// Returns the number of errors found during loading the GXL.
        /// </summary>
        public int Errors
        {
            get
            {
                return errors;
            }
        }

        /// <summary>
        /// Logs the given error message using the logger and increments the
        /// error count.
        /// </summary>
        /// <param name="message">message to be logged</param>
        protected override void LogError(string message)
        {
            base.LogError(message);
            errors++;
        }

        // graph where to add the GXL information
        private Graph graph;

        // the previously added graph element (node or edge)
        private GraphElement current = null;

        // The current GameObject that is created for the current graph element.
        // The current graph element will be added as a component to the current 
        // game object.
        GameObject currentGameObject;

        // A mapping of the GXL node ids onto the graph nodes.
        private Dictionary<String, Node> nodes = new Dictionary<string, Node>();

        // The set of edge-type names for edges considered to represent nesting.
        private readonly HashSet<string> hierarchicalEdgeTypes = null;

        /// <summary>
        /// Sets the graph name using the attribute 'id'.
        /// </summary>
        protected override void StartGraph()
        {
            currentGameObject = new GameObject();
            graph = currentGameObject.AddComponent<Graph>();
            graph.Path = filename;
            if (reader.HasAttributes)
            {
                while (reader.MoveToNextAttribute())
                {
                    if (reader.Name == "id")
                    {
                        graph.Name = reader.Value;
                        currentGameObject.name = reader.Value;
                        currentGameObject.tag = Tags.Graph;
                    }
                }
                // You can move the reader back to the element node as follows:
                // reader.MoveToElement();
            }
        }

        /// <summary>
        /// Sets current to a new graph node and adds it to the nodes mapping.
        /// </summary>
        protected override void StartNode()
        {
            if (current != null)
            {
                LogError("There is still a pending graph element when new node declaration has begun.");
            }
            currentGameObject = new GameObject();
            current = currentGameObject.AddComponent<Node>();
            if (reader.HasAttributes)
            {
                while (reader.MoveToNextAttribute())
                {
                    if (reader.Name == "id")
                    {
                        nodes.Add(reader.Value, (Node)current);
                        break;
                    }
                }
            }
            else
            {
                LogError("Node without ID");
            }
        }

        /// <summary>
        /// Sets the linkname of the current graph element (a node) and resets it to null.
        /// </summary>
        protected override void EndNode()
        {
            if (current != null)
            {
                if (!(current is Node))
                {
                    LogError("The declaration to be ended is no node.");
                }
                else
                {
                    // Now the current node should have a linkname and we can
                    // actually add it to the graph.
                    Node node = (Node)current;
                    if (String.IsNullOrEmpty(node.LinkName))
                    {
                        LogError("Node has no attribute " + Node.LinknameAttribute);
                    }
                    else
                    {
                        currentGameObject.tag = Tags.Node;
                        currentGameObject.name = node.LinkName;
                        graph.AddNode(node);
                    }
                }
                current = null;
            }
            else
            {
                LogError("There is no node to be ended here.");
            }
            //if (currentGameObject != null)
            //{
            //    Dump(currentGameObject);
            //}
        }

        private static void Dump(GameObject obj)
        {
            Debug.Log("Loaded: " + obj.name + "\n");
            if (obj.TryGetComponent<Node>(out Node node))
            {
                Debug.Log(node.ToString() + "\n");
            }
        }

        /// <summary>
        /// Sets current to a new edge. Adds this edge to the graph deriving the
        /// source and target node of this edge from the nodes mapping using the
        /// GXL node IDs in the GXL file.
        /// </summary>
        protected override void StartEdge()
        {
            if (current != null)
            {
                LogError("There is still a pending graph element when new edge declaration has begun.");
            }
            // the game object we create for this edge in the game; we do not
            // know whether this edge is a hierarchical or non-hierarchical 
            // edge until we see the edge type; hierarchical edges will not be 
            // represented as GameObjects; if we create a game object for a 
            // a hierarchical edge, we may need to destroy it later again
            currentGameObject = new GameObject();
            current = currentGameObject.AddComponent<Edge>();
            currentGameObject.tag = Tags.Edge;
            
            if (reader.HasAttributes)
            {
                Edge thisEdge = (Edge)current;

                string fromNode = "";
                string toNode = "";

                // determine fromNode and toNode
                while (reader.MoveToNextAttribute())
                {
                    if (reader.Name == "from")
                    {
                        if (fromNode != "")
                        {
                            LogError("Edge has multiple source nodes.");
                        }
                        else
                        {
                            fromNode = reader.Value;
                            // set source of the edge
                            if (nodes.TryGetValue(fromNode, out Node node))
                            {
                                thisEdge.Source = node;
                            }
                            else
                            {
                                LogError("Unkown source node ID " + fromNode + ".");
                            }
                        }
                    }
                    else if (reader.Name == "to")
                    {
                        if (toNode != "")
                        {
                            LogError("Edge has multiple target nodes.");
                        }
                        else
                        {
                            toNode = reader.Value;
                            // set target of the edge
                            if (nodes.TryGetValue(toNode, out Node node))
                            {
                                thisEdge.Target = node;
                            }
                            else
                            {
                                LogError("Unkown target node ID " + toNode + ".");
                            }
                        }
                    }
                    else if (reader.Name == "id")
                    {
                        currentGameObject.name = reader.Value;
                    }
                } // while
                if (fromNode == "")
                {
                    LogError("Edge has no source node.");
                    throw new SyntaxError("Edge has no source node.");
                }
                else if (toNode == "")
                {
                    LogError("Edge has no target node.");
                    throw new SyntaxError("Edge has no target node.");
                }
            }
            else
            {
                LogError("Edge without source and target node.");
            }
        }

        /// <summary>
        /// Resets current graph element (an edge) to null.
        /// </summary>
        protected override void EndEdge()
        {
            if (current != null)
            {
                if (!(current is Edge))
                {
                    LogError("The declaration to be ended is no edge.");
                }
                else
                {
                    Edge edge = current as Edge;
                    if (hierarchicalEdgeTypes.Contains(edge.Type))
                    {
                        // hierarchial edges are turned into children
                        // Note: a hierarchical edge starts at the child and ends at the parent
                        edge.Target.AddChild(edge.Source);

                        // hierarchical edges do not have an associated GameObject; we need to
                        // destroy currentGameObject (which we created in StartEdge above) again
                        Destroyer.DestroyGameObject(currentGameObject);
                    }
                    else
                    {  // non-hierarchical edges are added to the graph
                        graph.AddEdge(edge);
                    }
                }
                current = null;
            }
            else
            {
                LogError("There is no edge to be ended here.");
            }
        }

        /// <summary>
        /// Sets the type of the current graph element.
        /// </summary>
        protected override void StartType()
        {
            if (current == null)
            {
                LogError("Found type declaration outside of a node/edge declaration.");
            }
            else
            {
                if (reader.HasAttributes)
                {
                    while (reader.MoveToNextAttribute())
                    {
                        if (reader.Name == "xlink:href")
                        {
                            current.Type = reader.Value;
                            break;
                        }
                    }
                }
                else
                {
                    LogError("Type declaration without name.");
                }
            }
        }

        // The name of the currently processed attribute. For instance, when
        // parsing:
        //   <attr name="Source.Name">
        //     <string>.entry</string>
        //   </attr>
        // the attribute name will be Source.Name.
        // Is the empty string outside of an attribute declaration.
        private string currentAttributeName = "";

        /// <summary>
        /// Defines currentAttributeName.
        /// </summary>
        protected override void StartAttr()
        {
            if (current == null)
            {
                LogError("Found attribute declaration outside of a node/edge declaration");
            }
            else
            {
                if (reader.HasAttributes)
                {
                    while (reader.MoveToNextAttribute())
                    {
                        if (reader.Name == "name")
                        {
                            // save for later when we know the attribute type
                            currentAttributeName = reader.Value;
                            break;
                        }
                    }
                }
                else
                {
                    LogError("Attribute declaration without name.");
                }
            }
        }

        /// <summary>
        /// Sets toggle attribute value of attribute currentAttributeName of current graph element.
        /// </summary>
        protected override void StartEnum()
        {
            if (current == null)
            {
                LogError("Found toggle attribute (enum) outside of a node/edge declaration.");
            }
            else if (currentAttributeName == "")
            {
                LogError("There is not attribute name for this enum.");
            }
            else
            {
                // enums (toggles) have no further attributes
                current.SetToggle(currentAttributeName);
            }
        }

        /// <summary>
        /// Sets string attribute value of attribute currentAttributeName of current graph element.
        /// </summary>
        protected override void EndString(string value)
        {
            if (current == null)
            {
                LogError("Found string attribute outside of a node/edge declaration.");
            }
            else if (currentAttributeName == "")
            {
                LogError("There is not attribute name for this string.");
            }
            else
            {
                current.SetString(currentAttributeName, value);
            }
        }

        /// <summary>
        /// Sets float attribute value of attribute currentAttributeName of current graph element.
        /// </summary>
        protected override void EndFloat(float value)
        {
            if (current == null)
            {
                LogError("Found float attribute outside of a node/edge declaration.");
            }
            else if (currentAttributeName == "")
            {
                LogError("There is not attribute name for this float.");
            }
            else
            {
                current.SetFloat(currentAttributeName, value);
            }
        }

        /// <summary>
        /// Sets int attribute value of attribute currentAttributeName of current graph element.
        /// </summary>
        protected override void EndInt(int value)
        {
            if (current == null)
            {
                LogError("Found int attribute outside of a node/edge declaration.");
            }
            else if (currentAttributeName == "")
            {
                LogError("There is not attribute name for this int.");
            }
            else
            {
                current.SetInt(currentAttributeName, value);
            }
        }
    }
}