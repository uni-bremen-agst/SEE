using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.Tools.ReflexionAnalysis;

namespace Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions
{
    public class NoAttract : AttractFunction
    {
        public NoAttract(ReflexionGraph reflexionGraph, CandidateRecommendation candidateRecommendation, AttractFunctionConfig config) : base(reflexionGraph, candidateRecommendation, config)
        {
        }

        public override string DumpTrainingData()
        {
            return string.Empty;
        }

        public override bool EmptyTrainingData()
        {
            return true;
        }

        public override double GetAttractionValue(Node node, Node cluster)
        {
            return 0.0;
        }

        public override void HandleChangedCandidate(Node cluster, Node changedNode, ChangeType changeType)
        {
            return;
        }

        public override void HandleChangedState(EdgeChange edgeChange)
        {
            return;
        }
    }
}
