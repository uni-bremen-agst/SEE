using System;
using System.Collections.Generic;
using System.Linq;

namespace SEE.Layout.NodeLayouts.EmptySpace
{
    /// <summary>
    /// Finds empty space in an outer object with nested other objects (obstacles)
    /// using a sweep-line algorithm.
    ///
    /// Event Points: Consider all X- and Y-coordinates defined by the boundaries of the
    /// main rectangle (outer) and all obstacle rectangles (inner). These coordinates define
    /// a grid.
    ///
    /// Sweep Line: Imagine a vertical line sweeping from Ymin​ to Ymax​.
    ///
    /// Active Intervals: At any point along the sweep, the line maintains a list of
    /// vertical free intervals (gaps) between the obstacles.
    ///
    /// Maximal Rectangles: When the sweep line passes an obstacle's top or bottom edge,
    /// the existing free intervals may be terminated. A maximal empty rectangle is reported
    /// when a free interval is terminated by an obstacle boundary, or by the boundary of the
    /// outer rectangle, as it's the largest possible rectangle that could be formed by that
    /// specific vertical gap.
    ///
    /// Implementing the full Sweep-Line Algorithm for the Maximal Empty Rectangle problem
    /// is substantially more complex than the recursive partitioning method, as it requires
    /// specialized data structures like a Segment Tree or a balanced Interval Tree to
    /// efficiently manage the active free intervals.
    ///
    /// Since the constraints of a simple code block make a complete, optimized
    /// implementation impractical, a simplified implementation based on the
    /// Sweep-Line principle is used here that uses a basic set of ordered
    /// Event Points to determine the maximal empty rectangles, without the
    /// complexity of a full Segment Tree. This approach is conceptually
    /// correct but less performant for huge datasets.
    ///
    /// The algorithm proceeds by considering all unique X and Y coordinates
    /// from the outer rectangle and the obstacles (inner rectangles). These coordinates
    /// form a grid. The maximal empty rectangles must lie on this grid.
    /// </summary>
    internal static class EmptySpaceFinder
    {
        /// <summary>
        /// Returns the set of maximally large empty rectangles for given <paramref name="outerRectangle"/>
        /// containing the <paramref name="innerRectangles"/>. None of the results will overlap with any
        /// <paramref name="innerRectangles"/>. All of them will be completely contained in <paramref name="outerRectangle"/>.
        /// Together they cover the complete empty space in <paramref name="outerRectangle"/>.
        /// The elements of the results may overlap each other.
        /// </summary>
        /// <param name="outerRectangle">the outer rectangle (must not be null)</param>
        /// <param name="innerRectangles">the nested inner rectangles (must not be null)</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">thrown if one of the arguments is null</exception>
        /// <exception cref="ArgumentException">thrown if any of the <paramref name="innerRectangles"/>
        /// is not fully contained in <paramref name="outerRectangle"/></exception>
        public static IList<Rectangle> Find(Rectangle outerRectangle, IEnumerable<Rectangle> innerRectangles)
        {
            if (outerRectangle == null)
            {
                throw new ArgumentNullException(nameof(outerRectangle));
            }
            if (innerRectangles == null)
            {
                throw new ArgumentNullException(nameof(innerRectangles));
            }

            List<Rectangle> innerList = innerRectangles.ToList();

            if (!AreAllNested(outerRectangle, innerList))
            {
                throw new ArgumentException($"All {nameof(innerRectangles)} must be fully contained within {nameof(outerRectangle)}");
            }

            // Collect all unique coordinates along the sweep-line direction that define strip boundaries.
            // These are the Top and Bottom edges of outerRectangle and all inner rectangles if the
            // sweep line moves along the Y axis or the Left and Right edges of outerRectangle if the
            // sweep line moves along the X axis, respectively.
            // These are the sweep-line "event points".
            HashSet<float> coords = new() { outerRectangle.Top, outerRectangle.Bottom };
            foreach (Rectangle obs in innerList)
            {
                coords.Add(obs.Top);
                coords.Add(obs.Bottom);
            }
            // Sort the event points by their co-ordinate (Y co-ordinate if sweep lines moves along the Y axis
            // or X co-ordinate if it moves along the X axis, respectively.
            List<float> sorted = coords.Where(c => c >= outerRectangle.Top && c <= outerRectangle.Bottom).OrderBy(c => c).ToList();

            // List to hold the maximal empty rectangles found. This will be the result.
            // That result must later be filtered to ensure only MAXIMAL rectangles are returned.
            List<Rectangle> maximalRects = new();

            // Sweep the plane between consecutive sorted co-ordinates (vertically if the sweep lines moves
            // along the Y axis or horizontally if it moves along the X axis).
            for (int i = 0; i < sorted.Count - 1; i++)
            {
                // [start, end] defines the current strip.
                float start = sorted[i];
                float end = sorted[i + 1];
                // Height of the current strip.
                float height = end - start;

                if (height <= 0)
                {
                    continue;
                }

                // 1. Identify active obstacles within this strip [start, end].
                // An obstacle is active if it intersects the strip.
                // Note: This is a rather expensive operation requiring O(n) where n is the number of obstacles.
                List<Rectangle> activeObstacles = innerList.Where(o => o.Top < end && o.Bottom > start).ToList();

                // 2. Define the initial free interval (the full width of outerRectangle).
                IList<Gap> currentGaps = new List<Gap> { new() { Begin = outerRectangle.Left,
                                                                 End = outerRectangle.Right } };

                // 3. Subtract the projections of active obstacles.
                // The projections are horizontal (X intervals) if the sweep lines moves
                // along the Y axis or vertical (Y intervals) if it moves along the X axis.
                foreach (Rectangle obstacle in activeObstacles)
                {
                    // FIXME: Because all inner rectangles are guaranteed to be within outerRectangle,
                    // obstacleBegin and obstacleEnd will always be co-ordinates of obstacle.
                    float obstacleBegin = Math.Max(outerRectangle.Left, obstacle.Left);
                    // Should always be obs.Left
                    float obstacleEnd = Math.Min(outerRectangle.Right, obstacle.Right);
                    // Should always be obs.Right

                    // If the obstacle is valid in the dimension orthogonal to the sweep-line
                    // direction (X-dimension if the sweep line moves along the Y axis or
                    // Y-dimension if it moves along the X axis, respectively).
                    if (obstacleBegin < obstacleEnd)
                    {
                        currentGaps = SubtractInterval(currentGaps, obstacleBegin, obstacleEnd);
                    }
                }

                // 4. Any remaining gap, combined with the current height, forms an empty rectangle.
                foreach (Gap gap in currentGaps)
                {
                    if (gap.Begin < gap.End)
                    {
                        maximalRects.Add(new Rectangle(gap.Begin, start, gap.End - gap.Begin, height));
                    }
                }
            }

            // Final step: Filter to ensure only MAXIMAL rectangles are returned.
            return PostProcessMaximalRectangles(maximalRects);
        }

        /// <summary>
        /// Yields true if all inner <paramref name="innerRectangles"/> are within the bounds
        /// of the <paramref name="outerRectangle"/>.
        /// </summary>
        /// <param name="outerRectangle">the outer rectangle</param>
        /// <param name="innerRectangles">list of nested rectangles</param>
        /// <returns>true if all inner rectangles are within the outer rectangle</returns>
        private static bool AreAllNested(Rectangle outerRectangle, List<Rectangle> innerRectangles)
        {
            return !innerRectangles.Where(r => !outerRectangle.Contains(r)).Any();
        }

        /// <summary>
        /// Returns a new list of gaps resulting from subtracting the interval [<paramref name="obsX1"/>, <paramref name="obsX2"/>].
        /// Will cut the existing gaps by removing the specified interval, potentially splitting gaps.
        /// </summary>
        /// <param name="gaps">the current gaps</param>
        /// <param name="obsX1">left X co-ordinate of the obstacle</param>
        /// <param name="obsX2">right X co-ordinate of the obstacle</param>
        /// <returns>gaps not containing the given interval</returns>
        private static IList<Gap> SubtractInterval(IList<Gap> gaps, float obsX1, float obsX2)
        {
            List<Gap> newGaps = new();
            foreach (Gap gap in gaps)
            {
                // Case 1: Obstacle is entirely outside the gap (no change)
                if (obsX2 <= gap.Begin || obsX1 >= gap.End)
                {
                    // Existing gap remains the same.
                    newGaps.Add(gap);
                }
                // Case 2: Obstacle covers the entire gap (gap is eliminated).
                // Covers also the case that obsX2 == gap.X1 && obsX1 == gap.X2.
                else if (obsX1 <= gap.Begin && gap.End <= obsX2)
                {
                    // Do nothing (gap is removed)
                }
                // Case 3: Obstacle cuts the beginning of the gap (creates one smaller gap)
                else if (obsX1 <= gap.Begin && obsX2 < gap.End)
                {
                    newGaps.Add(new Gap { Begin = obsX2, End = gap.End });
                }
                // Case 4: Obstacle cuts the end of the gap (creates one smaller gap)
                else if (gap.Begin < obsX1 && obsX2 >= gap.End)
                {
                    newGaps.Add(new Gap { Begin = gap.Begin, End = obsX1 });
                }
                // Case 5: Obstacle cuts the middle of the gap (creates two smaller gaps)
                else if (obsX1 > gap.Begin && obsX2 < gap.End)
                {
                    newGaps.Add(new Gap { Begin = gap.Begin, End = obsX1 });
                    newGaps.Add(new Gap { Begin = obsX2, End = gap.End });
                }
                else
                {
                    // This should not happen if all cases are covered.
                    throw new InvalidOperationException($"Unhandled case in {nameof(SubtractInterval)}.");
                }
            }
            return newGaps;
        }

        // Post-processing to filter out non-maximal rectangles (those fully contained in others).
        private static IList<Rectangle> PostProcessMaximalRectangles(IList<Rectangle> rects)
        {
            List<Rectangle> maximal = new();
            // Using Distinct is important due to how the sweep line can generate duplicates.
            List<Rectangle> distinctRects = rects.Distinct().ToList();

            // FIXME: This is an O(n^2) operation.
            foreach (Rectangle r1 in distinctRects)
            {
                bool isMaximal = true;
                // FIXME: A is compared to B and later B to A; can be optimized.
                // For instance, sort by area and only compare larger to smaller.
                foreach (Rectangle r2 in distinctRects)
                {
                    if (r1 == r2)
                    {
                        continue; // Skip self-comparison
                    }

                    // If r1 is entirely contained within r2
                    if (r2.Contains(r1))
                    {
                        isMaximal = false;
                        break;
                    }
                }
                if (isMaximal)
                {
                    maximal.Add(r1);
                }
            }
            return maximal;
        }
    }
}
