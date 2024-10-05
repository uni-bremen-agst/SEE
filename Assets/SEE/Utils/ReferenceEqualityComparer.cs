using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace SEE.Utils
{
    // NOTE: The below class was copied from the .NET 5.x source code.
    //       Unity uses .NET 4.x, so this class would otherwise not be available.

    // Original license for this code:
    // The MIT License (MIT)
    // Copyright (c) .NET Foundation and Contributors
    //
    // All rights reserved.
    //
    // Permission is hereby granted, free of charge, to any person obtaining a copy
    // of this software and associated documentation files (the "Software"), to deal
    // in the Software without restriction, including without limitation the rights
    // to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    // copies of the Software, and to permit persons to whom the Software is
    // furnished to do so, subject to the following conditions:
    //
    // The above copyright notice and this permission notice shall be included in all
    // copies or substantial portions of the Software.
    //
    // THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    // IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    // FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    // AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    // LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    // OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    // SOFTWARE.

    /// <summary>
    /// An <see cref="IEqualityComparer{Object}"/> that uses reference equality (<see cref="object.ReferenceEquals(object, object)"/>)
    /// instead of value equality (<see cref="object.Equals(object)"/>) when comparing two object instances.
    /// </summary>
    /// <remarks>
    /// The <see cref="ReferenceEqualityComparer"/> type cannot be instantiated. Instead, use the <see cref="Instance"/> property
    /// to access the singleton instance of this type.
    /// </remarks>
    public sealed class ReferenceEqualityComparer : IEqualityComparer<object>
    {
        private ReferenceEqualityComparer() { }

        /// <summary>
        /// Gets the singleton <see cref="ReferenceEqualityComparer"/> instance.
        /// </summary>
        public static ReferenceEqualityComparer Instance { get; } = new();

        /// <summary>
        /// Determines whether two object references refer to the same object instance.
        /// </summary>
        /// <param name="x">The first object to compare.</param>
        /// <param name="y">The second object to compare.</param>
        /// <returns>
        /// <see langword="true"/> if both <paramref name="x"/> and <paramref name="y"/> refer to the same object instance
        /// or if both are <see langword="null"/>; otherwise, <see langword="false"/>.
        /// </returns>
        /// <remarks>
        /// This API is a wrapper around <see cref="object.ReferenceEquals(object?, object?)"/>.
        /// It is not necessarily equivalent to calling <see cref="object.Equals(object?, object?)"/>.
        /// </remarks>
        public new bool Equals(object x, object y) => ReferenceEquals(x, y);

        /// <summary>
        /// Returns a hash code for the specified object. The returned hash code is based on the object
        /// identity, not on the contents of the object.
        /// </summary>
        /// <param name="obj">The object for which to retrieve the hash code.</param>
        /// <returns>A hash code for the identity of <paramref name="obj"/>.</returns>
        /// <remarks>
        /// This API is a wrapper around <see cref="RuntimeHelpers.GetHashCode(object)"/>.
        /// It is not necessarily equivalent to calling <see cref="object.GetHashCode()"/>.
        /// </remarks>
        public int GetHashCode(object obj) => RuntimeHelpers.GetHashCode(obj);
    }
}
