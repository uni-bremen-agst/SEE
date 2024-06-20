using MoreLinq;
using SEE.DataModel.DG;
using SEE.Tools.ReflexionAnalysis;
using System.Collections.Generic;
using System.Linq;

namespace Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions
{
    public class EdgeStatesCache
    {
        private Dictionary<string, State> cache = new();

        private ReflexionGraph reflexionGraph;

        public EdgeStatesCache(ReflexionGraph graph) 
        {
            this.reflexionGraph = graph;
        }

        private void UpdateCache(Node candidate, Node cluster)
        {
            Node mapsTo = reflexionGraph.MapsTo(candidate);

            bool mappingChangeRequired = mapsTo != cluster && mapsTo != null;

            Node explicitlyMappedNode = candidate;

            if (mappingChangeRequired)
            {
                while(explicitlyMappedNode != null && !reflexionGraph.IsExplicitlyMapped(explicitlyMappedNode))
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

        // TODO: Adjust wording and doc
        public State GetFromCache(string clusterId, string candidateId, string candidateNeighborId, string edgeId)
        {
            Node candidate = reflexionGraph.GetNode(candidateId);
            Node candidateNeighbor = reflexionGraph.GetNode(candidateNeighborId);

            Node cluster = reflexionGraph.GetNode(clusterId ?? string.Empty);

            Node neighborCluster = reflexionGraph.MapsTo(candidateNeighbor);

            if(neighborCluster == null)
            {
                return State.Unmapped; // TODO: Undefined or Unmapped?
            }

            if (candidateId.Equals(candidateNeighborId) && !clusterId.Equals(neighborCluster.ID))
            {
                // TODO: When can this case happen?
                return State.Undefined;
            }

            string neighborClusterID = neighborCluster.ID;

            string key = $"{candidateId}#{clusterId}#{candidateNeighbor.ID}#{neighborClusterID}#{edgeId}";
            if(!this.cache.ContainsKey(key))
            {
                UpdateCache(candidate, cluster);
            } 

            return this.cache[key];
        }

        public void ClearCache()
        {
            cache?.Clear();
        }
    }
}
