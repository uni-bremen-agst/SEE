using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SEE.Layout.NodeLayouts.CirclePacking
{
    /// <summary>
    /// This class holds list of <see cref="Circle"/> objects and packs them closely.
    /// </summary>
    public class IncrementalCirclePacker
    {
        /// <summary>
        /// Packs the supplied circles into a tightly fitting circular container.
        /// Adjusts each circle's position so circles do not overlap and computes the
        /// container radius required to enclose them.
        /// </summary>
        /// <param name="circles">
        /// The list of circles to pack. The method will modify the circle instances
        /// (their positions and placement state). Must not be null.
        /// </param>
        /// <param name="containerCenter">
        /// The center point of the container used as the origin for packing.
        /// </param>
        /// <param name="containerRadius">
        /// The computed radius of the enclosing container. If this parameter is passed
        /// by reference (ref/out) the method will set it to the computed value;
        /// otherwise it is used as an initial hint and may be updated by the method.
        /// </param>
        /// <param name="newNodeIDsSizes">
        /// A mapping of new node IDs to their sizes (radii). Used when the layout must
        /// account for newly added nodes. May be null or empty if there are no new nodes.
        /// </param>
        /// <exception cref="System.ArgumentNullException">
        /// Thrown if <paramref name="circles"/> is null.
        /// </exception>
        /// <remarks>
        /// This routine implements an incremental packing strategy: it places circles
        /// one-by-one (or updates existing placements), attempts to minimize overlap
        /// and compaction, and updates circle centers accordingly. After placement
        /// it determines the minimal enclosing circle radius and writes it to
        /// <paramref name="containerRadius"/> (when used as an out/ref). The method
        /// mutates the provided circle objects and does not create a new collection.
        /// Callers should ensure any external references to circles remain valid and
        /// should validate the returned container radius if they rely on container bounds.
        /// </remarks>
        internal static void PackCircles(List<TheCircle> circles, Vector2 containerCenter, out float containerRadius, Dictionary<string, List<(string, float, Vector2)>> lastPositionsParam, string parentID)
        {
            IncrementalCirclePacker packer = new IncrementalCirclePacker();

            packer.PerformHistory(circles, parentID, containerCenter, lastPositionsParam);

            float maxCircleDiame = 0f;
            foreach (TheCircle circle in circles)
            {
                if (circle.Radius * 2f > maxCircleDiame)
                {
                    maxCircleDiame = circle.Radius * 2f;
                }
            }
            IncrementalCirclePackerExtended packerExtended = new IncrementalCirclePackerExtended(maxCircleDiame);
            packerExtended.PbdIterations = 10;
            packerExtended.ComputePacking(100, circles);
            TheCircle surroundingCircle = packer.ComputeSurroundingCircle(circles);
            if (surroundingCircle != null)
            {
                Vector2 offset = surroundingCircle.Center;
                foreach (TheCircle c in circles)
                {
                    c.Center -= offset;
                }
                surroundingCircle.Center = Vector2.zero;
            }
            containerRadius = surroundingCircle != null ? surroundingCircle.Radius : 0f; 
        }

        /// <summary>
        /// Restores and uses historic placement data (when available) to guide packing,
        /// then packs circles and updates the history cache.
        /// </summary>
        /// <param name="circles">
        /// The collection of circles to position. The method mutates each circle's
        /// Center, Radius and IsPlaced flags as it restores or re-packs them.
        /// </param>
        /// <param name="parent">The parent identifier used as the key into the
        /// <paramref name="lastPositions"/> history dictionary.
        /// </param>
        /// <param name="containerCenter">
        /// The center of the container used as the origin for packing and placement.
        /// </param>
        /// <param name="lastPositions">
        /// A dictionary mapping parent IDs to lists of tuples (node ID, radius, center).
        /// If an entry exists for <paramref name="parent"/>, the method attempts to
        /// restore those positions and radii before re-packing; otherwise it computes
        /// a fresh packing and records it here.
        /// </param>
        /// <remarks>
        /// If historic positions exist for <paramref name="parent"/>, this method
        /// restores matching circles from that snapshot, adjusts radii
        /// (calling <see cref="ExpandFromCircleA"/> as needed), and then packs
        /// any not-yet-placed circles using the packer's incremental placement
        /// routine. After placement the method writes an updated snapshot
        /// (ID, radius, center) back into <paramref name="lastPositions"/>
        /// for the given parent key. If no history exists, the method performs
        /// a full packing and stores the resulting snapshot.
        /// </remarks>
        private void PerformHistory(List<TheCircle> circles, string parent, Vector2 containerCenter, Dictionary<string, List<(string, float, Vector2)>> lastPositions)
        {
            List<(string, float)> newNodeIDsSizes = new List<(string, float)>();
            List<(string, float, Vector2)> bufferLastPos = lastPositions.FirstOrDefault(p => p.Key == parent).Value;
            if (bufferLastPos != default)
            {
                List<TheCircle> dealingCircles = new List<TheCircle>();
                foreach (TheCircle c in circles)
                {
                    (string, float, Vector2) tupple = bufferLastPos.FirstOrDefault(l => l.Item1 == c.ID);
                    if (tupple != default)
                    {
                        c.nextCenter = c.Center;
                        c.nextRadius = c.Radius;
                        c.Center = tupple.Item3;
                        c.Radius = tupple.Item2;
                        c.IsPlaced = true;
                        dealingCircles.Add(c);
                    }
                }

                foreach (TheCircle c in dealingCircles)
                {
                    (string, float, Vector2) tupple = bufferLastPos.FirstOrDefault(l => l.Item1 == c.ID);
                    if (c.Radius < c.nextRadius)
                    {
                        ExpandFromCircleA(circles, c, c.nextRadius);
                    }
                    else if (c.Radius > c.nextRadius)
                    {
                        c.Radius = c.nextRadius;
                    }

                    List<TheCircle> notPlacedCircles = circles.Where(c => !c.IsPlaced).ToList();

                    newNodeIDsSizes = notPlacedCircles.Select(n => (n.ID, n.Radius)).ToList();

                    PackingCircles(circles, containerCenter, newNodeIDsSizes);

                    List<(string, float, Vector2)> placedCircles = circles.Select(c => (c.ID, c.Radius, c.Center)).ToList();

                    lastPositions[parent] = placedCircles;
                }
            }
            else
            {
                newNodeIDsSizes = circles.Select(n => (n.ID, n.Radius)).ToList();
                PackingCircles(circles, containerCenter, newNodeIDsSizes);

                lastPositions[parent] = circles.Select(c => (c.ID, c.Radius, c.Center)).ToList();
            }
        }

        /// <summary>
        /// Increases circle A's radius to <paramref name="newRadius"/> and shifts other
        /// circles outward from A so they remain separated.
        /// </summary>
        /// <param name="circles">
        /// The collection containing A and the other circles to
        /// adjust. The method mutates the Center of other circles and the Radius of A.
        /// </param>
        /// <param name="A">The circle whose radius will be expanded. Its Radius is
        /// set to <paramref name="newRadius"/>.
        /// </param>
        /// <param name="newRadius">The new radius for A. If this value is less than
        /// or equal to A's current radius the method will still assign it but no
        /// outward translation is applied to other circles.
        /// </param>
        /// <remarks>
        /// The method computes the radius difference (rem = newRadius - oldRadius).
        /// If rem > 0 it moves every other circle by rem along the direction
        /// from A's center to that circle's center. If a circle shares the exact
        /// center with A, a fallback direction of (1,0) is used. This is a simple
        /// outward translation and does not perform collision resolution beyond
        /// the uniform offset; callers should re-run packing or collision checks
        /// if stricter non-overlap guarantees are required.
        /// </remarks>
        internal void ExpandFromCircleA(List<TheCircle> circles, TheCircle A, float newRadius)
        {
            float oldRadius = A.Radius;
            float rem = newRadius - oldRadius;

            A.Radius = newRadius;

            if (rem <= 0f) { return; }

            Vector2 centerA = A.Center;

            foreach (TheCircle c in circles)
            {
                if (c == A) { continue; }

                Vector2 dir = c.Center - centerA;
                float dist = dir.magnitude;

                if (dist == 0f)
                {
                    dir = new Vector2(1f, 0f);
                }
                else
                {
                    dir /= dist;
                }
                c.Center += dir * rem;
            }
        }

        /// <summary>
        /// Packs the supplied circles into a tightly fitting circular container.
        /// Adjusts each circle's position so circles do not overlap and computes the
        /// container radius required to enclose them.
        /// </summary>
        /// <param name="circles">
        /// The list of circles to pack. The method will modify the circle instances
        /// (their positions and placement state). Must not be null.
        /// </param>
        /// <param name="containerCenter">The center point of the container used as
        /// the origin for packing.
        /// </param>
        /// <param name="containerRadius">
        /// The computed radius of the enclosing container. If this parameter is passed
        /// by reference (ref/out) the method will set it to the computed value; otherwise
        /// it is used as an initial hint and may be updated by the method.
        /// </param>
        /// <param name="newNodeIDsSizes">
        /// A mapping of new node IDs to their sizes (radii).
        /// Used when the layout must account for newly added nodes. May be null or empty if
        /// there are no new nodes.
        /// </param>
        /// <exception cref="System.ArgumentNullException">
        /// Thrown if <paramref name="circles"/> is null.
        /// </exception>
        /// <remarks>
        /// This routine implements an incremental packing strategy: it places circles
        /// one-by-one (or updates existing placements), attempts to minimize overlap and
        /// compaction, and updates circle centers accordingly. After placement it determines
        /// the minimal enclosing circle radius and writes it to
        /// <paramref name="containerRadius"/>
        /// (when used as an out/ref). The method mutates the provided circle objects and
        /// does not create a new collection. Callers should ensure any external references
        /// to circles remain valid and should validate the returned container radius if they
        /// rely on container bounds.
        /// </remarks>
        internal void PackingCircles(List<TheCircle> circles, Vector2 containerCenter, List<(string, float)> newNodeIDsSizes = null)
        {
            List<TheCircle> placed = new List<TheCircle>();

            placed.AddRange(circles.Where(c => c.IsPlaced));

            circles = circles.Except(placed).ToList();

            foreach (TheCircle circle in circles)
            {
                Vector2 pos = FindEmptyPlace(placed, circle, containerCenter);
                circle.Center = pos;
                placed.Add(circle);
            }
        }

        /// <summary>
        /// Searches for a non-overlapping position for the specified circle relative
        /// to the already placed circles using the packer's incremental placement strategy.
        /// </summary>
        /// <param name="placedCircles">
        /// A collection of circles that have already been positioned and must not be
        /// overlapped. Must not be null (may be empty).
        /// </param>
        /// <param name="circle">
        /// The circle to place; its radius (and optionally its current position) are
        /// used by the algorithm. Must not be null.
        /// </param>
        /// <param name="containerCenter">
        /// The center of the container used as the placement origin. Returned if no
        /// valid non-overlapping position is found.
        /// </param>
        /// <returns>
        /// A Vector2 representing a candidate center for the given circle that does
        /// not intersect any circle in placedCircles. If the method cannot find a
        /// valid location, it returns containerCenter.
        /// </returns>
        /// <remarks>
        /// The implementation probes candidate locations (for example, along an
        /// expanding spiral or other sampling strategy) and returns the first
        /// position that does not overlap any placed circle. The method does
        /// not modify the placedCircles collection. Callers should
        /// verify additional constraints (such as container bounds) if required.
        private Vector2 FindEmptyPlace(List<TheCircle> placedCircles, TheCircle circle, Vector2 containerCenter)
        {
            List<Vector2> candidates = new List<Vector2>();

            candidates.Add(containerCenter);

            if (placedCircles.Count == 0 || placedCircles==null ) { return containerCenter; }

            foreach (TheCircle c in placedCircles)
            {
                float dist = c.Radius + circle.Radius;

                int steps = 20;
                for (int i = 0; i < steps; i++)
                {
                    float angle = (i / (float)steps) * Mathf.PI * 2f;
                    Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                    candidates.Add(c.Center + dir * dist);
                }
            }

            Vector2 bestPos = Vector2.zero;
            float bestScore = float.MaxValue;
            bool found = false;

            foreach (Vector2 pos in candidates)
            {
                if (IsOverlapping(pos, circle.Radius, placedCircles)) { continue; }

                circle.Center = pos;
                float score = ComputeSurroundingCircle(placedCircles.Concat(new[] { circle }).ToList()).Radius;

                if (score < bestScore)
                {
                    bestScore = score;
                    bestPos = pos;
                    found = true;
                }
            }

            if (found) { return bestPos; }

            float stepSize = circle.Radius * 0.5f;
            int ringSteps = 24;

            for (int ring = 1; ring < 100; ring++)
            {
                float dist = ring * stepSize;

                for (int i = 0; i < ringSteps; i++)
                {
                    float angle = (i / (float)ringSteps) * Mathf.PI * 2f;
                    Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

                    Vector2 pos = containerCenter + dir * dist;

                    if (!IsOverlapping(pos, circle.Radius, placedCircles))
                    {
                        return pos;
                    }
                }
            }
            Debug.LogError("No valid position found even after expanding!\n");
            return containerCenter;
        }

        /// <summary>
        /// Determines whether a candidate circle located at the given position with the specified
        /// radius intersects any circle in the provided collection of placed circles.
        /// </summary>
        /// <param name="pos">The candidate circle's center position to test.</param>
        /// <param name="radius">The candidate circle's radius (expected to be non-negative).</param>
        /// <param name="placedCircles">A collection of already placed circles to test against.
        /// This collection must not be null.</param>
        /// <returns> True if the candidate circle overlaps (intersects) any circle in
        /// placedCircles; otherwise false.
        /// </returns>
        /// <remarks>
        /// The check is typically performed by comparing the distance between centers
        /// to the sum of radii. The method does not modify the placedCircles collection.
        /// Callers should ensure radius and placedCircles are valid before calling.
        private bool IsOverlapping(Vector2 pos, float radius, List<TheCircle> placedCircles)
        {
            foreach (TheCircle c in placedCircles)
            {
                float dist = Vector2.Distance(pos, c.Center);
                if (dist < (radius + c.Radius))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Computes a circle that encloses all of the specified circles and returns a new circle
        /// instance with the computed center and radius.
        /// </summary>
        /// <param name="circles">A collection of circles to enclose. Each entry is treated as a
        /// filled disk (center + radius).</param>
        /// <returns>
        /// A Circle1 instance whose center and radius are chosen so that every input circle
        /// lies entirely inside the returned circle.
        /// </returns>
        /// <remarks>
        /// The returned circle's radius is at least the maximum distance from the computed
        /// center to any input circle's outer edge (center distance + that circle's radius).
        /// The method does not modify the input collection. If the input is empty the method
        /// returns a circle that represents an empty enclosure (implementation-defined);
        /// callers should handle that case if necessary.
        /// </remarks>
        internal TheCircle ComputeSurroundingCircle(List<TheCircle> circles)
        {
            if (circles.Count == 0) { return new TheCircle(null, Vector2.zero, 0); }
            if (circles.Count == 1)
            {
                return new TheCircle(null, circles[0].Center, circles[0].Radius);
            }

            TheCircle best = null;
            float largestRadius = 0f;

            for (int i = 0; i < circles.Count; i++)
            {
                for (int j = i + 1; j < circles.Count; j++)
                {
                    TheCircle circleA = circles[i];
                    TheCircle circleB = circles[j];

                    float d = Vector2.Distance(circleA.Center, circleB.Center);

                    if (d + Mathf.Min(circleA.Radius, circleB.Radius) <= Mathf.Max(circleA.Radius, circleB.Radius))
                    {
                        TheCircle larger = circleA.Radius > circleB.Radius ? circleA : circleB;

                        if (larger.Radius > largestRadius)
                        {
                            largestRadius = larger.Radius;
                            best = new TheCircle(null, larger.Center, larger.Radius);
                        }
                        continue;
                    }
                    float R = (d + circleA.Radius + circleB.Radius) / 2f;
                    Vector2 dir = (circleB.Center - circleA.Center).normalized;
                    Vector2 center = circleA.Center + dir * (R - circleA.Radius);
                    if (R > largestRadius)
                    {
                        largestRadius = R;
                        best = new TheCircle(null, center, R);
                    }
                }
            }
            return best;
        }
    }
}
