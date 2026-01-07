using System.Collections.Generic;

namespace SEE.DataModel.DG
{
    /// <summary>
    /// Allows one to determine whether there is any difference between two
    /// graph elements in terms of selected attributes.
    /// </summary>
    public class AttributeDiff : IGraphElementDiff
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="floatAttributes">The float attributes ought to be used for the comparison.</param>
        /// <param name="intAttributes">The integer attributes ought to be used for the comparison.</param>
        /// <param name="stringAttributes">The string attributes ought to be used for the comparison.</param>
        /// <param name="toggleAttributes">The toggle attributes ought to be used for the comparison.</param>
        public AttributeDiff
            (ICollection<string> floatAttributes,
             ICollection<string> intAttributes,
             ICollection<string> stringAttributes,
             ICollection<string> toggleAttributes)
        {
            this.floatAttributes = floatAttributes;
            this.intAttributes = intAttributes;
            this.stringAttributes = stringAttributes;
            this.toggleAttributes = toggleAttributes;
        }

        /// <summary>
        /// The float attributes to be used for the comparison.
        /// </summary>
        private readonly ICollection<string> floatAttributes;

        /// <summary>
        /// The integer attributes to be used for the comparison.
        /// </summary>
        private readonly ICollection<string> intAttributes;

        /// <summary>
        /// The string attributes to be used for the comparison.
        /// </summary>
        private readonly ICollection<string> stringAttributes;

        /// <summary>
        /// The toggle attributes to be used for the comparison.
        /// </summary>
        private readonly ICollection<string> toggleAttributes;

        /// <summary>
        /// True when there is a difference between <paramref name="left"/> and
        /// <paramref name="right"/> in terms of the the attributes set in the
        /// constructor.
        /// </summary>
        /// <param name="left">Left graph element to be compared.</param>
        /// <param name="right">Right graph element to be compared.</param>
        /// <returns>True if there is any difference.</returns>
        bool IGraphElementDiff.AreDifferent(GraphElement left, GraphElement right)
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
                return AttributesDiffer(left, right, floatAttributes, (GraphElement e, string a, out float v) => e.TryGetFloat(a, out v))
                   || AttributesDiffer(left, right, intAttributes, (GraphElement e, string a, out int v) => e.TryGetInt(a, out v))
                   || AttributesDiffer(left, right, stringAttributes, (GraphElement e, string a, out string v) => e.TryGetString(a, out v))
                   || AttributesDiffer<bool>(left, right, toggleAttributes, TryGetToggle);
            }
        }

        /// <summary>
        /// Returns true if <paramref name="element"/> has the toggle attribute <paramref name="attributeName"/>.
        /// </summary>
        /// <param name="element">Element to be queried.</param>
        /// <param name="attributeName">Name of the toggle attribute.</param>
        /// <param name="value">True if <paramref name="element"/> has the toggle attribute <paramref name="attributeName"/>.</param>
        /// <returns>True if <paramref name="element"/> has the toggle attribute <paramref name="attributeName"/>.</returns>
        private static bool TryGetToggle(GraphElement element, string attributeName, out bool value)
        {
            value = element.HasToggle(attributeName);
            return value;
        }

        /// <summary>
        /// Yields true if <paramref name="element"/> has an attribute <paramref name="attributeName"/> of type <typeparamref name="T"/>
        /// </summary>
        /// <typeparam name="T">data type of the attribute</typeparam>
        /// <param name="element">Element to be queried.</param>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <param name="value">Value of the attribute, if it exists; undefined otherwise.</param>
        /// <returns>True if <paramref name="element"/> has an attribute <paramref name="attributeName"/>.</returns>
        private delegate bool TryGetValue<T>(GraphElement element, string attributeName, out T value);

        /// <summary>
        /// Returns true if there is at least one attribute A in <paramref name="attributes"/>
        /// meeting any of the following conditions:
        /// (1) <paramref name="left"/> has A, but <paramref name="right"/> does not have it, or vice versa
        /// (2) <paramref name="left"/> and <paramref name="right"/> have A, but their values for A differ
        /// </summary>
        /// <typeparam name="T">the type of an attributes value</typeparam>
        /// <param name="left">Element to be compared to <paramref name="right"/>.</param>
        /// <param name="right">Element to be compared to <paramref name="left"/>.</param>
        /// <param name="attributes">The list of relevant attributes that are to be compared.</param>
        /// <param name="tryGetValue">A delegate to retrieve the value of an attribute; <seealso cref="TryGetValue{T}"/>.</param>
        /// <returns>True if the attributes of <paramref name="left"/> and <paramref name="right"/> differ
        /// as specified above.</returns>
        private static bool AttributesDiffer<T>(GraphElement left, GraphElement right, ICollection<string> attributes, TryGetValue<T> tryGetValue)
        {
            foreach (string attribute in attributes)
            {
                if (tryGetValue(left, attribute, out T leftValue))
                {
                    // left has attribute
                    if (tryGetValue(right, attribute, out T rightValue))
                    {
                        // and right has attribute; if they are different, we have found one difference
                        if (!leftValue.Equals(rightValue))
                        {
                            return true;
                        }
                        // the two attribute values are the same => we need to continue
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
                    if (tryGetValue(right, attribute, out T _))
                    {
                        return true;
                    }
                    // Neither of the two nodes have the attribute => we need to continue
                }
            }
            return false;
        }
    }
}