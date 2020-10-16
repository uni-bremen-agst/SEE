using System;

namespace SEE.DataModel.DG
{
    /// <summary>
    /// An exception thrown in case a graph, node, or edge attribute is unknown.
    /// </summary>
    [Serializable]
    public class UnknownAttribute : Exception
    {
        public UnknownAttribute()
        {
        }

        public UnknownAttribute(string message)
            : base(message)
        {
        }

        public UnknownAttribute(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}