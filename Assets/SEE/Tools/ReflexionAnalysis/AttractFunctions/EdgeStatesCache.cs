using SEE.DataModel.DG;
using SEE.Tools.ReflexionAnalysis;
using System.Collections.Generic;

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

                reflexionGraph.RemoveFromMappingSilent(mapsTo, explicitlyMappedNode);
            }

            reflexionGraph.AddToMappingSilent(cluster, candidate);

            IEnumerable<Edge> edges = candidate.GetImplementationEdges();

            foreach (Edge edge in edges)
            {
                Node candidateNeighbor = edge.Source.Equals(candidate) ? edge.Target : edge.Source;
                Node neighborCluster = reflexionGraph.MapsTo(candidateNeighbor);

                if (neighborCluster != null)
                {
                    string key = $"{cluster.ID}#{neighborCluster.ID}#{edge.ID}";
                    this.cache[key] = edge.State();
                }
            }

            reflexionGraph.RemoveFromMappingSilent(cluster, candidate);

            if (mappingChangeRequired)
            {
                reflexionGraph.AddToMappingSilent(mapsTo, explicitlyMappedNode);
            }
        }

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

            string key = $"{clusterId}#{neighborClusterID}#{edgeId}";
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
