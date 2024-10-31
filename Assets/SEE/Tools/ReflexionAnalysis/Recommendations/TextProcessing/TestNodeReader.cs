using SEE.DataModel.DG;
using System.Collections.Generic;

namespace Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions
{
    /// <summary>
    /// Class used for <see cref="LanguageAttract"/> function objects within the Testcases.
    /// </summary>
    public class TestNodeReader : INodeReader
    {
        /// <summary>
        /// Lookup table containing strings for node ids
        /// </summary>
        Dictionary<string, string> lookUp = new();

        /// <summary>
        /// Reads the source code region from a look up table using the node id.
        /// </summary>
        /// <param name="node">Given node</param>
        /// <returns>Source code region as a string</returns>
        public string ReadRegion(Node node)
        {
            if(lookUp.ContainsKey(node.ID))
            {
                return lookUp[node.ID];
            } 
            else
            {
                return "word1";
            }
        }

        /// <summary>
        /// Writes a string to the look up table for a given node id.
        /// </summary>
        /// <param name="id">Given node id</param>
        /// <param name="lookUp">String written to the table</param>
        public void SetLookUp(string id, string lookUp)
        {
            this.lookUp[id] = lookUp;
        }
    }
}
