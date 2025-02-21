using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Cysharp.Threading.Tasks;
using MoreLinq;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using SEE.Tools;
using SEE.Tools.LSP;
using SEE.Utils;
using SEE.Utils.Markdown;
using UnityEngine.Assertions;
using Debug = UnityEngine.Debug;

namespace SEE.DataModel.DG.IO
{
    /// <summary>
    /// The kinds of nodes that can be imported.
    ///
    /// These are the same as in OmniSharp.Extensions.LanguageServer.Protocol.Models.SymbolKind,
    /// but with values that are powers of 2 (and an offset of 1), so that they can be used as flags.
    /// </summary>
    /// <seealso cref="OmniSharp.Extensions.LanguageServer.Protocol.Models.SymbolKind"/>
    [Flags]
    public enum NodeKind
    {
        None = 0,
        File = 1 << 0,
        Module = 1 << 1,
        Namespace = 1 << 2,
        Package = 1 << 3,
        Class = 1 << 4,
        Method = 1 << 5,
        Property = 1 << 6,
        Field = 1 << 7,
        Constructor = 1 << 8,
        Enum = 1 << 9,
        Interface = 1 << 10,
        Function = 1 << 11,
        Variable = 1 << 12,
        Constant = 1 << 13,
        String = 1 << 14,
        Number = 1 << 15,
        Boolean = 1 << 16,
        Array = 1 << 17,
        Object = 1 << 18,
        Key = 1 << 19,
        Null = 1 << 20,
        EnumMember = 1 << 21,
        Struct = 1 << 22,
        Event = 1 << 23,
        Operator = 1 << 24,
        TypeParameter = 1 << 25,
        All = ~(~0 << 26)
    }

    /// <summary>
    /// The kinds of edges that can be imported.
    /// These edges will be created between nodes, thus representing relationships between source code elements.
    /// </summary>
    /// <remarks>
    /// Note that the values are powers of 2, so that they can be used as flags.
    /// </remarks>
    [Flags]
    public enum EdgeKind
    {
        /// <summary>
        /// No edge type.
        /// </summary>
        None = 0,

        /// <summary>
        /// A definition of a symbol.
        /// </summary>
        Definition = 1 << 0,

        /// <summary>
        /// A declaration of a symbol.
        /// </summary>
        Declaration = 1 << 1,

        /// <summary>
        /// A definition for the type of a symbol.
        /// </summary>
        TypeDefinition = 1 << 2,

        /// <summary>
        /// An implementation of a function or method.
        /// </summary>
        Implementation = 1 << 3,

        /// <summary>
        /// A general reference to a symbol.
        /// </summary>
        Reference = 1 << 4,

        /// <summary>
        /// An outgoing call to a function or method.
        /// </summary>
        Call = 1 << 5,

        /// <summary>
        /// A supertype of a class or interface.
        /// </summary>
        Extend = 1 << 6,

        /// <summary>
        /// All edge types.
        /// </summary>
        All = ~(~0 << 7)
    }

    /// <summary>
    /// The kinds of diagnostics that can be imported for nodes.
    ///
    /// These are the same as in OmniSharp.Extensions.LanguageServer.Protocol.Models.DiagnosticSeverity,
    /// but with values that are powers of 2 (and an offset of 1), so that they can be used as flags.
    /// </summary>
    /// <seealso cref="OmniSharp.Extensions.LanguageServer.Protocol.Models.DiagnosticSeverity"/>
    [Flags]
    public enum DiagnosticKind
    {
        None = 0,
        Error = 1 << 0,
        Warning = 1 << 1,
        Information = 1 << 2,
        Hint = 1 << 3,
        All = ~(~0 << 4)
    }

    /// <summary>
    /// A class that creates a graph from the output of a language server.
    /// </summary>
    /// <param name="Handler">The language server handler to be used for the import.</param>
    /// <param name="SourcePaths">The paths to the source files to be imported.</param>
    /// <param name="ExcludedPaths">The paths to be excluded from the import.</param>
    /// <param name="IncludeNodeTypes">The types of nodes to include in the import.</param>
    /// <param name="IncludeDiagnostics">The kinds of diagnostics to include in the import.</param>
    /// <param name="IncludeEdgeTypes">The types of edges to include in the import.</param>
    /// <param name="AvoidSelfReferences">If true, no self-references will be created.</param>
    /// <param name="AvoidParentReferences">If true, no edges to parent nodes will be created.</param>
    public record LSPImporter(
        LSPHandler Handler,
        IList<string> SourcePaths,
        IList<string> ExcludedPaths,
        NodeKind IncludeNodeTypes = NodeKind.All,
        DiagnosticKind IncludeDiagnostics = DiagnosticKind.All,
        // By default, all edge types are included, except for <see cref="EdgeKind.Definition"/>
        // and <see cref="EdgeKind.Declaration"/>, since nodes would otherwise often get a self-reference.
        EdgeKind IncludeEdgeTypes = EdgeKind.All & ~(EdgeKind.Definition | EdgeKind.Declaration),
        bool AvoidSelfReferences = true,
        bool AvoidParentReferences = true)
    {
        /// <summary>
        /// A mapping from directory paths to their corresponding nodes.
        /// </summary>
        private readonly IDictionary<string, Node> nodeAtDirectory = new Dictionary<string, Node>();

        /// <summary>
        /// A mapping from file paths to their corresponding range trees.
        /// </summary>
        /// <seealso cref="KDIntervalTree{T}"/>
        private IDictionary<string, KDIntervalTree<Node>> rangeTrees = new Dictionary<string, KDIntervalTree<Node>>();

        /// <summary>
        /// Number of newly added edges.
        /// </summary>
        private int newEdges;

        /// <summary>
        /// The attribute name for the selection range of an edge.
        /// </summary>
        private const string SelectionRangeAttribute = "SelectionRange";

        /// <summary>
        /// Whether the given <paramref name="checkPath"/> applies to the given <paramref name="actualPath"/>.
        /// </summary>
        /// <param name="checkPath">The path to check. May be a regular expression if ending with $.</param>
        /// <param name="actualPath">The actual path to compare against.</param>
        /// <returns>True if the check path applies to the actual path, false otherwise.</returns>
        private static bool PathApplies(string checkPath, string actualPath)
        {
            if (checkPath.EndsWith('$'))
            {
                // This is a regular expression.
                return Regex.IsMatch(actualPath, checkPath);
            }
            else
            {
                return actualPath.StartsWith(checkPath);
            }
        }

        /// <summary>
        /// Loads nodes and edges from the language server and adds them to the given <paramref name="graph"/>.
        /// </summary>
        /// <param name="graph">The graph to which the nodes and edges should be added.</param>
        /// <param name="changePercentage">A callback that is called with the progress percentage (0 to 1).</param>
        /// <param name="token">A cancellation token that can be used to cancel the import.</param>
        /// <exception cref="OperationCanceledException">Thrown if the operation is canceled.</exception>
        public async UniTask LoadAsync(Graph graph, Action<float> changePercentage = null,
                                       CancellationToken token = default)
        {
            // Query all documents whose file extension is supported by the language server.
            List<string> relevantExtensions = Handler.Server.Languages.SelectMany(x => x.FileExtensions).ToList();
            List<string> relevantDocuments = SourcePaths.SelectMany(RelevantDocumentsForPath)
                                                        .Where(x => ExcludedPaths.All(y => !PathApplies(y, x)))
                                                        .Distinct().ToList();
            IList<Node> originalNodes = graph.Nodes();
            nodeAtDirectory.Clear();
            newEdges = 0;

            // For changePercentage: Edge creation will very roughly take up a fraction of (1 - 1 / (n+1)) * 90%,
            // where n is the number of activated edge kinds (determined empirically).
            float activatedEdgeKinds = Enum.GetValues(typeof(EdgeKind)).Cast<EdgeKind>()
                                           .Count(x => x != EdgeKind.None && x != EdgeKind.All && IncludeEdgeTypes.HasFlag(x));
            float edgeProgressFactor = 0.9f - 0.9f / (activatedEdgeKinds + 1);

            bool supportsPullDiagnostics = Handler.ServerCapabilities.DiagnosticProvider != null;
            if (!supportsPullDiagnostics && IncludeDiagnostics != DiagnosticKind.None)
            {
                Debug.LogWarning("The language server does not support pull diagnostics. "
                                 + "We can only catch diagnostics that have been emitted until the graph import is done,"
                                 + "hence, some diagnostics might be missing.\n");
            }

            int documentCount;
            for (documentCount = 0; documentCount < relevantDocuments.Count; documentCount++)
            {
                string path = relevantDocuments[documentCount];
                Handler.OpenDocument(path);
                Node dirNode = AddOrGetDirectoryNode(Path.GetDirectoryName(path), graph);
                Node symbolParent = dirNode;
                if (IncludeNodeTypes.HasFlag(NodeKind.File))
                {
                    Node fileNode = new()
                    {
                        ID = Path.GetRelativePath(Handler.ProjectPath, path)
                    };
                    fileNode.SourceName = fileNode.Filename = Path.GetFileName(path);
                    fileNode.Directory = Path.GetDirectoryName(path);
                    fileNode.Type = NodeKind.File.ToString();
                    SetFileLOC(fileNode);
                    graph.AddNode(fileNode);
                    fileNode.Reparent(dirNode);
                    symbolParent = fileNode;
                    if (supportsPullDiagnostics)
                    {
                        IEnumerable<Diagnostic> diagnostics = await Handler.PullDocumentDiagnosticsAsync(path);
                        if (diagnostics != null)
                        {
                            HandleDiagnostics(diagnostics, path);
                        }
                    }
                }

                IUniTaskAsyncEnumerable<SymbolInformationOrDocumentSymbol> symbols = Handler.DocumentSymbols(path);
                await foreach (SymbolInformationOrDocumentSymbol symbol in symbols)
                {
                    if (symbol.IsDocumentSymbolInformation)
                    {
                        Debug.LogError("This language server emits SymbolInformation, which is deprecated and not "
                                       + "supported by SEE. Please choose a language server that is capable of "
                                       + "returning hierarchic DocumentSymbols.\n");
                        return;
                    }

                    await AddSymbolNodeAsync(symbol.DocumentSymbol, path, graph, symbolParent, token);
                }

                Handler.CloseDocument(path);
                // ~20% of the progress is made by loading the documents and its symbols.
                changePercentage?.Invoke((1 - edgeProgressFactor) * documentCount / relevantDocuments.Count);
            }

            // Relevant nodes (for edges) are those that have a source range and are not already in the graph.
            IList<Node> relevantNodes = graph.Nodes().Except(originalNodes).Where(x => x.SourceRange != null).ToList();
            Debug.Log($"LSPImporter: Found {documentCount} documents with relevant extensions ({string.Join(", ", relevantExtensions)}).\n");

            if (Handler.Server == LSPServer.EclipseJdtls)
            {
                // This server requires manual correction of the Java package hierarchies.
                HandleJavaClasses(relevantNodes);
            }

            if (relevantNodes.Count == 0)
            {
                Debug.LogError("LSPImporter: No relevant nodes found. Aborting import.\n");
                return;
            }

            // We build a range tree for each file, so that we can quickly find the nodes with the smallest size
            // that contain the given range.
            Dictionary<string, List<Node>> relevantNodesByPath = relevantNodes.GroupBy(x => x.Path()).ToDictionary(x => x.Key, x => x.ToList());
            rangeTrees = relevantNodesByPath.ToDictionary(x => x.Key, x => new KDIntervalTree<Node>(x.Value, node => node.SourceRange));

            int i = 0;
            foreach ((string path, List<Node> nodes) in relevantNodesByPath)
            {
                Handler.OpenDocument(path);
                foreach (Node node in nodes)
                {
                    if (token.IsCancellationRequested)
                    {
                        throw new OperationCanceledException();
                    }
                    // Depending on capabilities and settings, we connect the nodes with edges.
                    if (IncludeEdgeTypes.HasFlag(EdgeKind.Definition) && Handler.ServerCapabilities.DefinitionProvider.TrueOrValue())
                    {
                        await ConnectNodeViaAsync(Handler.Definition, LSP.Definition, node, graph, token: token);
                    }
                    if (IncludeEdgeTypes.HasFlag(EdgeKind.Declaration) && Handler.ServerCapabilities.DeclarationProvider.TrueOrValue())
                    {
                        await ConnectNodeViaAsync(Handler.Declaration, LSP.Declaration, node, graph, token: token);
                    }
                    if (IncludeEdgeTypes.HasFlag(EdgeKind.TypeDefinition) && Handler.ServerCapabilities.TypeDefinitionProvider.TrueOrValue())
                    {
                        await ConnectNodeViaAsync(Handler.TypeDefinition, LSP.OfType, node, graph, token: token);
                    }
                    if (IncludeEdgeTypes.HasFlag(EdgeKind.Implementation) && Handler.ServerCapabilities.ImplementationProvider.TrueOrValue())
                    {
                        await ConnectNodeViaAsync(Handler.Implementation, LSP.ImplementationOf, node, graph, reverseDirection: true, token);
                    }
                    if (IncludeEdgeTypes.HasFlag(EdgeKind.Reference) && Handler.ServerCapabilities.ReferencesProvider.TrueOrValue())
                    {
                        await ConnectNodeViaAsync((p, line, character) => Handler.References(p, line, character), LSP.Reference, node, graph, reverseDirection: true, token);
                    }
                    if (IncludeEdgeTypes.HasFlag(EdgeKind.Call) && Handler.ServerCapabilities.CallHierarchyProvider.TrueOrValue())
                    {
                        await HandleEdgeHierarchy(() => HandleCallHierarchyAsync(node, graph, token));
                    }
                    if (IncludeEdgeTypes.HasFlag(EdgeKind.Extend) && Handler.ServerCapabilities.TypeHierarchyProvider.TrueOrValue())
                    {
                        await HandleEdgeHierarchy(() => HandleTypeHierarchyAsync(node, graph, token));
                    }

                    // The remaining 80% of the progress is made by connecting the nodes.
                    // The Count+1 prevents the progress from reaching 1.0, since the diagnostics may not yet be pulled.
                    changePercentage?.Invoke(1 - edgeProgressFactor + edgeProgressFactor * i++ / (relevantNodes.Count + 1));
                }
            }
            Debug.Log($"LSPImporter: Imported {graph.Nodes().Except(originalNodes).Count()} new nodes and {newEdges} new edges.\n");

            // Handle diagnostics if not pulled.
            if (!supportsPullDiagnostics && IncludeDiagnostics != DiagnosticKind.None)
            {
                // In this case, we will wait one additional second to give the server at least some time to emit diagnostics.
                // TODO (#746): Collect diagnostics in background, or find a better way to handle this.
                await UniTask.Delay(Handler.TimeoutSpan, cancellationToken: token);
                foreach (PublishDiagnosticsParams diagnosticsParams in Handler.GetUnhandledPublishedDiagnostics())
                {
                    HandleDiagnostics(diagnosticsParams.Diagnostics, diagnosticsParams.Uri.Path);
                }
            }

            // Aggregate LOC upwards.
            MetricAggregator.AggregateSum(graph, new[] { NumericAttributeNames.LOC.Name() }, withSuffix: false, asInt: true);
            // Aggregate diagnostics upwards. We do this with a suffix, since these metrics may be used for erosion icons.
            MetricAggregator.AggregateSum(graph, IncludeDiagnostics.ToDiagnosticSeverity().Select(x => x.Name()), withSuffix: true, asInt: true);

            graph.BasePath = Handler.ProjectPath;

            changePercentage?.Invoke(1);

            return;

            async UniTask HandleEdgeHierarchy(Func<UniTask> hierarchyFunction)
            {
                try
                {
                    await hierarchyFunction();
                }
                catch (TimeoutException e)
                {
                    Debug.LogWarning(e + "\n");
                }
            }

            IEnumerable<string> RelevantDocumentsForPath(string path)
            {
                return relevantExtensions.SelectMany(x => Directory.EnumerateFiles(path, $"*.{x}", SearchOption.AllDirectories)).Select(x => Filenames.OnCurrentPlatform(x));
            }

            void HandleJavaClasses(IList<Node> nodes)
            {
                Dictionary<string, Node> packageNodes = new();
                // Java package hierarchies are not collected properly by the language server.
                // Instead, we will infer them from the file paths.
                foreach (Node node in nodes.Where(x => x.Type == NodeKind.Class.ToString()))
                {
                    // Aside from the hierarchies, we also want to remember as a metric how many methods are in a class.
                    node.SetInt("Num_Methods", nodes.Count(x => x.Parent == node && x.Type == NodeKind.Method.ToString()));

                    string relativePath = Path.GetRelativePath(Handler.ProjectPath, node.Directory);
                    string packageName = relativePath.Replace(Path.DirectorySeparatorChar, '.').TrimEnd('.');

                    if (packageNodes.TryGetValue(packageName, out Node package))
                    {
                        // The package node already exists, so we reparent the class node to it.
                        node.Reparent(package);
                        continue;
                    }
                    Node packageNode = new()
                    {
                        ID = packageName,
                        SourceName = packageName,
                        Directory = node.Directory,
                        Type = NodeKind.Package.ToString()
                    };
                    graph.AddNode(packageNode);
                    packageNodes[packageName] = packageNode;

                    // Reparent the class node to the package node (if it previously was just inside a directory).
                    if (node.Parent != null &&
                        (node.Parent.Type == NodeKind.File.ToString()
                            || node.Parent.Type == NodeKind.Package.ToString()
                            || node.Parent.Type == "Directory"))
                    {
                        node.Reparent(packageNode);
                    }
                }

                // Finally, another run through the nodes to reparent the package nodes to their parent packages.
                foreach (Node packageNode in packageNodes.Values)
                {
                    Node parentPackage = GetParentPackage(packageNode.ID);
                    if (parentPackage != null)
                    {
                        packageNode.Reparent(parentPackage);
                    }
                }

                return;

                Node GetParentPackage(string package)
                {
                    for (int lastDot = package.LastIndexOf('.'); lastDot != -1; lastDot = package.LastIndexOf('.'))
                    {
                        package = package[..lastDot];
                        if (packageNodes.TryGetValue(package, out Node parent))
                        {
                            return parent;
                        }
                    }
                    return null;
                }
            }
        }

        /// <summary>
        /// Associates the <paramref name="diagnostics"/> for the file at the given <paramref name="path"/>
        /// with the corresponding nodes in the graph. Specifically, the diagnostics are counted for each node
        /// and stored as attributes in the nodes.
        /// </summary>
        /// <param name="diagnostics">The diagnostics to associate with the nodes.</param>
        /// <param name="path">The path of the file to which the diagnostics belong.</param>
        private void HandleDiagnostics(IEnumerable<Diagnostic> diagnostics, string path)
        {
            foreach (Diagnostic diagnostic in diagnostics)
            {
                if (diagnostic.Severity.HasValue && IncludeDiagnostics.HasFlag(diagnostic.Severity.Value.ToDiagnosticKind()))
                {
                    IEnumerable<Node> diagnosticNodes = FindNodesByLocation(path, Range.FromLspRange(diagnostic.Range));
                    foreach (Node node in diagnosticNodes)
                    {
                        DiagnosticSeverity severity = diagnostic.Severity.Value;
                        if (node.TryGetInt(severity.Name(), out int count))
                        {
                            node.SetInt(severity.Name(), count + 1);
                        }
                        else
                        {
                            // If the severity metric is not yet set, we set it to 1.
                            node.SetInt(severity.Name(), 1);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Retrieves the outgoing call hierarchy for the given <paramref name="node"/>
        /// and adds the corresponding edges to the <paramref name="graph"/>.
        /// </summary>
        /// <param name="node">The node for which to retrieve the call hierarchy.</param>
        /// <param name="graph">The graph to which the edges should be added.</param>
        /// <param name="token">A cancellation token that can be used to cancel the operation.</param>
        private async UniTask HandleCallHierarchyAsync(Node node, Graph graph, CancellationToken token)
        {
            IUniTaskAsyncEnumerable<CallHierarchyItem> results = Handler.OutgoingCalls(SelectItem, node.Path(), node.SourceLine - 1 ?? 0, node.SourceColumn - 1 ?? 0);
            await foreach (CallHierarchyItem item in results)
            {
                if (token.IsCancellationRequested)
                {
                    throw new OperationCanceledException();
                }
                Node targetNode = FindNodesByLocation(item.Uri.Path, Range.FromLspRange(item.Range)).FirstOrDefault();
                if (targetNode == null)
                {
                    continue;
                }
                Edge edge = AddEdge(node, targetNode, LSP.Call, false, graph);
                edge?.SetRange(SelectionRangeAttribute, Range.FromLspRange(item.SelectionRange));
            }
            return;

            bool SelectItem(CallHierarchyItem item)
            {
                return item.Uri.Path == node.Path() && Range.FromLspRange(item.Range) == node.SourceRange;
            }
        }

        /// <summary>
        /// Retrieves the parent type hierarchy for the given <paramref name="node"/>
        /// and adds the corresponding edges to the <paramref name="graph"/>.
        /// </summary>
        /// <param name="node">The node for which to retrieve the type hierarchy.</param>
        /// <param name="graph">The graph to which the edges should be added.</param>
        /// <param name="token">A cancellation token that can be used to cancel the operation.</param>
        private async UniTask HandleTypeHierarchyAsync(Node node, Graph graph, CancellationToken token)
        {
            IUniTaskAsyncEnumerable<TypeHierarchyItem> results = Handler.Supertypes(SelectItem, node.Path(), node.SourceLine - 1 ?? 0, node.SourceColumn - 1 ?? 0);
            await foreach (TypeHierarchyItem item in results)
            {
                if (token.IsCancellationRequested)
                {
                    throw new OperationCanceledException();
                }
                Node targetNode = FindNodesByLocation(item.Uri.Path, Range.FromLspRange(item.Range)).FirstOrDefault();
                if (targetNode == null)
                {
                    continue;
                }
                Edge edge = AddEdge(node, targetNode, LSP.Extend, false, graph);
                edge.SetRange(SelectionRangeAttribute, Range.FromLspRange(item.SelectionRange));
            }
            return;

            bool SelectItem(TypeHierarchyItem item)
            {
                return item.Uri.Path == node.Path() && Range.FromLspRange(item.Range) == node.SourceRange;
            }
        }

        /// <summary>
        /// Checks whether the given <paramref name="node"/> and <paramref name="other"/> node are isomorphic,
        /// that is, whether they should be considered the same node in the graph.
        /// </summary>
        /// <param name="node">The first node to compare.</param>
        /// <param name="other">The second node to compare.</param>
        /// <returns>True if the nodes are isomorphic, false otherwise.</returns>
        private static bool AreIsomorphic(Node node, Node other) => node.HasSameAttributes(other);

        /// <summary>
        /// Adds a node for the given LSP <paramref name="symbol"/> to the given <paramref name="graph"/>.
        /// </summary>
        /// <param name="symbol">The LSP symbol for which to add a node.</param>
        /// <param name="path">The path of the file in which the symbol is located.</param>
        /// <param name="graph">The graph to which the node should be added.</param>
        /// <param name="parent">The parent node of the symbol node.</param>
        /// <param name="token">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>The added node, or null if the node was skipped.</returns>
        private async UniTask AddSymbolNodeAsync(DocumentSymbol symbol, string path, Graph graph, Node parent,
                                                 CancellationToken token)
        {
            if (token.IsCancellationRequested)
            {
                throw new OperationCanceledException();
            }

            Node childParent;
            if (IncludeNodeTypes.HasFlag(symbol.Kind.ToNodeKind()))
            {
                Node node = childParent = new Node();
                string name = symbol.Name;
                if (parent != null)
                {
                    name = $"{parent.SourceName}.{name}";
                }
                node.ID = name;
                node.SourceName = symbol.Name;
                node.Filename = Path.GetFileName(path);
                node.Directory = Path.GetDirectoryName(path);
                node.Type = symbol.Kind.ToNodeKind().ToString();
                node.SourceRange = Range.FromLspRange(symbol.Range);
                node.SourceLine = symbol.SelectionRange.Start.Line + 1;
                node.SourceColumn = symbol.SelectionRange.Start.Character + 1;
                node.SetRange(SelectionRangeAttribute, Range.FromLspRange(symbol.SelectionRange));
                node.SetInt(NumericAttributeNames.LOC.Name(), symbol.Range.End.Line - symbol.Range.Start.Line);
                if (symbol.Tags != null && symbol.Tags.Contains(SymbolTag.Deprecated))
                {
                    node.SetToggle("Deprecated", true);
                }

                Node sameNode = graph.Nodes().FirstOrDefault(x => AreIsomorphic(node, x));
                if (sameNode == null)
                {
                    // We pre-fetch the hover information and store it in the node.
                    if (Handler.ServerCapabilities.HoverProvider.TrueOrValue())
                    {
                        Hover hover = await Handler.HoverAsync(path, node.SourceLine - 1 ?? 0, node.SourceColumn - 1 ?? 0);
                        if (hover != null)
                        {
                            node.SetString("HoverText", hover.Contents.ToRichText());
                        }
                    }

                    if (graph.Nodes().Any(x => x.ID == node.ID))
                    {
                        // We need to make sure that the node ID is unique in the graph, so we append a random suffix.
                        node.ID += $"#{Guid.NewGuid()}";
                    }

                    graph.AddNode(node);
                    // Note: We need to use AbsolutePlatformPath() because path is a platform path.
                    // Moreover, we can check this assertion here once the node has been added to the graph,
                    // since AbsolutePlatformPath() will access the graph's BasePath.
                    Assert.AreEqual(path, node.AbsolutePlatformPath());
                    node.Reparent(parent);
                }
                else
                {
                    // An isomorphic node already exists in the graph. We will use that one instead.
                }
            }
            else
            {
                // We skip nodes that are not of the desired type, but will still need to add their descendants.
                childParent = parent;
            }

            foreach (DocumentSymbol child in symbol.Children ?? Array.Empty<DocumentSymbol>())
            {
                await AddSymbolNodeAsync(child, path, graph, childParent, token);
            }
        }

        /// <summary>
        /// Adds a node for the given <paramref name="directoryPath"/> to the given <paramref name="graph"/>.
        /// If the node already exists, it is returned immediately.
        /// If the directory path is not within the project path, null is returned.
        /// If the node for the parent directory does not yet exist, it is created recursively.
        /// </summary>
        /// <param name="directoryPath">The path of the directory for which to add a node.</param>
        /// <param name="graph">The graph to which the node should be added.</param>
        /// <returns>The added or existing node for the directory.</returns>
        private Node AddOrGetDirectoryNode(string directoryPath, Graph graph)
        {
            if (nodeAtDirectory.TryGetValue(directoryPath, out Node node))
            {
                return node;
            }
            else if (!directoryPath.StartsWith(Handler.ProjectPath))
            {
                // We have gone beyond the root of the project.
                return null;
            }

            // If the directory path ends with a separator, we remove it,
            // so that the last component is correctly identified.
            if (directoryPath.EndsWith(Path.DirectorySeparatorChar))
            {
                directoryPath = directoryPath[..^1];
            }
            // The node for the directory does not yet exist, so we create it.
            node = new Node
            {
                ID = Path.GetRelativePath(Handler.ProjectPath, directoryPath) + '/',
                SourceName = Path.GetFileName(directoryPath),
                Directory = directoryPath,
                Type = "Directory"
            };
            if (node.ID == ".")
            {
                // In case the project path is the root directory, we make the ID a bit more descriptive.
                node.ID = Path.GetFileName(Handler.ProjectPath);
            }
            nodeAtDirectory[directoryPath] = node;
            graph.AddNode(node);

            // We recursively add the parent directory.
            Node parent = AddOrGetDirectoryNode(Path.GetDirectoryName(directoryPath), graph);
            if (parent != null && !ReferenceEquals(parent, node))
            {
                node.Reparent(parent);
            }

            return node;
        }


        /// <summary>
        /// Connects the given <paramref name="node"/> to other nodes in the <paramref name="graph"/>
        /// via the given LSP function <paramref name="lspFunction"/>.
        /// </summary>
        /// <param name="lspFunction">An LSP function that returns connected locations for the given path,
        /// line, and column.</param>
        /// <param name="type">The type of the edges to be created.</param>
        /// <param name="node">The node to use as a source for the edges.</param>
        /// <param name="graph">The graph to which the edges should be added.</param>
        /// <param name="reverseDirection">If true, the direction of the edges is reversed, i.e.,
        /// <param name="token">A cancellation token that can be used to cancel the operation.</param>
        /// the source and target nodes are swapped.</param>
        private async UniTask ConnectNodeViaAsync(Func<string, int, int, IUniTaskAsyncEnumerable<LocationOrLocationLink>> lspFunction,
                                                  string type, Node node, Graph graph, bool reverseDirection = false,
                                                  CancellationToken token = default)
        {
            IUniTaskAsyncEnumerable<LocationOrLocationLink> locations = lspFunction(node.Path(), node.SourceLine - 1 ?? 0, node.SourceColumn - 1 ?? 0);
            await foreach (LocationOrLocationLink location in locations)
            {
                if (token.IsCancellationRequested)
                {
                    throw new OperationCanceledException();
                }

                if (location.IsLocation)
                {
                    // NOTE: We assume only local files are used.
                    foreach (Node targetNode in FindNodesByLocation(location.Location!.Uri.Path, Range.FromLspRange(location.Location.Range)))
                    {
                        AddEdge(node, targetNode, type, reverseDirection, graph);
                    }
                }
                else
                {
                    foreach (Node targetNode in FindNodesByLocation(location.LocationLink!.TargetUri.Path, Range.FromLspRange(location.LocationLink.TargetRange)))
                    {
                        Edge edge = AddEdge(node, targetNode, type, reverseDirection, graph);
                        edge?.SetRange(SelectionRangeAttribute, Range.FromLspRange(location.LocationLink.TargetSelectionRange));
                    }
                }
            }
        }


        /// <summary>
        /// Adds an edge between the given <paramref name="source"/> and <paramref name="target"/> nodes
        /// of the given <paramref name="type"/> to the given <paramref name="graph"/>.
        ///
        /// The difference to <see cref="Graph.AddEdge(Node, Node, string)"/> is that this method skips adding the edge:
        /// if it already exists,
        /// if the source and target nodes are the same (depending on <see cref="AvoidSelfReferences"/>), or
        /// if the target node is the parent of the source node (depending on <see cref="AvoidParentReferences"/>).
        /// </summary>
        /// <param name="source">The source node of the edge.</param>
        /// <param name="target">The target node of the edge.</param>
        /// <param name="type">The type of the edge.</param>
        /// <param name="reverseDirection">If true, the direction of the edge is reversed
        /// (i.e., the source and target nodes are swapped).</param>
        /// <param name="graph">The graph to which the edge should be added.</param>
        /// <returns>The added edge, or null if no edge was added.</returns>
        private Edge AddEdge(Node source, Node target, string type, bool reverseDirection, Graph graph)
        {
            if (AvoidSelfReferences && ReferenceEquals(target, source))
            {
                // Avoid self-references.
                return null;
            }
            if (AvoidParentReferences && ReferenceEquals(target, source.Parent))
            {
                // Imagine a variable declaration `int something = 10 - otherThing;`, where `otherThing`
                // is declared in the same local scope as this line.
                // The range of the `something` node will consist of the letters `something`.
                // If we now find references for the `otherThing` variable, one of them will be in this line,
                // consisting of the `otherThing` letter. Hence, it would be desirable to connect the `something`
                // node with the `otherThing` node. However, since the ranges of the reference (`otherThing`) and
                // the declaration (`something`) are totally disjoint, we would instead create a reference from
                // `something` to its parent (e.g., the function), which isn't helpful—hence, we skip this edge.
                return null;
            }

            if (reverseDirection)
            {
                (source, target) = (target, source);
            }
            if (graph.ContainsEdgeID(Edge.GetGeneratedID(source, target, type)))
            {
                // Avoid redundant edges.
                return null;
            }

            newEdges++;
            return graph.AddEdge(source, target, type);
        }

        /// <summary>
        /// Finds the "most fitting" nodes that are located at the given <paramref name="range"/>
        /// in the file at the given <paramref name="path"/>.
        ///
        /// Nodes are "fitted" to ranges by the <see cref="KDIntervalTree{T}"/> that is built for each file.
        /// </summary>
        /// <param name="path">The path of the file in which to search for nodes.</param>
        /// <param name="range">The range in the file at which to search for nodes.</param>
        /// <returns>The nodes that are located at the given range in the file.</returns>
        /// <seealso cref="KDIntervalTree{T}"/>
        private IEnumerable<Node> FindNodesByLocation(string path, Range range)

        {
            if (rangeTrees.TryGetValue(path, out KDIntervalTree<Node> tree))
            {
                // We need to do a stabbing query here, with the caveat that we want the tightest fitting range.
                // We use our custom-made KDIntervalTree for this purpose.
                return tree.Stab(range);
            }

            return Enumerable.Empty<Node>();
        }

        /// <summary>
        /// Sets the lines of code (LOC) attribute for the given file node to the number of lines in the file.
        /// </summary>
        /// <param name="node">The file node for which to set the LOC attribute.</param>
        private static void SetFileLOC(Node node)
        {
            Assert.IsTrue(node.Type == NodeKind.File.ToString());
            node.SetInt(NumericAttributeNames.LOC.Name(), File.ReadAllLines(node.Path()).Length);
        }
    }

    /// <summary>
    /// Provides helper extensions methods to convert between <see cref="NodeKind"/>
    /// and <see cref="SymbolKind"/>.
    /// </summary>
    public static class NodeKindExtensions
    {
        /// <summary>
        /// Converts a <see cref="SymbolKind"/> to a <see cref="NodeKind"/>.
        /// </summary>
        /// <param name="kind">The symbol kind to convert.</param>
        /// <returns>The corresponding node kind.</returns>
        public static NodeKind ToNodeKind(this SymbolKind kind)
        {
            // By taking the power of 2, we can use the original enum values as flags.
            int shiftedValue = 1 << (int)(kind - 1);
            if (Enum.IsDefined(typeof(NodeKind), shiftedValue))
            {
                return (NodeKind)shiftedValue;
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "The given SymbolKind is not supported by the importer.");
            }
        }

        /// <summary>
        /// Converts a <see cref="NodeKind"/> to an enumeration of <see cref="SymbolKind"/>.
        /// </summary>
        /// <param name="kind">The node kind to convert.</param>
        /// <returns>The corresponding symbol kinds.</returns>
        public static IEnumerable<SymbolKind> ToSymbolKind(this NodeKind kind)
        {
            if (kind == NodeKind.All)
            {
                foreach (SymbolKind symbolKind in Enum.GetValues(typeof(SymbolKind)).Cast<SymbolKind>())
                {
                    yield return symbolKind;
                }
            }
            else
            {
                foreach (NodeKind nodeKind in Enum.GetValues(typeof(NodeKind)).Cast<NodeKind>().Where(x => x.HasFlag(kind)))
                {
                    // This has to do the inverse to the above, i.e., log2, to get the original enum value.
                    int nodeKindValue = (int)nodeKind;
                    int symbolKindValue = (int)Math.Log(nodeKindValue, 2) + 1;
                    // If the enum is not defined, we don't throw an exception, because we have a flag enum
                    // with certain values (like None) that are not defined in the original enum.
                    if (Enum.IsDefined(typeof(SymbolKind), symbolKindValue))
                    {
                        yield return (SymbolKind)symbolKindValue;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Provides helper extensions methods to convert between <see cref="DiagnosticSeverity"/>
    /// and <see cref="DiagnosticKind"/>.
    /// </summary>
    public static class DiagnosticKindExtensions
    {
        /// <summary>
        /// Converts a <see cref="DiagnosticSeverity"/> to a <see cref="DiagnosticKind"/>.
        /// </summary>
        /// <param name="severity">The diagnostic severity to convert.</param>
        /// <returns>The corresponding diagnostic kind.</returns>
        public static DiagnosticKind ToDiagnosticKind(this DiagnosticSeverity severity)
        {
            // By taking the power of 2, we can use the original enum values as flags.
            int shiftedValue = 1 << (int)(severity - 1);
            if (Enum.IsDefined(typeof(DiagnosticKind), shiftedValue))
            {
                return (DiagnosticKind)shiftedValue;
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(severity), severity, "The given DiagnosticSeverity is not supported by the importer.");
            }
        }

        /// <summary>
        /// Converts a <see cref="DiagnosticKind"/> to an enumeration of <see cref="DiagnosticSeverity"/>.
        /// </summary>
        /// <param name="kind">The diagnostic kind to convert.</param>
        /// <returns>The corresponding diagnostic severities.</returns>
        public static IEnumerable<DiagnosticSeverity> ToDiagnosticSeverity(this DiagnosticKind kind)
        {
            if (kind == DiagnosticKind.All)
            {
                foreach (DiagnosticSeverity diagnosticSeverity in Enum.GetValues(typeof(DiagnosticSeverity)).Cast<DiagnosticSeverity>())
                {
                    yield return diagnosticSeverity;
                }
            }
            else
            {
                foreach (DiagnosticKind diagnosticKind in Enum.GetValues(typeof(DiagnosticKind)).Cast<DiagnosticKind>().Where(x => x.HasFlag(kind)))
                {
                    // This has to do the inverse to the above, i.e., log2, to get the original enum value.
                    int diagnosticKindValue = (int)diagnosticKind;
                    int diagnosticSeverityValue = (int)Math.Log(diagnosticKindValue, 2) + 1;
                    // If the enum is not defined, we don't throw an exception, because we have a flag enum
                    // with certain values (like None) that are not defined in the original enum.
                    if (Enum.IsDefined(typeof(DiagnosticSeverity), diagnosticSeverityValue))
                    {
                        yield return (DiagnosticSeverity)diagnosticSeverityValue;
                    }
                }
            }
        }

        public static string Name(this DiagnosticSeverity severity)
        {
            return severity switch
            {
                DiagnosticSeverity.Error => NumericAttributeNames.LspError.Name(),
                DiagnosticSeverity.Warning => NumericAttributeNames.LspWarning.Name(),
                DiagnosticSeverity.Information => NumericAttributeNames.LspInfo.Name(),
                DiagnosticSeverity.Hint => NumericAttributeNames.LspHint.Name(),
                _ => throw new ArgumentOutOfRangeException(nameof(severity), severity, null)
            };
        }
    }
}
