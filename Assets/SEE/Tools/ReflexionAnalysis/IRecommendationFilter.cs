using Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.SEE.Tools.ReflexionAnalysis
{
    public interface IRecommendationFilter
    {
        IEnumerable<MappingPair> GetMappingPairs();

        IEnumerable<MappingPair> GetRecommendations();

        IEnumerable<MappingPair> GetRecommendationForCluster(string clusterId);

        IEnumerable<MappingPair> GetRecommendationForCandidate(string candidateId);

        void RemoveCandidate(string candidateId);

        void RemoveCluster(string clusterId);

        MappingPair GetMappingPair(string candidateId, string clusterId);

        void UpdateMappingPair(MappingPair mappingPair);

        void Reset();
    }
}
