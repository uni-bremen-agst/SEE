using System.Collections.Generic;
using System.Linq;

namespace SEE.Utils
{
    /// <summary>
    /// A delegate that is called during a forrest traversal once for each
    /// visited tree node.
    ///
    /// If this method returns <c>true</c>, the traversal continues with the
    /// next node; otherwise the traversal terminates and none of the remaining
    /// nodes gets visited anymore.
    /// </summary>
    /// <typeparam name="T">the data type for the node data</typeparam>
    /// <param name="child">the currently visited node</param>
    /// <param name="parent">the parent of the currently visited node; may be null
    /// if the child is a root</param>
    /// <returns>if <c>true</c>, traversal continues; otherwise it terminates</returns>
    public delegate bool TreeVisitor<T>(T child, T parent);

    /// <summary>
    /// A representation of a tree with multiple roots, in other words, a
    /// forrest.
    /// </summary>
    /// <typeparam name="T">the data type for the node data</typeparam>
    public class Forrest<T>
    {
        /// <summary>
        /// A delegate to be called whenever an element in the forrest is visited.
        /// If it yields <c>true</c>, the treversal is continued, otherwise terminated.
        /// </summary>
        /// <param name="child">the currently visited node</param>
        /// <param name="parent">the parent of the currently visited node</param>
        /// <returns>whether the traversal should be continued</returns>
        private delegate bool NodeVisitor(Node child, Node parent);

        /// <summary>
        /// Represents a node in the forrest.
        /// </summary>
        private class Node
        {
            /// <summary>
            /// The user-specific data associated with this node.
            /// </summary>
            internal readonly T item;
            /// <summary>
            /// The parent of this node. May be null, in which case it is considered
            /// a root of the forrest.
            /// </summary>
            private Node parent;
            /// <summary>
            /// The immediate children of this node.
            /// </summary>
            private readonly ICollection<Node> children = new List<Node>();

            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="item">the user-specific data to be associated with this node</param>
            internal Node(T item)
            {
                this.item = item;
            }

            /// <summary>
            /// Adds <paramref name="child"/> as a child to this node.
            /// </summary>
            /// <param name="child">child to be added</param>
            internal void AddChild(T child)
            {
                Node childNode = new Node(child)
                {
                    parent = this
                };
                children.Add(childNode);
            }

            /// <summary>
            /// Traverses the forrest in preorder an calls <paramref name="visitor"/>
            /// for each visited node. If <paramref name="visitor"/> yields <c>true</c>,
            /// the traversal continues with the next node in the preorder not yet visited;
            /// otherwise the traversal terminates.
            /// </summary>
            /// <param name="visitor">visitor to be called for every visited node</param>
            /// <returns><c>true</c> if and only if the traversal is to be continued</returns>
            internal bool PreorderTraverse(NodeVisitor visitor)
            {
                return visitor(this, parent) && children.All(child => child.PreorderTraverse(visitor));
            }
        }

        /// <summary>
        /// The list of roots. Its order corresponds to the order
        /// in which <see cref="AddRoot(T)"/> was called. The earliest root
        /// added will be the first entry of this list.
        /// </summary>
        private readonly IList<Node> roots = new List<Node>();

        /// <summary>
        /// The total number of elements in this forrest (not just roots).
        /// </summary>
        public int Count
        {
            get
            {
                int result = 0;
                PreorderTraverse(Inc);
                return result;

                bool Inc(Node child, Node _)
                {
                    result++;
                    return true;
                }
            }
        }

        /// <summary>
        /// Adds <paramref name="root"/> as a root to this forrest.
        /// </summary>
        /// <param name="root">root to be added</param>
        public void AddRoot(T root)
        {
            roots.Add(new Node(root));
        }

        /// <summary>
        /// Returns the first <see cref="Node"/> having <paramref name="item"/>
        /// as associated data, i.e., with an item equal to <paramref name="item"/>.
        /// May be null if none was found. Traversal is in preorder.
        /// </summary>
        /// <param name="item">item to be searched</param>
        /// <returns>first node representing <paramref name="item"/> or <c>null</c></returns>
        private Node Find(T item)
        {
            Node result = null;
            PreorderTraverse(FindChild);
            return result;

            bool FindChild(Node current, Node _)
            {
                if (current.item.Equals(item))
                {
                    result = current;
                    return false;
                }
                return true;
            }
        }

        /// <summary>
        /// Adds <paramref name="child"/> as a child of <paramref name="parent"/> to
        /// this forrest. There may no other element in this forrest holding the
        /// same item as <paramref name="child"/>. The <paramref name="parent"/>
        /// must already exist in the forrest.
        /// </summary>
        /// <param name="child">child to be added</param>
        /// <param name="parent">parent of <paramref name="child"/></param>
        /// <exception cref="System.Exception">thrown if <paramref name="parent"/> does
        /// not exist in the forrest or if there is another element having the
        /// same item as <paramref name="child"/></exception>
        public void AddChild(T child, T parent)
        {
            if (Find(child) != null)
            {
                throw new System.Exception($"[{nameof(Forrest<T>)}] Child exists already");
            }
            Node parentNode = Find(parent);
            if (parentNode == null)
            {
                throw new System.Exception($"[{nameof(Forrest<T>)}] Parent not found");
            }
            parentNode.AddChild(child);
        }

        /// <summary>
        /// Traverses this forrest in preorder and calls <paramref name="visitor"/> for each
        /// visited element. The arguments passed to <paramref name="visitor"/> are the
        /// visited element and the parent of this visited element (as second parameter).
        /// If the visited element is a root, its parent argument will be <c>null</c>.
        ///
        /// If <paramref name="visitor"/> yields <c>false</c>, the traversal terminates;
        /// otherwise it continues with the next not yet visited node in preorder.
        /// </summary>
        /// <param name="visitor">the delegate to be called when a node is visited</param>
        public void PreorderTraverse(TreeVisitor<T> visitor)
        {
            PreorderTraverse(Visit);

            bool Visit(Node node, Node parent)
            {
                return visitor(node.item, parent != null ? parent.item : default);
            }
        }

        /// <summary>
        /// Traverses this forrest in preorder and calls <paramref name="visitor"/> for each
        /// visited element. The arguments passed to <paramref name="visitor"/> are the
        /// visited element and the parent of this visited element (as second parameter).
        /// If the visited element is a root, its parent argument will be <c>null</c>.
        ///
        /// If <paramref name="visitor"/> yields <c>false</c>, the traversal terminates;
        /// otherwise it continues with the next not yet visited node in preorder.
        /// </summary>
        /// <param name="visitor">the delegate to be called when a node is visited</param>
        private void PreorderTraverse(NodeVisitor visitor)
        {
            foreach (Node root in roots)
            {
                if (!root.PreorderTraverse(visitor))
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Returns all roots of this forrest as an ordered list. The order is the
        /// same as the roots were added via <see cref="AddRoot(T)"/>, that is,
        /// the first element in the result is the root that was added first.
        /// </summary>
        /// <returns>ordered list of roots</returns>
        /// <remarks>If you want all elements in the forrest, you can use <see cref="AllElements"/>
        /// instead. As an alternative, you can use <see cref="PreorderTraverse(TreeVisitor{T})"/>.
        /// </remarks>
        public IList<T> ToList()
        {
            return roots.Select(r => r.item).ToList();
        }

        /// <summary>
        /// Returns all elements in the forrest (not only the roots) in preorder.
        /// </summary>
        /// <returns>all elements in the forrest</returns>
        public IList<T> AllElements()
        {
            List<T> result = new();
            PreorderTraverse(AddToList);
            return result;

            bool AddToList(Node node, Node _)
            {
                result.Add(node.item);
                return true;
            }
        }
    }
}
