using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using SEE.Utils.Paths;
using UnityEngine;

namespace SEE.DataModel.DG.IO
{
    /// <summary>
    /// Reads a graph from a GXL file and returns it as a graph.
    /// </summary>
    public class GraphReader : GXLParser
    {
        /// <summary>
        /// Constructor. If <paramref name="rootID"/> is neither null nor the empty string and if
        /// the loaded graph has multiple roots, a single artificial root with that name will be added
        /// that becomes the parent of all other original roots. The <paramref name="rootID"/>
        /// determines both Source.Name, Linkage.Name, and Type of that artificial root. If
        /// <paramref name="rootID"/> is null or the empty string or has a single root, the graph
        /// will be loaded as stored in the GXL file.
        ///
        /// The loaded graph will have the <paramref name="basePath"/> that will be used
        /// to turn relative file-system paths into absolute ones. It should be chosen as
        /// the root directory in which the source code can be found.
        ///
        /// When the graph is loaded, the node levels are calculated.
        ///
        /// Precondition: <paramref name="rootID"/> must be unique.
        /// </summary>
        /// <param name="hierarchicalEdgeTypes">the set of edge-type names for edges considered to represent nesting</param>
        /// <param name="basePath">the base path of the graph</param>
        /// <param name="rootID">unique ID of the artificial root node if required</param>
        /// <param name="logger">the logger used for messages; if null, no messages are emitted</param>
        public GraphReader(HashSet<string> hierarchicalEdgeTypes, string basePath, string rootID = "", Utils.ILogger logger = null)
            : base(logger)
        {
            this.hierarchicalEdgeTypes = hierarchicalEdgeTypes;
            this.rootName = string.IsNullOrEmpty(rootID) ? "" : rootID;
            this.basePath = basePath;
        }

        /// <summary>
        /// Loads and returns a graph from the given <paramref name="path"/> assumed to contain GXL data.
        /// </summary>
        /// <param name="path">path of the GXL data</param>
        /// <param name="hierarchicalEdgeTypes">edge types forming the node hierarchy</param>
        /// <param name="basePath">the base path of the graph</param>
        /// <param name="changePercentage">to report progress</param>
        /// <param name="token">token with which the loading can be cancelled</param>
        /// <param name="logger">logger to log the output</param>
        /// <returns>loaded graph</returns>
        public static async UniTask<Graph> LoadAsync(DataPath path, HashSet<string> hierarchicalEdgeTypes, string basePath,
                                                     Action<float> changePercentage = null, CancellationToken token = default,
                                                     Utils.ILogger logger = null)
        {
            GraphReader graphReader = new(hierarchicalEdgeTypes, basePath, logger: logger);
            await graphReader.LoadAsync(await path.LoadAsync(), path.Path, changePercentage, token);
            return graphReader.GetGraph();
        }

        /// <summary>
        /// The value for the Source.Name, Linkage.Name, and Type of the artificial root if
        /// one is to be created at all.
        /// </summary>
        private readonly string rootName;

        /// <summary>
        /// The base path of the graph to be loaded.
        /// </summary>
        private readonly string basePath;

        /// <summary>
        /// Adds the nodes and edges in the GXL data provided in the <paramref name="gxl"/> stream and
        /// adds an artificial root node if there is no unique root node. The node levels will be
        /// calculated, too.
        /// </summary>
        /// <param name="gxl">Stream containing GXL data that shall be processed</param>
        /// <param name="name">Name of the GXL data stream. Only used for display purposes in log messages</param>
        /// <param name="changePercentage">to report progress</param>
        /// <param name="token">token with which the loading can be cancelled</param>
        public override async UniTask LoadAsync(Stream gxl, string name = "[unknown]",
                                                Action<float> changePercentage = null,
                                                CancellationToken token = default)
        {
            await base.LoadAsync(gxl, name, changePercentage, token);
            graph.BasePath = basePath;
            if (!string.IsNullOrWhiteSpace(rootName))
            {
                List<Node> roots = graph.GetRoots();
                if (roots.Count == 0)
                {
                    Debug.LogWarning($"Graph stored in {Name} is empty.\n");
                }
                else if (roots.Count > 1)
                {
                    Debug.LogWarning($"Graph stored in {Name} has multiple roots. Adding an artificial single root {rootName}.\n");
                    Node singleRoot = new()
                    {
                        Type = Graph.RootType,
                        ID = rootName,
                        SourceName = ""
                    };
                    graph.AddNode(singleRoot);
                    foreach (Node root in roots)
                    {
                        singleRoot.AddChild(root);
                    }
                }
            }

            /// The graph is loaded and its node hierarchy established. We can finalize
            /// the node hierarchy. This finalization is necessary to calculate the
            /// node levels. These in turn will be need to be available for setting the
            /// metric <see cref="Graph.MetricLevel"/>, which must be available when the
            /// metric scaler is asked to analyze the node metric values.
            graph.FinalizeNodeHierarchy();
        }

        /// <summary>
        /// Returns the graph after it was loaded. Load() must have been called before.
        /// </summary>
        /// <returns>loaded graph</returns>
        public Graph GetGraph()
        {
            return graph;
        }

        /// <summary>
        /// Returns the number of errors found during loading the GXL.
        /// </summary>
        public int Errors { get; private set; }

        /// <summary>
        /// Logs the given error message using the logger and increments the
        /// error count.
        /// </summary>
        /// <param name="message">message to be logged</param>
        protected override void LogError(string message)
        {
            base.LogError(message);
            Errors++;
        }

        // graph where to add the GXL information
        private Graph graph;

        // the previously added graph element (node or edge)
        private GraphElement current;

        // A mapping of the GXL node ids onto the graph nodes.
        private readonly Dictionary<string, Node> nodes = new();

        // The set of edge-type names for edges considered to represent nesting.
        private readonly HashSet<string> hierarchicalEdgeTypes;

        /// <summary>
        /// Sets the graph name using the attribute 'id'.
        /// </summary>
        protected override void StartGraph()
        {
            nodes.Clear();
            // We don't know the base path yet, hence, we use the empty string.
            graph = new Graph("")
            {
                Path = Name
            };
            if (Reader.HasAttributes)
            {
                while (Reader.MoveToNextAttribute())
                {
                    if (Reader.Name == "id")
                    {
                        graph.Name = Reader.Value;
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
            if (!ReferenceEquals(current, null))
            {
                LogError("There is still a pending graph element when new node declaration has begun.");
            }

            current = new Node();
            if (Reader.HasAttributes)
            {
                while (Reader.MoveToNextAttribute())
                {
                    if (Reader.Name == "id")
                    {
                        if (!nodes.TryAdd(Reader.Value, (Node)current))
                        {
                            LogError($"Node ID {Reader.Value} is not unique.");
                        }
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
        /// Sets the ID of the current graph element (a node), inserts into the graph, and resets it to null.
        /// </summary>
        protected override void EndNode()
        {
            if (ReferenceEquals(current, null))
            {
                LogError("There is no node to be ended here.");
            }
            else
            {
                if (current is not Node node)
                {
                    LogError("The declaration to be ended is no node.");
                }
                else
                {
                    // Now the current node should have a linkname and we can
                    // actually add it to the graph.
                    if (node.TryGetString(Linkage.Name, out string linkname))
                    {
                        // The attribute Linkage.Name is actually not always unique. There are cases where multiple
                        // nodes may have the same value for Linkage.Name. They will differ in another attribute
                        // Linkage.PIR_Node. If we have a node with both attributes, we can combine them to
                        // make a unique ID. The attribute Linkage.PIR_Node is an integer attribute.
                        if (node.TryGetInt("Linkage.PIR_Node", out int pir))
                        {
                            node.ID = $"{linkname}#{pir}";
                        }
                        else
                        {
                            node.ID = linkname;
                        }

                        try
                        {
                            graph.AddNode(node);
                        }
                        catch (InvalidOperationException e)
                        {
                            LogError($"Node ID {node.ID} is not unique: {e.Message}. This node will be ignored.");
                        }
                    }
                    else
                    {
                        LogError($"Node has no attribute {Linkage.Name}");
                        // let's try to use the Source.Name for the linkname instead, hoping it is unique
                        if (string.IsNullOrEmpty(node.SourceName))
                        {
                            LogError($"Node doesn't even have an attribute {Node.SourceNameAttribute}");
                        }
                        else
                        {
                            node.ID = node.SourceName;
                            graph.AddNode(node);
                        }
                    }
                }

                current = null;
            }
        }

        private static void Dump(GameObject obj)
        {
            Debug.Log($"Loaded: {obj.name}\n");
            if (obj.TryGetComponent(out Node node))
            {
                Debug.Log($"{node}\n");
            }
        }

        /// <summary>
        /// Sets current to a new edge. Adds this edge to the graph deriving the
        /// source and target node of this edge from the nodes mapping using the
        /// GXL node IDs in the GXL file.
        /// </summary>
        protected override void StartEdge()
        {
            if (!ReferenceEquals(current, null))
            {
                LogError("There is still a pending graph element when new edge declaration has begun.");
            }

            if (Reader.HasAttributes)
            {
                // We will first collect those attributes and then create the edge
                // because the constructor of Edge requires an ID.
                string fromNode = "";
                string toNode = "";
                string id = "";

                // determine id, fromNode and toNode
                while (Reader.MoveToNextAttribute())
                {
                    switch (Reader.Name)
                    {
                        case "from" when fromNode != "":
                            LogError("Edge has multiple source nodes.");
                            break;
                        case "from":
                            fromNode = Reader.Value;
                            break;
                        case "to" when toNode != "":
                            LogError("Edge has multiple target nodes.");
                            break;
                        case "to":
                            toNode = Reader.Value;
                            break;
                        case "id":
                            id = Reader.Value;
                            break;
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
                else if (id == "")
                {
                    LogError("Edge has no id.");
                    throw new SyntaxError("Edge has no id.");
                }

                // Note that we do not know yet whether this edge is a hierarchical
                // or non-hierarchical edge until we see the edge type.
                Edge thisEdge = new();
                // set source of the edge
                if (nodes.TryGetValue(fromNode, out Node sourceNode))
                {
                    thisEdge.Source = sourceNode;
                }
                else
                {
                    LogError($"Unknown source node ID {fromNode}.");
                }

                // set target of the edge
                if (nodes.TryGetValue(toNode, out Node targetNode))
                {
                    thisEdge.Target = targetNode;
                }
                else
                {
                    LogError($"Unknown target node ID {toNode}.");
                }

                current = thisEdge;
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
            if (!ReferenceEquals(current, null))
            {
                if (current is not Edge edge)
                {
                    LogError("The declaration to be ended is no edge.");
                }
                else
                {
                    if (hierarchicalEdgeTypes.Contains(edge.Type) && edge.Target != null && edge.Source != null)
                    {
                        // hierarchical edges are turned into children
                        // Note: a hierarchical edge starts at the child and ends at the parent
                        edge.Target.AddChild(edge.Source);
                    }
                    else
                    {
                        // non-hierarchical edges are added to the graph
                        try
                        {
                            graph.AddEdge(edge);
                        }
                        catch (Exception e)
                        {
                            LogError($"Edge {edge.ID} cannot be added to the graph: {e.Message}. This edge will be ignored.");
                        }
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
            if (ReferenceEquals(current, null))
            {
                LogError("Found type declaration outside of a node/edge declaration.");
            }
            else
            {
                if (Reader.HasAttributes)
                {
                    while (Reader.MoveToNextAttribute())
                    {
                        if (Reader.Name == "xlink:href")
                        {
                            current.Type = Reader.Value;
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
        private string currentAttributeName = string.Empty;

        /// <summary>
        /// Defines currentAttributeName.
        /// </summary>
        protected override void StartAttr()
        {
            if (Reader.HasAttributes)
            {
                while (Reader.MoveToNextAttribute())
                {
                    if (Reader.Name == "name")
                    {
                        // save for later when we know the attribute type
                        currentAttributeName = Reader.Value;
                        break;
                    }
                }
            }
            else
            {
                LogError("Attribute declaration without name.");
            }
        }

        /// <summary>
        /// Sets toggle attribute of attribute currentAttributeName of current graph element.
        /// </summary>
        protected override void StartEnum()
        {
            if (currentAttributeName == string.Empty)
            {
                LogError("There is no attribute name for this enum.");
            }
            else if (ReferenceEquals(current, null))
            {
                // current does not refer to a node or edge, hence, we should
                // be in the context of a graph.
                if (graph != null)
                {
                    graph.SetToggle(currentAttributeName);
                }
                else
                {
                    LogError("Found toggle attribute (enum) outside of a graph/node/edge declaration.");
                }
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
            if (currentAttributeName == string.Empty)
            {
                LogError("There is not attribute name for this string.");
            }
            else if (ReferenceEquals(current, null))
            {
                // current does not refer to a node or edge, hence, we should
                // be in the context of a graph.
                if (graph != null)
                {
                    graph.SetString(currentAttributeName, value);
                }
                else
                {
                    LogError("Found string attribute outside of a graph/node/edge declaration.");
                }
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
            if (currentAttributeName == string.Empty)
            {
                LogError("There is not attribute name for this float.");
            }
            else if (ReferenceEquals(current, null))
            {
                // current does not refer to a node or edge, hence, we should
                // be in the context of a graph.
                if (graph != null)
                {
                    graph.SetFloat(currentAttributeName, value);
                }
                else
                {
                    LogError("Found float attribute outside of a graph/node/edge declaration.");
                }
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
            if (currentAttributeName == "")
            {
                LogError("There is not attribute name for this int.");
            }
            else if (ReferenceEquals(current, null))
            {
                // current does not refer to a node or edge, hence, we should
                // be in the context of a graph.
                if (graph != null)
                {
                    graph.SetInt(currentAttributeName, value);
                }
                else
                {
                    LogError("Found int attribute outside of a graph/node/edge declaration.");
                }
            }
            else
            {
                // In the Axivion Suite, the Source.Region_Length and Source.Region_Start attributes are used to
                // denote ranges. In SEE, we use the SourceRange attribute to denote ranges, which works with an
                // explicit end line rather than a length. We hence need to convert the Source.Region_Length and
                // Source.Region_Start attributes to SourceRange attributes.
                switch (currentAttributeName)
                {
                    case RegionLengthAttribute:
                        // NOTE: This assumes the Region_Length is always declared *after* the Region_Start.
                        int endLine = current.GetInt(GraphElement.SourceRangeAttribute + Attributable.RangeStartLineSuffix) + value;
                        current.SetInt(GraphElement.SourceRangeAttribute + Attributable.RangeEndLineSuffix, endLine);
                        break;
                    case RegionStartAttribute:
                        current.SetInt(GraphElement.SourceRangeAttribute + Attributable.RangeStartLineSuffix, value);
                        break;
                    default:
                        current.SetInt(currentAttributeName, value);
                        break;
                }
            }
        }
    }
}
