using System.Collections.Generic;

namespace SEE.DataModel.DG
{
    /// <summary>
    /// Allows one to determine whether there is any difference between two 
    /// graph elements in terms of selected numeric attributes.
    /// </summary>
    public class NumericAttributeDiff : GraphElementDiff
    {
        /// <summary>
        /// Constructor.
        /// 
        /// Precondition: every attribute in <paramref name="attributes"/> must denote
        /// a numeric attribute (float or int).
        /// </summary>
        /// <param name="attributes">the attributes ought to be used for the comparison</param>
        public NumericAttributeDiff(ICollection<string> attributes)
        {
            this.attributes = attributes;
        }

        /// <summary>
        /// The attributes ought to be used for the comparison.
        /// </summary>
        private readonly ICollection<string> attributes;

        /// <summary>
        /// True whether there is a difference between <paramref name="left"/> and
        /// <paramref name="right"/> in terms of the numeric attributes set in the
        /// constructor.
        /// </summary>
        /// <param name="left">left graph element to be compared</param>
        /// <param name="right">right graph element to be compared</param>
        /// <returns>true if there is any difference</returns>
        bool GraphElementDiff.AreDifferent(GraphElement left, GraphElement right)
        {
            if (left == null)
            {
                return right != null;
            }
            else if (right == null)
            {
                return true;
            }
            else
            {
                foreach (string attribute in attributes)
                {
                    if (left.TryGetNumeric(attribute, out float leftValue))
                    {
                        // left has attribute
                        if (right.TryGetNumeric(attribute, out float rightValue))
                        {
                            // and right has attribute; they must be different
                            return leftValue != rightValue;
                        }
                        else
                        {
                            // right does not have attribute
                            return true;
                        }
                    }
                    else
                    {
                        // left does not have the attribute => right must neither have it;
                        // if right has the attribute, we must return true because there
                        // is a difference
                        return right.TryGetNumeric(attribute, out float _);
                    }
                }
                return false;
            }
        }
    }
}