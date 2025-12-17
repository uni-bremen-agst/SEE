namespace SEE.DataModel.DG
{
    /// <summary>
    /// Allows one to determine whether there is any difference between two
    /// graph elements.
    /// </summary>
    public interface IGraphElementDiff
    {
        /// <summary>
        /// True whether there is a difference between <paramref name="left"/> and
        /// <paramref name="right"/>.
        /// </summary>
        /// <param name="left">Left graph element to be compared.</param>
        /// <param name="right">Right graph element to be compared.</param>
        /// <returns>True if there is any difference.</returns>
        bool AreDifferent(GraphElement left, GraphElement right);
    }
}
