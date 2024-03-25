using SEE.DataModel.DG;
using System.Collections.Generic;

namespace Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions
{
    public class NodeReaderTest : INodeReader
    {
        Dictionary<string, string> lookUp = new();

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

        public void SetLookUp(string id, string lookUp)
        {
            this.lookUp[id] = lookUp;
        }
    }
}
