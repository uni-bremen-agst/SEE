using System;
using System.Collections.Generic;

namespace SEE.Utils
{
    /// <summary>
    /// Implements a union-find (disjoint-set) data structure for partitioning objects of
    /// type <typeparamref name="O"/> based on their associated values of type <typeparamref name="V"/>.
    /// All objects with the same value are grouped into the same partition.
    /// </summary>
    /// <typeparam name="O">type of objects to be partitioned</typeparam>
    /// <typeparam name="V">the value type used for partitioning</typeparam>
    public class UnionFind<O, V>
    {
        /// The core idea of a Union-Find data structure is to maintain a collection of disjoint sets,
        /// supporting two primary operations: Find, which determines the representative (or root) of
        /// the set that an element belongs to, and Union, which merges two sets. This implementation
        /// uses a dictionary to map each object to its parent and another dictionary to track the
        /// size of each set for the union by size optimization. This approach avoids using a
        /// fixed-size array, making it more flexible for generic object types.
        ///
        /// The provided implementation uses two key optimizations: path compression in the
        /// Find operation to flatten the tree structure, and union by size in the Union operation
        /// to keep the trees balanced. These optimizations improve the amortized time complexity
        /// of both operations to nearly constant time, O(α(n)), where α is the inverse Ackermann
        /// function, which grows very slowly.

        /// <summary>
        /// Maps each object to its parent in the union-find structure.
        /// </summary>
        private readonly Dictionary<O, O> parent;

        /// <summary>
        /// Tracks the size of each set for the union by size optimization.
        /// </summary>
        private readonly Dictionary<O, int> size;

        /// <summary>
        /// The delegate to obtain the value associated with an object.
        /// </summary>
        private readonly Func<O, V> getValue;

        /// <summary>
        /// Constructor to initialize the union-find structure with a collection of objects.
        /// </summary>
        /// <param name="objects">The objects to be partitioned.</param>
        /// <param name="valueSelector">The delegate to obtain the value associated with an object.</param>
        public UnionFind(IEnumerable<O> objects, Func<O, V> valueSelector)
        {
            parent = new Dictionary<O, O>();
            size = new Dictionary<O, int>();
            getValue = valueSelector;

            foreach (O obj in objects)
            {
                parent[obj] = obj;
                size[obj] = 1;
            }
        }

        /// <summary>
        /// Finds the representative element of the partition containing the specified object <paramref name="obj"/>.
        /// </summary>
        /// <remarks>This method uses path compression to optimize future queries, which may modify the
        /// internal structure of the set. The operation has an amortized time complexity of O(α(n)), where α is the
        /// inverse Ackermann function.</remarks>
        /// <param name="obj">The object whose set representative is to be found. Must be a member of the set.</param>
        /// <returns>The representative element of the set containing <paramref name="obj"/>.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="obj"/> is not a member of the set.</exception>
        public O Find(O obj)
        {
            if (!parent.ContainsKey(obj))
            {
                throw new ArgumentException("Object not in the set.", nameof(obj));
            }

            // Path compression
            if (!EqualityComparer<O>.Default.Equals(parent[obj], obj))
            {
                parent[obj] = Find(parent[obj]);
            }
            return parent[obj];
        }

        /// <summary>
        /// Unites the partitions containing <paramref name="obj1"/> and <paramref name="obj2"/>.
        /// </summary>
        /// <param name="obj1">First object.</param>
        /// <param name="obj2">Second object.</param>
        public void Union(O obj1, O obj2)
        {
            O root1 = Find(obj1);
            O root2 = Find(obj2);

            // If the objects already belong to the same set, do nothing.
            if (EqualityComparer<O>.Default.Equals(root1, root2))
            {
                return;
            }

            // Union by size.
            if (size[root1] < size[root2])
            {
                (root1, root2) = (root2, root1); // Swap to ensure root1 is the larger set.
            }

            parent[root2] = root1;
            size[root1] += size[root2];
        }

        /// <summary>
        /// Partitions objects into partitions based on their associated values and unions objects
        /// within the same partition.
        /// </summary>
        /// <remarks>This method iterates through all objects, groups them by their associated values, and
        /// performs a union operation on all objects within each group that contains more than one object. The
        /// grouping is determined by the value returned from the <c>getValue</c> function for each object.</remarks>
        public void PartitionByValue()
        {
            Dictionary<V, List<O>> valueGroups = new();

            foreach (O obj in parent.Keys)
            {
                V value = getValue(obj);
                if (!valueGroups.ContainsKey(value))
                {
                    valueGroups[value] = new List<O>();
                }
                valueGroups[value].Add(obj);
            }

            foreach (List<O> group in valueGroups.Values)
            {
                if (group.Count > 1)
                {
                    O firstObj = group[0];
                    for (int i = 1; i < group.Count; i++)
                    {
                        Union(firstObj, group[i]);
                    }
                }
            }
        }

        /// <summary>
        /// Returns the partitions as a collection of the representatives of each partition.
        /// </summary>
        /// <returns>Partitions.</returns>
        public ICollection<IList<O>> GetPartitions()
        {
            Dictionary<O, IList<O>> partitions = new();

            foreach (O obj in new List<O>(parent.Keys))
            {
                O root = Find(obj); // Find the root for the current object
                if (!partitions.ContainsKey(root))
                {
                    partitions[root] = new List<O>();
                }
                partitions[root].Add(obj);
            }

            return partitions.Values;
        }
    }
}
