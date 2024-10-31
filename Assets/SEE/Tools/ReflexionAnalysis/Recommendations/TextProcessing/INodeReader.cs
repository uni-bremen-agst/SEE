using SEE.DataModel.DG;

namespace Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions
{
    /// <summary>
    /// Interface used by the <see cref="LanguageAttract"/> function object to read source code regions from nodes.
    /// </summary>
    public interface INodeReader
    {
        /// <summary>
        /// Reads the source code region from a Node .
        /// </summary>
        /// <param name="node">Given node</param>
        /// <returns>Source code region as a string</returns>
        string ReadRegion(Node node);
    }
}
