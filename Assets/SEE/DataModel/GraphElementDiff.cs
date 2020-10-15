namespace SEE.DataModel.DG
{
    /// <summary>
    /// Allows one to determine whether there is any difference between two 
    /// graph elements.
    /// </summary>
    public interface GraphElementDiff
    {
        /// <summary>
        /// True whether there is a difference between <paramref name="left"/> and
        /// <paramref name="right"/>.
        /// </summary>
        /// <param name="left">left graph element to be compared</param>
        /// <param name="right">right graph element to be compared</param>
        /// <returns>true if there is any difference</returns>
        bool AreDifferent(GraphElement left, GraphElement right);
    }
}
