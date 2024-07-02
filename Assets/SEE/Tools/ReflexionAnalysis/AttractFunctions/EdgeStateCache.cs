using MoreLinq;
using SEE.DataModel.DG;
using SEE.Tools.ReflexionAnalysis;
using System.Collections.Generic;
using System.Linq;

namespace Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions
{
    /// <summary>
    /// This class can be used to retrieve edge states given a certain mapping constellation.
    /// If a edge state was already calculated it will be returned directly from the cache 
    /// without actually mapping nodes within the associated <see cref="ReflexionGraph"/> object.
    /// </summary>
    public class EdgeStateCache
    {
        /// <summary>
        /// Dictionary containing edge states for keys representing a 
        /// mapping constellelation.
        /// </summary>
        private Dictionary<string, State> cache = new();

        /// <summary>
        /// Given reflexion graph object this cache is working on.
        /// </summary>
        private ReflexionGraph reflexionGraph;

        /// <summary>
        /// This constructor initializes a new instance of <see cref="EdgeStateCache"/>
        /// for a given reflexion graph.
        /// </summary>
        /// <param name="graph">Given reflexion graph this object is reading and writing on.</param>
        public EdgeStateCache(ReflexionGraph graph) 
        {
            this.reflexionGraph = graph;
        }

        /// <summary>
        /// Returns a state for a given edge as if a given candidate node
        /// was mapped to a given cluster node. The edge have to be associated 
        /// with the candidate node or its subtree. The candidate 
        /// will be mapped and removed from the cluster during a 
        /// cache miss to calculate the necessary states and cache keys.
        /// 
        /// The returned state is valid as long as:
        /// - The neighbor of the incoming or outgoing edge which is not part of the subtree is mapped.
        /// This will be checked before the state is retrieved.
        /// - The architecture dependencies does not change within the architecture subgraph of 
        /// the reflexion graph.
        /// 
        /// If the neighbor is not mapped during this call, the State
        /// <see cref="State.Unmapped"/> will be returned, but not 
        /// written to the cache.
        /// 
        /// </summary>
        /// <param name="clusterId">Cluster node id the candidate node is potentially mapped to.</param>
        /// <param name="candidateId">Candidate node id of the node that is potentially mapped to.</param>
        /// <param name="edgeId">Id of the edge of which the potential state should be retrieved.</param>
        /// <param name="outgoing">Wether the edge is outgoing from the or incoming to the the candidate subtree.</param>
        /// <returns>The state the given edge would be in, if the candidate is mapped to the cluster.</returns>
        public State GetFromCache(string clusterId, string candidateId, string edgeId, bool outgoing)
        {
            Node candidate = reflexionGraph.GetNode(candidateId);
            Edge edge = reflexionGraph.GetEdge(edgeId);
            Node subtreeNeighbor = outgoing ? edge.Target : edge.Source;

            Node cluster = reflexionGraph.GetNode(clusterId ?? string.Empty);

            Node neighborCluster = reflexionGraph.MapsTo(subtreeNeighbor);

            if(neighborCluster == null)
            {
                return State.Unmapped;
            }

            if (candidateId.Equals(subtreeNeighbor.ID) && !clusterId.Equals(neighborCluster.ID))
            {
                // TODO: When can this case happen?
                return State.Undefined;
            }

            string neighborClusterID = neighborCluster.ID;

            string key = $"{candidateId}#{clusterId}#{subtreeNeighbor.ID}#{neighborClusterID}#{edgeId}";
            if(!this.cache.ContainsKey(key))
            {
                UpdateCache(candidate, cluster);
            } 

            return this.cache[key];
        }

        /// <summary>
        /// This method maps the given candidate node to the given cluster node and writes 
        /// for every edge associated with its subtree its corresponding state 
        /// to the cache for generated key.
        /// 
        /// If the given candidate is already mapped, its is removed and remapped silently 
        /// to its previous cluster.
        /// 
        /// The keys for the cache are generated based on the IDs of the graph elements:
        /// key = "{candidate.ID}#{cluster.ID}#{neighborOfSubtree.ID}#{neighborCluster.ID}#{edge.ID}"
        /// 
        /// </summary>
        /// <param name="candidate">Given candidate node</param>
        /// <param name="cluster">Given cluster node</param>
        private void UpdateCache(Node candidate, Node cluster)
        {
            Node mapsTo = reflexionGraph.MapsTo(candidate);

            bool mappingChangeRequired = mapsTo != cluster && mapsTo != null;

            Node explicitlyMappedNode = candidate;

            if (mappingChangeRequired)
            {
                while (explicitlyMappedNode != null && !reflexionGraph.IsExplicitlyMapped(explicitlyMappedNode))
                {
                    explicitlyMappedNode = explicitlyMappedNode.Parent;
                }

                reflexionGraph.RemoveFromMappingSilent(explicitlyMappedNode);
            }

            reflexionGraph.AddToMappingSilent(cluster, candidate);

            List<Edge> subtreeEdgesIncoming = new List<Edge>();
            List<Edge> subtreeEdgesOutgoing = new List<Edge>();
            IList<Node> subtreeNodes = candidate.PostOrderDescendants();
            subtreeNodes.ForEach(d => subtreeEdgesIncoming.AddRange(d.Incomings.Where(x => x.IsInImplementation())));
            subtreeNodes.ForEach(d => subtreeEdgesOutgoing.AddRange(d.Outgoings.Where(x => x.IsInImplementation())));

            subtreeEdgesIncoming.ForEach(e => WriteToCache(e, e.Source));
            subtreeEdgesOutgoing.ForEach(e => WriteToCache(e, e.Target));

            void WriteToCache(Edge edge, Node neighborOfSubtree)
            {
                Node neighborCluster = reflexionGraph.MapsTo(neighborOfSubtree);

                if (neighborCluster != null)
                {
                    string key = $"{candidate.ID}#{cluster.ID}#{neighborOfSubtree.ID}#{neighborCluster.ID}#{edge.ID}";
                    this.cache[key] = edge.State();
                }
            }

            reflexionGraph.RemoveFromMappingSilent(candidate);

            if (mappingChangeRequired)
            {
                reflexionGraph.AddToMappingSilent(mapsTo, explicitlyMappedNode);
            }
        }

        /// <summary>
        /// Clears this cache.
        /// </summary>
        public void ClearCache()
        {
            cache?.Clear();
        }
    }
}
