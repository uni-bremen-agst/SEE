using Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions;
using System.Collections.Generic;

namespace Assets.SEE.Tools.ReflexionAnalysis
{
    /// <summary>
    /// Iterface providing methods to implement a recommendation filter. 
    /// The recommendation filter determines which candidates are recommended 
    /// for which clusters based on the current attraction values. The filter 
    /// also calculates the recommendations that are allowed to be mapped automatically. 
    /// </summary>
    public interface IRecommendationFilter
    {
        /// <summary>
        /// Returns the current attraction matrix as a list of mapping pairs.
        /// </summary>
        /// <returns></returns>
        IEnumerable<MappingPair> GetMappingPairs();

        /// <summary>
        /// Returns the recommended mapping pairs the filter does allow to be mapped automatically
        /// </summary>
        /// <returns></returns>
        IEnumerable<MappingPair> GetAutomaticMappings();

        /// <summary>
        /// Returns the recommended mapping pairs
        /// </summary>
        /// <returns></returns>
        IEnumerable<MappingPair> GetRecommendations();

        /// <summary>
        /// Returns the recommended nodes for a given cluster id
        /// </summary>
        /// <param name="clusterId">given cluster id</param>
        /// <returns></returns>
        IEnumerable<MappingPair> GetRecommendationForCluster(string clusterId);

        /// <summary>
        /// Returns the recommended nodes for a given candidate id
        /// </summary>
        /// <param name="clusterId">given candidate id</param>
        /// <returns></returns>
        IEnumerable<MappingPair> GetRecommendationForCandidate(string candidateId);

        /// <summary>
        /// Removes a given candidate from the attraction matrix
        /// </summary>
        /// <param name="candidateId">given candidate id</param>
        void RemoveCandidate(string candidateId);

        /// <summary>
        /// Removes a given cluster from the attraction matrix
        /// </summary>
        /// <param name="clusterId">given cluster id</param>
        void RemoveCluster(string clusterId);

        /// <summary>
        /// Returns a specified mapping pair of the matrix
        /// </summary>
        /// <param name="candidateId">given candidate id</param>
        /// <param name="clusterId">given cluster id</param>
        /// <returns></returns>
        MappingPair GetMappingPair(string candidateId, string clusterId);

        /// <summary>
        /// This function is called when a attraction value of a mapping pair changes.
        /// This method is supposed to implement the management of the attraction value matrix.
        /// </summary>
        /// <param name="mappingPair">given mapping pair</param>
        void UpdateMappingPair(MappingPair mappingPair);

        /// <summary>
        /// Resets the attraction value matrix of this filter.
        /// </summary>
        void Reset();
    }
}
