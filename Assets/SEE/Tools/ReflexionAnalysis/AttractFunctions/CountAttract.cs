using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.Tools.ReflexionAnalysis;
using System.Collections.Generic;


namespace Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions
{
    public class CountAttract : AttractFunction
    {
        private float delta;

        private float phi;

        private Dictionary<string, int> overallValues;

        private Dictionary<string, int> mappingCount;

        /// <summary>
        /// 
        /// </summary>
        public float Phi { get => phi; set => phi = value; }

        public CountAttract(ReflexionGraph graph, string targetType) : base(graph, targetType)
        {
            overallValues = new Dictionary<string, int>();
            mappingCount = new Dictionary<string, int>();
            delta = 0;
            Phi = 1f;
        }

        public override double GetAttractionValue(Node candidateNode, Node cluster)
        {
            if (!candidateNode.Type.Equals(targetType)) return 0;
            if (overallValues.TryGetValue(candidateNode.ID, out int overall))
            {
                UnityEngine.Debug.Log($"Overall({candidateNode.ID}) = {overall}");
                double toOthers = GetToOthersValue(candidateNode, cluster);

                UnityEngine.Debug.Log($"CountAttract({candidateNode.ID},{cluster.ID}) = {overall - toOthers}");
                return overall - toOthers;
            } 
            else
            {
                // TODO: dirty? does no overall value imply always 0?
                // TODO: add exception here
                return 0;
            };
        }

        public double GetToOthersValue(Node candidateNode, Node cluster)
        {
            List<Edge> implementationEdges = candidateNode.GetImplementationEdges();
            double toOthers = 0;

            this.reflexionGraph.SuppressNotifications = true;
            this.reflexionGraph.AddToMapping(candidateNode, cluster);

            foreach (Edge edge in implementationEdges)
            {
                Node neighborOfCandidate = edge.Source.Equals(candidateNode) ? edge.Target : edge.Source;
                Node neighborCluster = reflexionGraph.MapsTo(neighborOfCandidate);
  
                // TODO: Equals does not work? Use IDs instead.
                if (neighborCluster == null || neighborCluster.ID.Equals(cluster.ID)) continue;

                double weight = 1.0;
                
                UnityEngine.Debug.Log($"{edge.ID} Get Reflexion.State = {edge.State()}");
                if (edge.State() == State.Allowed || edge.State() == State.ImplicitlyAllowed)
                {
                    UnityEngine.Debug.Log($"State is allowed. Phi value will be applied.");
                    weight *= Phi;
                }

                toOthers += weight;
            }

            this.reflexionGraph.RemoveFromMapping(candidateNode);
            this.reflexionGraph.SuppressNotifications = false;

            UnityEngine.Debug.Log($"ToOthers({candidateNode.ID},{cluster.ID}) = {toOthers}");
            return toOthers;
        }

        public override void HandleMappedEntities(Node cluster, List<Node> mappedEntities, ChangeType changeType)
        {
            foreach (Node mappedEntity in mappedEntities)
            {
                List<Edge> implementationEdges = mappedEntity.GetImplementationEdges();
                foreach (Edge edge in implementationEdges)
                {
                    Node neighborOfMappedEntity = edge.Source.Equals(mappedEntity) ? edge.Target : edge.Source;
                    UpdateOverallTable(neighborOfMappedEntity, edge, changeType);
                    
                    // TODO: Is there a way to also update a datastructure for the ToOthers value efficiently?
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

        private int GetEdgeWeight(Edge edge)
        {
            // TODO: get correct Edge Weight
            return 1;
        }
    }
}
