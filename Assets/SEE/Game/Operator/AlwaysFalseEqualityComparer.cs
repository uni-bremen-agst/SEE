using System;
using System.Collections.Generic;
using Random = UnityEngine.Random;

namespace SEE.Game.Operator
{
    /// <summary>
    /// An equality comparer that always returns false.
    /// </summary>
    /// <typeparam name="T">The type of objects to compare.</typeparam>
    public class AlwaysFalseEqualityComparer<T>: EqualityComparer<T>
    {
        public override bool Equals(T x, T y)
        {
            return false;
        }

        public override int GetHashCode(T obj)
        {
            // We would like a different hash code for each object, but I'm not sure how to do that.
            return obj.GetHashCode();
        }
    }
}