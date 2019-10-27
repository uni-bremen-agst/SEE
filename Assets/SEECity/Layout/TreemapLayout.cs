using System;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Layout
{
    /// <summary>
    /// Yields a squarified treemap node layout according to the algorithm 
    /// described by Bruls, Huizing, van Wijk, "Squarified Treemaps".
    /// pp. 33-42, Eurographics / IEEE VGTC Symposium on Visualization, 2000.
    /// </summary>
    public class TreemapLayout : NodeLayout
    {
        public TreemapLayout(float groundLevel,
                             BlockFactory blockFactory,
                             float width,
                             float depth)
        : base(groundLevel, blockFactory)
        {
            name = "Treemap";
            this.width = width;
            this.depth = depth;
        }

        /// <summary>
        /// The width of the rectangle in which to place all nodes.
        /// </summary>
        private readonly float width;

        /// <summary>
        /// The depth of the rectangle in which to place all nodes.
        /// </summary>
        private readonly float depth;

        /// <summary>
        /// Representation of a rectangle in the tree map. Must be a class (not a
        /// struct) because we need reference semantics when we iterate
        /// on the rectangles using ForEach below.
        /// </summary>
        private class Rectangle
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
        /// The absolute padding to be added in between neighboring rectangles
        /// so that they can be distinguished.
        /// </summary>
        private const float padding = 0.1f;

        /// <summary>
        /// Adds padding to the rectangle.
        /// </summary>
        /// <param name="rect">rectangle for which to add padding</param>
        private static void pad_rectangle(ref Rectangle rect)
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
        /// <param name="sizes"></param>
        /// <param name="x"></param>
        /// <param name="z"></param>
        /// <param name="width"></param>
        /// <param name="depth"></param>
        /// <returns></returns>
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
            if (sizes.Count == 1)
            {
                return Layout(sizes, x, z, width, depth);
            }
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
        private static List<Rectangle> Squarified_Layout_With_Padding(List<NodeSize> sizes, float x, float z, float width, float depth)
        {
            List<Rectangle> result = Squarified_Layout(sizes, x, z, width, depth);
            result.ForEach(rect => pad_rectangle(ref rect));
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

        // Just for testing. FIXME: Remove it later.
        public void Draw()
        {
            List<float> sizes = new List<float>() { 1, 3, 6, 2, 2, 6, 4 };
            List<NodeSize> nodes = new List<NodeSize>();
            foreach (float size in sizes)
            {
                GameObject o = blockFactory.NewBlock();
                blockFactory.ScaleBlock(o, new Vector3(size, 1.0f, size));
                nodes.Add(new NodeSize(o, size));
            }
            Normalize(nodes, this.width, this.depth);
            nodes.Sort(delegate (NodeSize x, NodeSize y) { return y.size.CompareTo(x.size); });
            List<Rectangle> rects = Squarified_Layout_With_Padding(nodes, 0, 0, this.width, this.depth);
            Show(nodes, rects);
        }

        // Just for testing. FIXME: Remove it later.
        private void Show(List<NodeSize> nodes, List<Rectangle> rects)
        {
            int i = 0;
            foreach (Rectangle rect in rects)
            {
                GameObject o = nodes[i].gameNode;
                blockFactory.ScaleBlock(o, new Vector3(rect.width / blockFactory.Unit(), 1.0f, rect.depth / blockFactory.Unit()));
                blockFactory.SetGroundPosition(o, new Vector3(rect.x + rect.width / 2.0f, 0.0f, rect.z + rect.depth / 2.0f));
                i++;
            }
        }

        /// <summary>
        /// The information about the size of a game node.
        /// </summary>
        private class NodeSize
        {
            public NodeSize(GameObject gameNode, float size)
            {
                this.gameNode = gameNode;
                this.size = size;
            }
            public GameObject gameNode;
            public float size;
        }

        public override Dictionary<GameObject, NodeTransform> Layout(ICollection<GameObject> gameNodes)
        {
            List<NodeSize> sizes = GetSizes(gameNodes);
            Normalize(sizes, this.width, this.depth);
            sizes.Sort(delegate (NodeSize x, NodeSize y) { return y.size.CompareTo(x.size); });
            List<Rectangle> rects = Squarified_Layout_With_Padding(sizes, 0, 0, this.width, this.depth);
            return To_Transforms(sizes, rects);
        }

        /// <summary>
        /// Returns the transforms (position, scale) of the game objects (nodes) according to their
        /// corresponding rectangle in rects. 
        /// 
        /// Precondition: For every i in the range of nodes: rects[i] is the transform
        /// corresponding to nodes[i].
        /// </summary>
        /// <param name="nodes">the game nodes</param>
        /// <param name="rects">their corresponding rectangle</param>
        /// <returns></returns>
        private Dictionary<GameObject, NodeTransform> To_Transforms(List<NodeSize> nodes, List<Rectangle> rects)
        {
            Dictionary<GameObject, NodeTransform> result = new Dictionary<GameObject, NodeTransform>();
            int i = 0;
            foreach (Rectangle rect in rects)
            {
                GameObject o = nodes[i].gameNode;
                Vector3 position = new Vector3(rect.x + rect.width / 2.0f, groundLevel, rect.z + rect.depth / 2.0f);
                Vector3 scale = new Vector3(rect.width / blockFactory.Unit(), 1.0f, rect.depth / blockFactory.Unit());
                result[o] = new NodeTransform(position, scale);
                i++;
            }
            return result;
        }

        /// <summary>
        /// Returns the list of node sizes; one for each game node in gameNodes. The
        /// size of a game node is the maximum of its width and depth.
        /// </summary>
        /// <param name="gameNodes">list of game nodes whose sizes are to be determined</param>
        /// <returns>list of maximal lengths</returns>
        private List<NodeSize> GetSizes(ICollection<GameObject> gameNodes)
        {
            List<NodeSize> result = new List<NodeSize>();
            foreach (GameObject gameNode in gameNodes)
            {
                Vector3 size = blockFactory.GetSize(gameNode);
                // x and z lenghts may differ; we need to consider the larger value
                result.Add(new NodeSize(gameNode, Mathf.Max(size.x, size.z)));
            }
            return result;
        }
    }
}
