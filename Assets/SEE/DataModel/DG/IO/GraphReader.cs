using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Joveler.Compression.XZ;
using SEE.Utils;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace SEE.DataModel.DG.IO
{
    /// <summary>
    /// Reads a graph from a GXL file and returns it as a graph.
    /// </summary>
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
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
        /// When the graph is loaded, the node levels are calculated.
        ///
        /// Precondition: <paramref name="rootID"/> must be unique.
        /// </summary>
        /// <param name="filename">the name of the GXL file</param>
        /// <param name="hierarchicalEdgeTypes">the set of edge-type names for edges considered to represent nesting</param>
        /// <param name="basePath">the base path of the graph</param>
        /// <param name="rootID">unique ID of the artificial root node if required</param>
        /// <param name="logger">the logger used for messages; if null, no messages are emitted</param>
        public GraphReader(string filename, HashSet<string> hierarchicalEdgeTypes, string basePath, string rootID = "", Utils.ILogger logger = null)
            : base(OpenFile(filename), filename, logger)
        {
            this.hierarchicalEdgeTypes = hierarchicalEdgeTypes;
            this.rootName = string.IsNullOrEmpty(rootID) ? "" : rootID;
            this.basePath = basePath;
        }

        /// <summary>
        /// This static constructor is used to initialize the liblzma library.
        /// It needn't be called explicitly, Unity does this automatically once via the <c>InitializeOnLoad</c>
        /// attribute assigned to this class.
        /// </summary>
        static GraphReader()
        {
            try
            {
                XZInit.GlobalInit(GetLiblzmaPath());
            }
            catch (InvalidOperationException e) when (e.Message.Contains(" is already initialized"))
            {
                // Already loaded. We can ignore this.
            }
        }

        /// <summary>
        /// Returns the platform-dependent path to the liblzma native library.
        /// </summary>
        /// <returns>Path to the liblzma library</returns>
        /// <exception cref="PlatformNotSupportedException">If the system platform is not supported</exception>
        private static string GetLiblzmaPath()
        {
            // The library liblzma.dll is located in Assets/Native/LZMA/<arch>/native/liblzma.dll
            // where <arch> specifies the operating system the Unity editor is currently running on
            // and the hardware architecture (e.g., win-x64).
            //
            // If SEE is started from the Unity editor, the library will be looked up
            // under this path.
            // In a build application of SEE (i.e., an executable running independently
            // from the Unity editor), the library is located in
            // SEE_Data/Plugins/<arch>/liblzma.dll instead, where <arch> specifies
            // the hardware architecture (e.g., x86_64; see also
            // https://docs.unity3d.com/Manual/PluginInspector.html).

            string libDir = Application.isEditor ?
                                Path.Combine(Path.GetFullPath(Application.dataPath), "Native", "LZMA")
                              : Path.Combine(Path.GetFullPath(Application.dataPath), "Plugins");

            if (Application.isEditor)
            {
                // In the editor, the <arch> specifier is a combination of the OS and the process
                // architecture. We will first handle the OS.
                OSPlatform platform = GetOSPlatform();
                if (platform == OSPlatform.Windows)
                {
                    libDir = Path.Combine(libDir, "win");
                }
                else if (platform == OSPlatform.Linux)
                {
                    libDir = Path.Combine(libDir, "linux");
                }
                else if (platform == OSPlatform.OSX)
                {
                    libDir = Path.Combine(libDir, "osx");
                }

                // Now follows the process architecture.
                switch (RuntimeInformation.ProcessArchitecture)
                {
                    case Architecture.X86:
                        libDir += "-x86";
                        break;
                    case Architecture.X64:
                        libDir += "-x64";
                        break;
                    case Architecture.Arm when platform == OSPlatform.Windows:
                        libDir += "10-arm";
                        break;
                    case Architecture.Arm64 when platform == OSPlatform.Windows:
                        libDir += "10-arm64";
                        break;
                    case Architecture.Arm:
                        libDir += "-arm";
                        break;
                    case Architecture.Arm64:
                        libDir += "-arm64";
                        break;
                    default: throw new PlatformNotSupportedException($"Unknown architecture {RuntimeInformation.ProcessArchitecture}");
                }

                libDir = Path.Combine(libDir, "native");
            }
            else
            {
                // In a deployed application, only the process architecture matters.
                string arch = RuntimeInformation.ProcessArchitecture switch
                {
                    Architecture.X86 or Architecture.Arm => "x86",
                    Architecture.X64 or Architecture.Arm64 => "x86_64",
                    _ => throw new PlatformNotSupportedException($"Unknown architecture {RuntimeInformation.ProcessArchitecture}"),
                };
                libDir = Path.Combine(libDir, arch);
            }

            string libPath = null;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                libPath = Path.Combine(libDir, "liblzma.dll");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                if (Application.isEditor)
                {
                    libPath = Path.Combine(libDir, "liblzma.so");
                }
                // Under Linux native plugins aren't stored inside a architecture subdir (e.G. x86_64).
                // They are stored directly in the Plugins dir.
                // So under Linux when constructing the path, it is necessary to omit this subdirectory specifically for Linux builds.
                else
                {
                    libPath = Path.Combine(Path.Combine(Path.GetFullPath(Application.dataPath), "Plugins"), "liblzma.so");
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                libPath = Path.Combine(libDir, "liblzma.dylib");
            }

            if (libPath == null)
            {
                throw new PlatformNotSupportedException("Unable to find native library.");
            }

            if (!File.Exists(libPath))
            {
                throw new PlatformNotSupportedException($"Unable to find native library [{libPath}].");
            }

            return libPath;

            // Returns the type of operating system. If other than Windows, Linux,
            // or OSX, an exception is thrown.
            static OSPlatform GetOSPlatform()
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return OSPlatform.Windows;
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    return OSPlatform.Linux;
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    return OSPlatform.OSX;
                }
                else
                {
                    throw new PlatformNotSupportedException
                        ("Only Windows, Linux, and OSX are supported operating systems.");
                }
            }
        }

        /// <summary>
        /// Opens the file with given <paramref name="filename"/> and returns it as a <see cref="Stream"/>.
        /// If <paramref name="filename"/> has the filename extension
        /// <see cref="Filenames.CompressedGXLExtension"/>, the stream will be the
        /// uncompressed content of the open file; otherwise it will be the content
        /// of the file as is.
        /// </summary>
        /// <param name="filename">name of the file to be opened</param>
        /// <returns>stream of the (possibly uncompressed) content of the opened file</returns>
        private static Stream OpenFile(string filename)
        {
            FileStream stream = File.OpenRead(filename);
            if (filename.ToLower().EndsWith(Filenames.CompressedGXLExtension))
            {
                // Handle compressed LZMA2 file.
                XZDecompressOptions options = new()
                {
                    LeaveOpen = false
                };
                return new XZStream(stream, options);
            }
            else
            {
                return stream;
            }
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
        /// Loads the graph from the GXL file and adds an artificial root node if requested
        /// (see constructor). The node levels will be calculated, too.
        /// </summary>
        public override void Load()
        {
            base.Load();
            graph.BasePath = basePath;
            if (!string.IsNullOrWhiteSpace(rootName))
            {
                List<Node> roots = graph.GetRoots();
                if (roots.Count == 0)
                {
                    Debug.LogWarning($"Graph stored in {name} is empty.\n");
                }
                else if (roots.Count > 1)
                {
                    Debug.LogWarning($"Graph stored in {name} has multiple roots. Adding an artificial single root {rootName}.\n");
                    Node singleRoot = new()
                    {
                        Type = Graph.UnknownType,
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
            // We don't know the base path yet, hence, we use the empty string.
            graph = new Graph("")
            {
                Path = name
            };
            if (reader.HasAttributes)
            {
                while (reader.MoveToNextAttribute())
                {
                    if (reader.Name == "id")
                    {
                        graph.Name = reader.Value;
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
                    if (node.TryGetString(Node.LinknameAttribute, out string linkname))
                    {
                        // The attribute Linkage.Name is actually not unique. There are cases where multiple
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
                        LogError($"Node has no attribute {Node.LinknameAttribute}");
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

            if (reader.HasAttributes)
            {
                // We will first collect those attributes and then create the edge
                // because the constructor of Edge requires an ID.
                string fromNode = "";
                string toNode = "";
                string id = "";

                // determine id, fromNode and toNode
                while (reader.MoveToNextAttribute())
                {
                    switch (reader.Name)
                    {
                        case "from" when fromNode != "":
                            LogError("Edge has multiple source nodes.");
                            break;
                        case "from":
                            fromNode = reader.Value;
                            break;
                        case "to" when toNode != "":
                            LogError("Edge has multiple target nodes.");
                            break;
                        case "to":
                            toNode = reader.Value;
                            break;
                        case "id":
                            id = reader.Value;
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
        private string currentAttributeName = string.Empty;

        /// <summary>
        /// Defines currentAttributeName.
        /// </summary>
        protected override void StartAttr()
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
                current.SetInt(currentAttributeName, value);
            }
        }
    }
}
