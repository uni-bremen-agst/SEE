using System.Collections.Generic;
using UnityEngine;

namespace SEE.Layout.NodeLayouts.TreeMap
{
    internal class RectangleTiling
    {
        /// <summary>
        /// Representation of a rectangle in the tree map. Must be a class (not a
        /// struct) because we need reference semantics when we iterate
        /// on the rectangles using ForEach below.
        /// </summary>
        public class Rectangle
        {
            public Rectangle(float x, float z, float width, float depth)
            {
                this.x = x;
                this.z = z;
                this.width = width;
                this.depth = depth;
            }
            public float x;      // x co-ordinate at corner
            public float z;      // z co-ordinate at corner
            public float width;  // width
            public float depth;  // depth (breadth)
        }

        /// <summary>
        /// The information about the size of a game node.
        /// </summary>
        public class NodeSize
        {
            public NodeSize(ILayoutNode gameNode, float size)
            {
                this.gameNode = gameNode;
                this.size = size;
            }
            public ILayoutNode gameNode;
            public float size;
        }

        /// <summary>
        /// Adds padding to the rectangle.
        /// </summary>
        /// <param name="rect">rectangle for which to add padding</param>
        /// <param name="padding">the absolute padding to be added in between 
        /// neighboring rectangles so that they can be distinguished</param>
        private static void Add_Padding(ref Rectangle rect, float padding)
        {
            if (rect.width > 2 * padding)
            {
                rect.x += padding;
                rect.width -= 2 * padding;
            }
            if (rect.depth > 2 * padding)
            {
                rect.z += padding;
                rect.depth -= 2 * padding;
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
        private static List<Rectangle> Layout_In_Z(List<NodeSize> sizes, float x, float z, float depth)
        {
            List<Rectangle> result = new List<Rectangle>();
            float width = Sum(sizes) / depth;
            foreach (NodeSize node in sizes)
            {
                result.Add(new Rectangle(x, z, width, node.size / width));
                z += node.size / width;
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
        private static List<Rectangle> Layout_In_X(List<NodeSize> sizes, float x, float z, float width)
        {
            List<Rectangle> result = new List<Rectangle>();
            float depth = Sum(sizes) / width;
            foreach (NodeSize node in sizes)
            {
                result.Add(new Rectangle(x, z, node.size / depth, depth));
                x += node.size / depth;
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
                result += node.size;
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
            return width >= depth ? Layout_In_Z(sizes, x, z, depth) : Layout_In_X(sizes, x, z, width);
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
        private static Rectangle Remaining_Rectangle_In_Row(List<NodeSize> sizes, float x, float z, float width, float depth)
        {
            float relative_width = Sum(sizes) / depth;
            return new Rectangle(x + relative_width, z, width - relative_width, depth);
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
        private static Rectangle Remaining_Rectangle_In_Column(List<NodeSize> sizes, float x, float z, float width, float depth)
        {
            float relative_width = Sum(sizes) / width;
            return new Rectangle(x, z + relative_width, width, depth - relative_width);
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
        private static Rectangle Remaining_Rectangle(List<NodeSize> sizes, float x, float z, float width, float depth)
        {
            return width >= depth ? Remaining_Rectangle_In_Row(sizes, x, z, width, depth)
                                  : Remaining_Rectangle_In_Column(sizes, x, z, width, depth);
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
        private static float Worst_Aspect_Ratio(List<NodeSize> sizes, float x, float z, float width, float depth)
        {
            float max = 0.0f;
            foreach (Rectangle rect in Layout(sizes, x, z, width, depth))
            {
                float ratio = Mathf.Max(rect.width / rect.depth, rect.depth / rect.width);
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
        private static List<Rectangle> Squarified_Layout(List<NodeSize> sizes, float x, float z, float width, float depth)
        {
            if (sizes.Count == 0)
            {
                return new List<Rectangle>();
            }
            NormalizeAndSort(sizes, width, depth);

            // The position at which to split.
            int i = 1;
            while (i < sizes.Count
                   && Worst_Aspect_Ratio(sizes.GetRange(0, i), x, z, width, depth)
                      >= Worst_Aspect_Ratio(sizes.GetRange(0, i + 1), x, z, width, depth))
            {
                i++;
            }

            List<NodeSize> current = sizes.GetRange(0, i); // sizes from the beginning through i-1
            List<NodeSize> remaining = sizes.GetRange(i, sizes.Count - current.Count); // sizes i through the rest of the list
            Rectangle rect = Remaining_Rectangle(current, x, z, width, depth);
            List<Rectangle> result = Layout(current, x, z, width, depth);
            result.AddRange(Squarified_Layout(remaining, rect.x, rect.z, rect.width, rect.depth));
            return result;
        }

        private static void NormalizeAndSort(List<NodeSize> sizes, float width, float depth)
        {
            Normalize(sizes, width, depth);
            sizes.Sort(delegate (NodeSize x, NodeSize y) { return y.size.CompareTo(x.size); });
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
        internal static List<Rectangle> Squarified_Layout_With_Padding(List<NodeSize> sizes, float x, float z, float width, float depth, float padding)
        {
            List<Rectangle> result = Squarified_Layout(sizes, x, z, width, depth);
            result.ForEach(rect => Add_Padding(ref rect, padding));
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
            float total_size = Sum(sizes);
            float total_area = width * depth;
            foreach (NodeSize node in sizes)
            {
                node.size *= total_area / total_size;
            }
        }
    }
}
