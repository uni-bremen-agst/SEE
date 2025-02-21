using System.Collections.Generic;
using UnityEngine;

namespace SEE.Layout.NodeLayouts.TreeMap
{
    /// <summary>
    /// Implements the tiling of rectangles for a tree map layout.
    /// </summary>
    internal class RectangleTiling
    {
        /// <summary>
        /// Representation of a rectangle in the tree map. Must be a class (not a
        /// struct) because we need reference semantics when we iterate
        /// on the rectangles using ForEach below.
        /// </summary>
        public class Rectangle
        {
            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="x">X co-ordinate at corner</param>
            /// <param name="z">Z co-ordinate at corner</param>
            /// <param name="width">width of the rectangle</param>
            /// <param name="depth">depth (breadth) of the rectangle</param>
            public Rectangle(float x, float z, float width, float depth)
            {
                X = x;
                Z = z;
                Width = width;
                Depth = depth;
            }
            /// <summary>
            /// X co-ordinate at corner.
            /// </summary>
            public float X;
            /// <summary>
            /// Z co-ordinate at corner.
            /// </summary>
            public float Z;
            /// <summary>
            /// Width of the rectangle.
            /// </summary>
            public float Width;
            /// <summary>
            /// Depth (breadth) of the rectangle.
            /// </summary>
            public float Depth;
        }

        /// <summary>
        /// The information about the size of a game node.
        /// </summary>
        public class NodeSize
        {
            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="gameNode">layout node this node size corresponds to</param>
            /// <param name="size">size of the node</param>
            public NodeSize(ILayoutNode gameNode, float size)
            {
                GameNode = gameNode;
                Size = size;
            }
            /// <summary>
            /// The layout node this node size corresponds to.
            /// </summary>
            public ILayoutNode GameNode;
            /// <summary>
            /// The size of the node.
            /// </summary>
            public float Size;
        }

        /// <summary>
        /// Adds padding to the rectangle to all sides, that is, the width
        /// and depth are increased by twice the <paramref name="padding"/>.
        /// </summary>
        /// <param name="rect">rectangle for which to add padding</param>
        /// <param name="padding">the absolute padding to be added in between
        /// neighboring rectangles so that they can be distinguished</param>
        private static void AddPadding(ref Rectangle rect, float padding)
        {
            if (rect.Width > 2 * padding)
            {
                rect.X += padding;
                rect.Width -= 2 * padding;
            }
            if (rect.Depth > 2 * padding)
            {
                rect.Z += padding;
                rect.Depth -= 2 * padding;
            }
        }

        /// <summary>
        /// Returns rectangles, one for each size in sizes along the z axis.
        /// The rectangles will fill up given depth. Their width will be determined by
        /// their area sizes.
        /// </summary>
        /// <param name="sizes">list of area sizes of siblings</param>
        /// <param name="x">x co-ordinate at which to start layouting</param>
        /// <param name="z">y co-ordinate at which to start layouting</param>
        /// <param name="depth">the available depth into which to squeeze the rectangles</param>
        /// <returns>rectangles filling depth with area proportional to sizes</returns>
        private static List<Rectangle> LayoutInZ(List<NodeSize> sizes, float x, float z, float depth)
        {
            List<Rectangle> result = new();
            float width = Sum(sizes) / depth;
            foreach (NodeSize node in sizes)
            {
                result.Add(new Rectangle(x, z, width, node.Size / width));
                z += node.Size / width;
            }
            return result;
        }

        /// <summary>
        /// Returns rectangles, one for each size in sizes along the z axis.
        /// The rectangles will fill up given width. Their depth will be determined by
        /// their area sizes.
        /// </summary>
        /// <param name="sizes">list of area sizes of siblings</param>
        /// <param name="x">x co-ordinate at which to start layouting</param>
        /// <param name="z">y co-ordinate at which to start layouting</param>
        /// <param name="width">the available width into which to squeeze the rectangles</param>
        /// <returns>rectangles filling width with area proportional to sizes</returns>
        private static List<Rectangle> LayoutInX(List<NodeSize> sizes, float x, float z, float width)
        {
            List<Rectangle> result = new();
            float depth = Sum(sizes) / width;
            foreach (NodeSize node in sizes)
            {
                result.Add(new Rectangle(x, z, node.Size / depth, depth));
                x += node.Size / depth;
            }
            return result;
        }

        /// <summary>
        /// Returns the sum of all sizes.
        /// </summary>
        /// <param name="sizes">list of sizes to be summed up</param>
        /// <returns>sum of all sizes</returns>
        private static float Sum(List<NodeSize> sizes)
        {
            float result = 0.0f;

            foreach (NodeSize node in sizes)
            {
                result += node.Size;
            }
            return result;
        }

        /// <summary>
        /// Returns rectangles, one for each size in sizes along the either the z or x axis
        /// depending upon width >= depth.
        /// The rectangles will fill up the given space (depth if width >= depth, otherwise
        /// width). Their other length (width if width >= depth, otherwise depth) will be determined by
        /// their area sizes.
        /// </summary>
        /// <param name="sizes">list of area sizes of siblings</param>
        /// <param name="x">x co-ordinate at which to start layouting</param>
        /// <param name="z">y co-ordinate at which to start layouting</param>
        /// <param name="width">the available width into which to squeeze the rectangles</param>
        /// <param name="depth">the available depth into which to squeeze the rectangles</param>
        /// <returns>rectangles filling width with area proportional to sizes</returns>
        private static List<Rectangle> Layout(List<NodeSize> sizes, float x, float z, float width, float depth)
        {
            return width >= depth ? LayoutInZ(sizes, x, z, depth) : LayoutInX(sizes, x, z, width);
        }

        /// <summary>
        /// Returns the remaining rectangle in a row (for the case width >= depth).
        /// </summary>
        /// <param name="sizes">list of area sizes of siblings</param>
        /// <param name="x">x co-ordinate at which to start layouting</param>
        /// <param name="z">y co-ordinate at which to start layouting</param>
        /// <param name="width">the available width into which to squeeze the rectangles</param>
        /// <param name="depth">the available depth into which to squeeze the rectangles</param>
        /// <returns>remaining rectangle</returns>
        private static Rectangle RemainingRectangleInRow(List<NodeSize> sizes, float x, float z, float width, float depth)
        {
            float relativeWidth = Sum(sizes) / depth;
            return new Rectangle(x + relativeWidth, z, width - relativeWidth, depth);
        }

        /// <summary>
        /// Returns the remaining rectangle in a column (for the case width < depth).
        /// </summary>
        /// <param name="sizes">list of area sizes of siblings</param>
        /// <param name="x">x co-ordinate at which to start layouting</param>
        /// <param name="z">y co-ordinate at which to start layouting</param>
        /// <param name="width">the available width into which to squeeze the rectangles</param>
        /// <param name="depth">the available depth into which to squeeze the rectangles</param>
        /// <returns>remaining rectangle</returns>
        private static Rectangle RemainingRectangleInColumn(List<NodeSize> sizes, float x, float z, float width, float depth)
        {
            float relativeWidth = Sum(sizes) / width;
            return new Rectangle(x, z + relativeWidth, width, depth - relativeWidth);
        }

        /// <summary>
        /// Returns the remaining rectangle in a row or column, depending upon whether width >= depth.
        /// </summary>
        /// <param name="sizes">list of area sizes of siblings</param>
        /// <param name="x">x co-ordinate at which to start layouting</param>
        /// <param name="z">y co-ordinate at which to start layouting</param>
        /// <param name="width">the available width into which to squeeze the rectangles</param>
        /// <param name="depth">the available depth into which to squeeze the rectangles</param>
        /// <returns>remaining rectangle</returns>
        private static Rectangle RemainingRectangle(List<NodeSize> sizes, float x, float z, float width, float depth)
        {
            return width >= depth ? RemainingRectangleInRow(sizes, x, z, width, depth)
                                  : RemainingRectangleInColumn(sizes, x, z, width, depth);
        }

        /// <summary>
        /// Returns the worst aspect ratio of the layout for the given sizes.
        ///
        /// Note: This function implements function 'worst' described in the paper by Bruls et al.
        ///
        /// </summary>
        /// <param name="sizes">area size of the rectangles to compute the treemap for</param>
        /// <param name="x">x co-ordinate of the "origin"</param>
        /// <param name="z">z co-ordinate of the "origin"</param>
        /// <param name="width">full width of the treemap</param>
        /// <param name="depth">full depth of the treemap</param>
        /// <returns>worst aspect ratio of the layout for the given sizes</returns>
        private static float WorstAspectRatio(List<NodeSize> sizes, float x, float z, float width, float depth)
        {
            float max = 0.0f;
            foreach (Rectangle rect in Layout(sizes, x, z, width, depth))
            {
                float ratio = Mathf.Max(rect.Width / rect.Depth, rect.Depth / rect.Width);
                if (ratio > max)
                {
                    max = ratio;
                }
            }
            return max;
        }

        /// <summary>
        /// Yields a squarified treemap for rectangles that are "padded" to allow for a visible border.
        /// The rectangles are kept in the rectangle defined by given width and depth.
        ///
        /// Preconditions:
        ///  1) Every size in `sizes` must be positive.
        ///  2) Every size is normalized such that width * depth = Sum(sizes).
        ///  3) 'sizes' must be sorted in descending order.
        /// </summary>
        /// <param name="sizes">area size of the rectangles to compute the treemap for</param>
        /// <param name="x">x co-ordinate of the "origin"</param>
        /// <param name="z">z co-ordinate of the "origin"</param>
        /// <param name="width">full width of the treemap</param>
        /// <param name="depth">full depth of the treemap</param>
        /// <returns>list of rectangles in the treemap; the order corresponds to the input order of the sizes.</returns>
        private static List<Rectangle> SquarifiedLayout(List<NodeSize> sizes, float x, float z, float width, float depth)
        {
            if (sizes.Count == 0)
            {
                return new List<Rectangle>();
            }
            NormalizeAndSort(sizes, width, depth);

            // The position at which to split.
            int i = 1;
            while (i < sizes.Count
                   && WorstAspectRatio(sizes.GetRange(0, i), x, z, width, depth)
                      >= WorstAspectRatio(sizes.GetRange(0, i + 1), x, z, width, depth))
            {
                i++;
            }

            List<NodeSize> current = sizes.GetRange(0, i); // sizes from the beginning through i-1
            List<NodeSize> remaining = sizes.GetRange(i, sizes.Count - current.Count); // sizes i through the rest of the list
            Rectangle rect = RemainingRectangle(current, x, z, width, depth);
            List<Rectangle> result = Layout(current, x, z, width, depth);
            result.AddRange(SquarifiedLayout(remaining, rect.X, rect.Z, rect.Width, rect.Depth));
            return result;
        }

        private static void NormalizeAndSort(List<NodeSize> sizes, float width, float depth)
        {
            Normalize(sizes, width, depth);
            sizes.Sort(delegate (NodeSize x, NodeSize y) { return y.Size.CompareTo(x.Size); });
        }

        /// <summary>
        /// Yields a squarified treemap for rectangles that are "padded" to allow for a visible border.
        /// The rectangles are kept in the rectangle defined by given width and depth.
        ///
        /// Preconditions:
        ///  1) Every size in `sizes` must be positive.
        ///  2) Every size is normalized such that width * depth = Sum(sizes).
        ///  3) 'sizes' must be sorted in descending order.
        /// </summary>
        /// <param name="sizes">area size of the rectangles to compute the treemap for</param>
        /// <param name="x">x co-ordinate of the "origin"</param>
        /// <param name="z">z co-ordinate of the "origin"</param>
        /// <param name="width">full width of the treemap</param>
        /// <param name="depth">full depth of the treemap</param>
        /// <param name="padding">the absolute padding to be added in between
        /// neighboring rectangles so that they can be distinguished</param>
        /// <returns>list of rectangles in the treemap; the order corresponds to the input order of the sizes.</returns>
        internal static List<Rectangle> SquarifiedLayoutWithPadding(List<NodeSize> sizes, float x, float z, float width, float depth, float padding)
        {
            List<Rectangle> result = SquarifiedLayout(sizes, x, z, width, depth);
            result.ForEach(rect => AddPadding(ref rect, padding));
            return result;
        }

        /// <summary>
        /// Normalizes given sizes so that Sum(sizes) = width * depth.
        /// </summary>
        /// <param name="sizes">Input list of numeric values to normalize.</param>
        /// <param name="width">x dimension of the rectangle to be normalized</param>
        /// <param name="depth">z dimension of the rectangle to be normalized</param>

        private static void Normalize(List<NodeSize> sizes, float width, float depth)
        {
            float totalSize = Sum(sizes);
            float totalArea = width * depth;
            foreach (NodeSize node in sizes)
            {
                node.Size *= totalArea / totalSize;
            }
        }
    }
}
