namespace SEE.DataModel.DG.IO
{
    /// <summary>
    /// Input and output of graph data in GXL format.
    /// </summary>
    public abstract class GraphIO
    {
        /// <summary>
        /// The attribute name for the source region line length.
        /// We need to use this attribute for our SourceRange attribute.
        /// </summary>
        protected const string RegionLengthAttribute = "Source.Region_Length";

        /// <summary>
        /// The attribute name for the source region starting line.
        /// We need to use this attribute for our SourceRange attribute.
        /// </summary>
        protected const string RegionStartAttribute = "Source.Region_Start";
    }
}
