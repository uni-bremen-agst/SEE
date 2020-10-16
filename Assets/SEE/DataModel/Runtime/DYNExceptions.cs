using System;

namespace SEE.DataModel.Runtime
{

    /// <summary>
    /// Is thrown if not enough categories exist.
    /// </summary>
    public class NotEnoughCategoriesException : ArgumentException
    {
        public NotEnoughCategoriesException()
        {
        }

        public NotEnoughCategoriesException(string message)
            : base(message)
        {
        }

        public NotEnoughCategoriesException(string message, Exception inner)
            : base(message, inner)
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="categoryCount">The count of the categories.</param>
        public NotEnoughCategoriesException(int categoryCount)
            : base("Category count: " + categoryCount)
        {
        }
    }

    /// <summary>
    /// Is thrown if the attribute count is incorrect.
    /// </summary>
    public class IncorrectAttributeCountException : ArgumentOutOfRangeException
    {
        public IncorrectAttributeCountException()
        {
        }

        public IncorrectAttributeCountException(string message)
            : base(message)
        {
        }

        public IncorrectAttributeCountException(string message, Exception inner)
            : base(message, inner)
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="categoryCount">The count of the categories.</param>
        /// <param name="valueCount">The count of the attributes of the function call.</param>
        public IncorrectAttributeCountException(int categoryCount, int valueCount)
            : base("Category count: " + categoryCount + ", value count: " + valueCount)
        {
        }
    }

}
