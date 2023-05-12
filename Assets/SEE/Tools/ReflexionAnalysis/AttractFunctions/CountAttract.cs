using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.Tools.ReflexionAnalysis;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions
{
    public class CountAttract : AttractFunction
    {
        private float delta;

        private float phi;

        private Dictionary<string, int> overallValues;

        private Dictionary<string, int> toOtherValues;

        private Dictionary<string, int> mappingCount;

        public CountAttract(ReflexionGraph graph, string targetType) : base(graph, targetType)
        {
            overallValues = new Dictionary<string, int>();
            toOtherValues= new Dictionary<string, int>();
            mappingCount = new Dictionary<string, int>();
            delta = 0;
            phi = 0.5f;
        }

        public override double GetAttractionValue(Node node, Node cluster)
        {
            if (!node.Type.Equals(targetType)) return 0;
            if (overallValues.TryGetValue(node.ID, out int overall))
            {
                UnityEngine.Debug.Log($"Overall({node.ID}) = {overall}");
                double toOthers = GetToOthersValue(node, cluster);

                UnityEngine.Debug.Log($"CountAttract({node.ID},{cluster.ID}) = {overall - toOthers}");
                return overall - toOthers;
            } 
            else
            {
                // TODO: dirty? does no overall value imply always 0?
                return 0;
            };
        }

        public double GetToOthersValue(Node node, Node cluster)
        {
            List<Edge> implementationEdges = GetImplementationEdges(node);
            double toOthers = 0;
            foreach (Edge edge in implementationEdges)
            {
                Node neighborOfMappedEntity = edge.Source.Equals(node) ? edge.Target : edge.Source;
                Node neighborCluster = reflexionGraph.MapsTo(neighborOfMappedEntity);
  
                // TODO: Equals does not work? Use IDs instead.
                if (neighborCluster == null || /*neighborCluster.Equals(cluster)*/ neighborCluster.ID.Equals(cluster.ID)) continue;

                double weight = 1.0;

                // TODO: use phi weight
                UnityEngine.Debug.Log($"neighbor edge {edge.ID} is in state {edge.State()}");
                if (edge.State() == State.Allowed)
                {
                    UnityEngine.Debug.Log($"State is allowed. Phi value will be applied.");
                    weight *= phi;
                }

                toOthers += weight;
            }
            UnityEngine.Debug.Log($"ToOthers({node.ID},{cluster.ID}) = {toOthers}");
            return toOthers;
        }

        public override void HandleMappedEntities(Node cluster, List<Node> mappedEntities, ChangeType changeType)
        {
            foreach (Node mappedEntity in mappedEntities)
            {
                List<Edge> implementationEdges = GetImplementationEdges(mappedEntity);
                foreach (Edge edge in implementationEdges)
                {
                    Node neighborOfMappedEntity = edge.Source.Equals(mappedEntity) ? edge.Target : edge.Source;
                    UpdateOverallTable(neighborOfMappedEntity, edge, changeType);
                    // TODO: Update To others table?
                    UpdateMappingCountTable(neighborOfMappedEntity, changeType);
                }
            }
        }

        public void UpdateOverallTable(Node NeighborOfMappedEntity, Edge edge, ChangeType changeType)
        {
            if (!overallValues.ContainsKey(NeighborOfMappedEntity.ID)) overallValues.Add(NeighborOfMappedEntity.ID, 0);
            int edgeWeight = GetEdgeWeight(edge);
            if (changeType == ChangeType.Removal) edgeWeight *= -1;
            overallValues[NeighborOfMappedEntity.ID] += edgeWeight;
        }

        public void UpdateMappingCountTable(Node NeighborOfMappedEntity, ChangeType changeType)
        {
            if (!mappingCount.ContainsKey(NeighborOfMappedEntity.ID)) mappingCount.Add(NeighborOfMappedEntity.ID, 0);
            int count = 1;
            if (changeType == ChangeType.Removal) count *= -1;
            mappingCount[NeighborOfMappedEntity.ID] += count;
        }

        public void UpdateToOthersTable()
        {
            // TODO: possible?
        }

        private int GetEdgeWeight(Edge edge)
        {
            // TODO get correct Edge Weight
            return 1;
        }

        private List<Edge> GetImplementationEdges(Node targetedEntity)
        {
            // TODO: How to determine the right edges, more processing needed
            // TODO: Which types are relevant?
            List<Edge> edges = new List<Edge>();
            edges.AddRange(targetedEntity.Incomings);
            edges.AddRange(targetedEntity.Outgoings);
            edges = edges.Distinct().Where(x => x.IsInImplementation()).ToList();
            return edges;
        }
    }
}
