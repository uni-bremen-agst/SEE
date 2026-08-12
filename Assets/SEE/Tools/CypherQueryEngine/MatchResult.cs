using System.Collections.Generic;
using System.Linq;
using System;
using SEE.DataModel.DG;

namespace Cypher
{
    /// <summary>
    /// Pairs Variables with GraphElements according to MATCH Pattern.
    /// </summary>
    public class MatchResult
    {
        /// <summary>
        /// Dictionary for the Pairings
        /// </summary>
        public Dictionary<string, GraphElement> Variables { get; set; }

        /// <summary>
        /// Constructor for MatchResult
        /// </summary>
        /// <param name="dict">Pairings</param>
        public MatchResult(Dictionary<string, GraphElement> dict)
        {
            Variables = dict;
        }
    }
}
