using System;
using System.Collections.Generic;
using Random = UnityEngine.Random;

namespace SEE.Game.Operator
{
    /// <summary>
    /// An equality comparer whose `Equals` method always returns false.
    /// The `GetHashCode` method returns the hash code of the object being compared.
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
            // NOTE: The hash code returned here may not be consistent with the Equals method.
            return obj.GetHashCode();
        }
    }
}
