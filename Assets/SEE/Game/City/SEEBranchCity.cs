using SEE.DataModel.DG;
using SEE.Game.Evolution;
using SEE.UI.RuntimeConfigMenu;
using SEE.Utils;
using SEE.Utils.Config;
using SEE.Utils.Paths;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Game.City
{
    /// <summary>
    /// Manages settings of the graph data showing a single version of a software
    /// system and its differences relative to a baseline graph (a former version
    /// of the same software system).
    /// </summary>
    public class SEEBranchCity : SEECity
    {
        /// <summary>
        /// This graph will be used to store a graph and will be used as a flag
        /// </summary>
        static Graph tempGraph = null;

        /// <summary>
        /// The path to the GXL file containing the graph data.
        /// Note that any deriving class may use multiple GXL paths from which the single city is constructed.
        /// </summary>
        [ShowInInspector, Tooltip("Path of GXL file for the baseline graph"), TabGroup(DataFoldoutGroup), RuntimeTab(DataFoldoutGroup)]
        public FilePath BaselineGXLPath = new();

        /// <summary>
        /// Attributes to mark changes
        /// </summary>
        public MarkerAttributes MarkerAttributes = new();

        /// <summary>
        /// First, <see cref=">SEECity.LoadData"/> will be called.
        /// The resulting graph is available in <see cref="LoadedGraph"/> afterwards.
        /// Then, the differences of <see cref="LoadedGraph"/> with respect to a
        /// baseline graph stored in <see cref="BaselineGXLPath"/> will be merged
        /// into <see cref="LoadedGraph"/>.
        ///
        /// If a graph element is in the baseline but not in <see cref="LoadedGraph"/>,
        /// it will be added to <see cref="LoadedGraph"/> and marked by toggle <see cref="IsDeleted"/>.
        ///
        /// If a graph element is in <see cref="LoadedGraph"/> but not in the baseline,
        /// it will be marked by toggle <see cref="IsNew"/>.
        ///
        /// If a graph element is in <see cref="LoadedGraph"/> and in the baseline, but has
        /// any change in any of the existing numeric attributes, it will be marked by toggle <see cref="IsNew"/>.
        /// A change of an attribute could be the addition or deletion of the attribute as well as
        /// a change of the attribute's value. Note that only numeric attributes are considered.
        ///
        /// Whether two graph elements in the two graphs are considered the logically identical
        /// graph element is determined by their <see cref="Node.ID"/>.
        /// </summary>
        /// <remarks>This method loads only the data, but does not actually render the graph.</remarks>
        [Button(ButtonSizes.Small)]
        [ButtonGroup(DataButtonsGroup), RuntimeButton(DataButtonsGroup, "Load Data")]
        [PropertyOrder(DataButtonsGroupOrderLoad)]
        public override void LoadData()
        {
            base.LoadData();
            if (string.IsNullOrEmpty(BaselineGXLPath.Path))
            {
                Debug.LogError("Empty path for baseline GXL.\n");
            }
            else
            {
                Merge(BaselineGXLPath.Path);
            }
        }

        /// <summary>
        /// Merges the changes of nodes and edges of the <see cref="LoadedGraph"/> with
        /// respect to a baseline graph. The baseline graph is loaded from <paramref name="pathBaselineGXL"/>.
        ///
        /// </summary>
        /// <param name="pathBaselineGXL"></param>
        private void Merge(string pathBaselineGXL)
        {
            Graph baseline = LoadGraph(pathBaselineGXL);

            tempGraph = null;
            // TODO (#696): Normally we would call LoadMetrics() to add additional metrics
            // for the graph stored in a separate CSV file. That is done for LoadedGraph.
            // Do we want to do that for this baseline graph, too? If so, we need to add
            // another CSV path to this class.

            InspectSchema(baseline);

            //If baseline has more nodes than LoadedGraph then the baseline has to be the LoadedGraph
            if (baseline.Nodes().Count > LoadedGraph.Nodes().Count)
            {
                tempGraph = baseline;
                baseline = LoadedGraph;
                LoadedGraph = tempGraph;
            }

            MergeNodes(baseline);
            MergeEdges(baseline);
        }

        /// <summary>
        /// Handling nodes of <see cref="LoadedGraph"/> relative to former <paramref name="baseline"/>.
        /// </summary>
        /// <param name="baseline">a predecessor graph of <see cref="LoadedGraph">/></param>
        private void MergeNodes(Graph baseline)
        {
            LoadedGraph.Diff(baseline,
                          g => g.Nodes(),
                          (g, id) => g.GetNode(id),
                          GraphExtensions.AttributeDiff(LoadedGraph, baseline),
                          new NodeEqualityComparer(),
                          out ISet<Node> addedNodes,
                          out ISet<Node> removedNodes,
                          out ISet<Node> changedNodes,
                          out ISet<Node> equalNodes);

            MergeGraphElements(addedNodes, removedNodes, changedNodes, n => { LoadedGraph.AddNode(n); }, baseline);
        }

        /// <summary>
        /// Handling edges of <see cref="LoadedGraph"/> relative to former <paramref name="baseline"/>.
        /// </summary>
        /// <param name="baseline">a predecessor graph of <see cref="LoadedGraph">/></param>
        private void MergeEdges(Graph baseline)
        {
            LoadedGraph.Diff(baseline,
                        g => g.Edges(),
                        (g, id) => g.GetEdge(id),
                        GraphExtensions.AttributeDiff(LoadedGraph, baseline),
                        new EdgeEqualityComparer(),
                        out ISet<Edge> addedEdges,
                        out ISet<Edge> removedEdges,
                        out ISet<Edge> changedEdges,
                        out ISet<Edge> equalEdges);

            MergeGraphElements(addedEdges, removedEdges, changedEdges, AddEdge, baseline);

            // Adds edge to LoadedGraph. Note: edge is assumed to be cloned
            // from an edge belonging to the baseline graph that has no
            // corresponding edge in LoadedGraph, thus, was deleted.
            void AddEdge(Edge edge)
            {
                // edge is cloned from a baseline edge, but after the cloning its source
                // and target are nodes in the baseline graph. edge will be added to the
                // LoadedGraph, hence, we need to adjust its source and target to the
                // corresponding nodes in LoadedGraph.
                edge.Source = LoadedGraph.GetNode(edge.Source.ID);
                edge.Target = LoadedGraph.GetNode(edge.Target.ID);
                // edge was clone from a baseline edge, but the cloned edge does
                // not belong to any graph (its graph is null). As a consequence,
                // we do not need to reset ItsGraph.
                Debug.Log(LoadedGraph);
                LoadedGraph.AddEdge(edge);
            }
        }

        /// <summary>
        /// A method that adds <paramref name="graphElement"/> to <see cref="LoadedGraph"/>.
        /// </summary>
        /// <typeparam name="T">type of <see cref="GraphElement"/></typeparam>
        /// <param name="graphElement">the element to be added</param>
        private delegate void AddToGraph<T>(T graphElement) where T : GraphElement;

        /// <summary>
        /// Marks all <paramref name="added"/> as <see cref="IsNew"/>.
        /// Marks all <paramref name="changed"/> as <see cref="IsChanged"/>.
        /// Adds all <paramref name="removed"/> to <see cref="LoadedGraph"/>
        /// and marks them as <see cref="IsDeleted"/>.
        ///
        /// Assumption: <paramref name="added"/>, <paramref name="changed"/>, and
        /// <paramref name="removed"/> are mutually exclusive.
        /// </summary>
        /// <typeparam name="T">type of <see cref="GraphElement"/></typeparam>
        /// <param name="added">added graph elements</param>
        /// <param name="removed">removed graph elements</param>
        /// <param name="changed">changed graph elements</param>
        /// <param name="addToGraph">adds <paramref name="graphElement"/> to <see cref="LoadedGraph"/>;
        /// will be called for all elements in <paramref name="removed"/></param>
        private static void MergeGraphElements<T>(ISet<T> added, ISet<T> removed, ISet<T> changed, AddToGraph<T> addToGraph, Graph baseline)
            where T : GraphElement
        {
            //SetToggle will be IsDeleted if baseline and LoadedGraph have been swapped
            _ = added.ForEach(node => { node.SetToggle(tempGraph != null ? ChangeMarkers.IsDeleted : ChangeMarkers.IsNew); });
            _ = changed.ForEach(node => { UpdateChanged(node); });
            _ = removed.ForEach(node => { MergeRemoved(node); });

            // Adds graphElement to LoadedGraph and marks it as deleted.
            void MergeRemoved(T graphElement)
            {
                T removedGraphElement = graphElement.Clone() as T;
                addToGraph(removedGraphElement);
                //SetToggle will be IsNew if baseline and LoadedGraph have been swapped
                removedGraphElement.SetToggle(tempGraph != null ? ChangeMarkers.IsNew : ChangeMarkers.IsDeleted);
            }

            // Marks given graph element as changed.
            // Note: graphElement is from baseline graph.
            void UpdateChanged(T graphElement)
            {
                graphElement.SetToggle(ChangeMarkers.IsChanged);
                //Calculates the diff between the corresponding metrics. Diff = new - old

                //The corresponding Node from the baseline
                Node baseNode = baseline.GetNode(graphElement.ID);
                List<string> intMetrics = new List<string>(baseNode.AllIntAttributeNames());
                List<string> intMetrics2 = new List<string>(graphElement.AllIntAttributeNames());
                intMetrics.ForEach(metric =>
                {
                    string diffStr = metric + ".diff";
                    string deletedStr = metric + ".deleted";

                    int baselineInt = baseNode.GetInt(metric);
                    graphElement.TryGetInt(metric, out int graphInt);
                    int diffInt = (tempGraph == null) ? (graphInt - baselineInt) : (baselineInt - graphInt);
                    //If a metric has been deleted it will additionaly be marked with ".deleted" otherwise it has a ".diff"
                    if (intMetrics2.Contains(metric))
                    {
                        graphElement.SetInt(diffStr, diffInt);
                    }
                    else
                    {
                        graphElement.SetInt(deletedStr, diffInt);
                    }
                });
            }
        }

        /// <summary>
        /// Resets everything that is specific to a given graph and removes the component.
        /// </summary>
        [Button(ButtonSizes.Small, Name = "Reset Data")]
        [ButtonGroup(ResetButtonsGroup), RuntimeButton(ResetButtonsGroup, "Reset")]
        [PropertyOrder(ResetButtonsGroupOrderReset)]
        public override void Reset()
        {
            base.Reset();
            // Destroy Component.
            if (gameObject.TryGetComponent(out EvolutionRenderer evolutionRenderer))
            {
                DestroyImmediate(evolutionRenderer);
            }
        }

        #region Configuration file input/output
        //--------------------------------
        // Configuration file input/output
        //--------------------------------

        /// <summary>
        /// Label of attribute <see cref="BaselineGXLPath"/> in the configuration file.
        /// </summary>
        private const string baselineGXLPathLabel = "BaselineGXLPath";

        /// <summary>
        /// Label of attribute <see cref="MarkerAttributes"/> in the configuration file.
        /// </summary>
        private const string markerAttributesLabel = "markerAttributes";

        protected override void Save(ConfigWriter writer)
        {
            base.Save(writer);
            BaselineGXLPath.Save(writer, baselineGXLPathLabel);
            MarkerAttributes.Save(writer, markerAttributesLabel);
        }

        protected override void Restore(Dictionary<string, object> attributes)
        {
            base.Restore(attributes);
            BaselineGXLPath.Restore(attributes, baselineGXLPathLabel);
            MarkerAttributes.Restore(attributes);
        }
        #endregion
    }
}
