using System;
using System.Collections.Generic;
using System.Linq;
using static SEE.Layout.NodeLayouts.IncrementalTreeMap.Direction;

namespace SEE.Layout.NodeLayouts.IncrementalTreeMap
{
    /// <summary>
    /// Provides helper functions for the incremental tree map layout.
    /// </summary>
    internal static class Utils
    {
        /// <summary>
        /// Returns the item of the given collection that maximizes the given function.
        /// </summary>
        /// <param name="collection">The collection whose maximum with respect to
        /// <paramref name="eval"/> shall be returned.</param>
        /// <param name="eval">The function to be maximized.</param>
        /// <returns>Item of <paramref name="collection"/> that maximizes <paramref name="eval"/>.</returns>
        public static T ArgMax<T>(ICollection<T> collection, Func<T, IComparable> eval)
        {
            IComparable bestVal = collection.Max(eval);
            return collection.First(x => eval(x).CompareTo(bestVal) == 0);
        }

        /// <summary>
        /// Returns the item of the given collection that minimizes the given function.
        /// </summary>
        /// <param name="collection">The collection whose minimum with respect to
        /// <paramref name="eval"/> shall be returned.</param>
        /// <param name="eval">The function to be minimized.</param>
        /// <returns>Item of <paramref name="collection"/> that minimizes <paramref name="eval"/>.</returns>
        public static T ArgMin<T>(ICollection<T> collection, Func<T, IComparable> eval)
        {
            IComparable bestVal = collection.Min(eval);
            return collection.First(x => eval(x).CompareTo(bestVal) == 0);
        }

        /// <summary>
        /// Returns a new rectangle that includes all rectangles of <paramref name="nodes"/>.
        /// </summary>
        /// <param name="nodes">Nodes with set rectangles.</param>
        /// <returns>New parent rectangle.</returns>
        public static Rectangle CreateParentRectangle(IList<Node> nodes)
        {
            double x = nodes.Min(node => node.Rectangle.X);
            double z = nodes.Min(node => node.Rectangle.Z);
            double width = nodes.Max(node => node.Rectangle.X + node.Rectangle.Width) - x;
            double depth = nodes.Max(node => node.Rectangle.Z + node.Rectangle.Depth) - z;
            return new Rectangle(x, z, width, depth);
        }

        /// <summary>
        /// A list of <paramref name="nodes"/> with rectangles that are laid out
        /// in an <paramref name="oldRectangle"/> will be transformed (linearly), so that they fit
        /// in <paramref name="newRectangle"/>.
        /// </summary>
        /// <param name="nodes">Nodes with rectangles that should be transformed.</param>
        /// <param name="newRectangle">New parent rectangle.</param>
        /// <param name="oldRectangle">Old parent rectangle.</param>
        public static void TransformRectangles(IList<Node> nodes, Rectangle newRectangle, Rectangle oldRectangle)
        {
            double scaleX = newRectangle.Width / oldRectangle.Width;
            double scaleZ = newRectangle.Depth / oldRectangle.Depth;

            foreach (Node node in nodes)
            {
                node.Rectangle.X = (node.Rectangle.X - oldRectangle.X) * scaleX + newRectangle.X;
                node.Rectangle.Z = (node.Rectangle.Z - oldRectangle.Z) * scaleZ + newRectangle.Z;
                node.Rectangle.Width *= scaleX;
                node.Rectangle.Depth *= scaleZ;
            }
        }

        /// <summary>
        /// Clones a list of nodes and preserves the layout including segments and rectangles.
        /// Therefore the <see cref="Segment"/>s and <see cref="Rectangle"/>s of the nodes are cloned to.
        /// </summary>
        /// <param name="nodes">Siblings to be cloned.</param>
        /// <returns>Dictionary that maps ID to new clone node.</returns>
        public static IDictionary<string, Node> CloneGraph(IList<Node> nodes)
        {
            IDictionary<string, Node> clonesMap = nodes.ToDictionary(node => node.ID,
                node => new Node(node.ID)
                {
                    Rectangle = node.Rectangle.Clone(), DesiredSize = node.DesiredSize
                }
            );
            CloneSegments(from : nodes,to : clonesMap);
            return clonesMap;
        }

        /// <summary>
        /// Clones all <see cref="Segment"/>s of <paramref name="from"/>
        /// and registers the segment clones to the nodes of <paramref name="to"/>.
        /// The nodes of <paramref name="from"/> and <paramref name="to"/> need to have the same IDs.
        /// </summary>
        /// <param name="from">Nodes with segment structure that should be copied.</param>
        /// <param name="to">Dictionary that maps ID to nodes that should get the segment structure.</param>
        public static void CloneSegments(IEnumerable<Node> from, IDictionary<string, Node> to)
        {
            IEnumerable<Segment> segments = from.SelectMany(n => n.SegmentsDictionary().Values).Distinct();
            foreach (Segment segment in segments)
            {
                Segment segmentClone = new Segment(segment.IsConst, segment.IsVertical);
                // Segment.SidesXNodes must be copied (.ToList()) because else in the case 'from == to'
                // it would change the list while iterating over it.
                foreach (Node node in segment.Side1Nodes.ToList())
                {
                    to[node.ID].RegisterSegment(segmentClone,
                        segmentClone.IsVertical ? Right : Upper);
                }

                foreach (Node node in segment.Side2Nodes.ToList())
                {
                    to[node.ID].RegisterSegment(segmentClone,
                        segmentClone.IsVertical ? Left : Lower);
                }
            }
        }
    }
}
