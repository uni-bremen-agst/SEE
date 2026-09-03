using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SEE.Layout.NodeLayouts.CirclePacking
{
    /// <summary>
    /// This class holds a list of <see cref="Circle"/> objects and packs them closely.
    /// The original source can be found
    /// <see href="https://www.codeproject.com/Articles/42067/D-Circle-Packing-Algorithm-Ported-to-Csharp">HERE</see>.
    /// </summary>
    public class IncrementalCirclePacker
    {
        /// <summary>
        /// Packs the given list of circles into a container, adjusting 
        /// their positions and radii to minimize overlap and fit within the container.
        /// </summary>
        /// <param name="circles"></param>
        /// <param name="containerCenter"></param>
        /// <param name="containerRadius"></param>
        /// <param name="lastPositionsParam"></param>
        /// <param name="parentID"></param>
        internal static void PackCircles(List<TheCircle> circles, Vector2 containerCenter, out float containerRadius, Dictionary<string, List<(string, float, Vector2)>> lastPositionsParam, string parentID)
        {
            IncrementalCirclePacker packer = new IncrementalCirclePacker();

            packer.PerformHistory(circles, parentID, containerCenter, out containerRadius, lastPositionsParam);

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
            containerRadius = packer.ComputeSurroundingCircleAndResetCircles(circles).Radius;
        }

        /// <summary>
        /// Performs the packing of circles based on their last known positions, 
        /// adjusting their centers and radii as necessary.
        /// </summary>
        /// <param name="circles"></param>
        /// <param name="parent"></param>
        /// <param name="containerCenter"></param>
        /// <param name="containerRadius"></param>
        /// <param name="lastPositions"></param>
        private void PerformHistory(List<TheCircle> circles, string parent, Vector2 containerCenter, out float containerRadius, Dictionary<string, List<(string, float, Vector2)>> lastPositions)
        {
            containerRadius = 0f;
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

                    PackingCircles(circles, containerCenter, out containerRadius, newNodeIDsSizes);

                    List<(string, float, Vector2)> placedCircles = circles.Select(c => (c.ID, c.Radius, c.Center)).ToList();

                    lastPositions[parent] = placedCircles;
                }
            }
            else
            {
                newNodeIDsSizes = circles.Select(n => (n.ID, n.Radius)).ToList();
                PackingCircles(circles, containerCenter, out containerRadius, newNodeIDsSizes);

                lastPositions[parent] = circles.Select(c => (c.ID, c.Radius, c.Center)).ToList();
            }
        }

        /// <summary>
        /// Expands the radius of circle A to a new radius and adjusts the positions of other circles accordingly.
        /// </summary>
        /// <param name="circles"></param>
        /// <param name="A"></param>
        /// <param name="newRadius"></param>
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
        /// Packs the given list of circles into a container, 
        /// adjusting their positions to minimize overlap and fit within the container.
        /// </summary>
        /// <param name="circles"></param>
        /// <param name="containerCenter"></param>
        /// <param name="containerRadius"></param>
        /// <param name="newNodeIDsSizes"></param>
        internal void PackingCircles(List<TheCircle> circles, Vector2 containerCenter, out float containerRadius, List<(string, float)> newNodeIDsSizes = null)
        {

            List<TheCircle> placed = new List<TheCircle>();
            containerRadius = 0f;

            placed.AddRange(circles.Where(c => c.IsPlaced));

            circles = circles.Except(placed).ToList();

            foreach (TheCircle circle in circles)
            {
                Vector2 pos = FindEmptyPlace(
                    placed,
                    circle,
                    containerCenter
                );
                circle.Center = pos;
                placed.Add(circle);

            }
        }

        /// <summary>
        /// Finds an empty position for the given circle that does not overlap with already placed circles.
        /// </summary>
        /// <param name="placedCircles"></param>
        /// <param name="circle"></param>
        /// <param name="containerCenter"></param>
        /// <returns>The empty position, or the container center if no valid position is found.</returns>
        private Vector2 FindEmptyPlace(List<TheCircle> placedCircles, TheCircle circle, Vector2 containerCenter)
        {
            List<Vector2> candidates = new List<Vector2>();

            candidates.Add(containerCenter);

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
        /// Checks if the given position and radius overlaps with any of the already placed circles.
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="radius"></param>
        /// <param name="placedCircles"></param>
        /// <returns>if the position and radius overlap with any placed circle</returns>
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
        /// Computes the smallest circle that can surround all the given circles, 
        /// returning a new circle with the computed center and radius.
        /// </summary>
        /// <param name="circles"></param>
        /// <returns>circle that surrounds all the given circles</returns>
        internal TheCircle ComputeSurroundingCircle(List<TheCircle> circles)
        {
            if (circles.Count == 0) { return new TheCircle(null, Vector2.zero, 0); }

            if (circles.Count == 1)
            {
                return new TheCircle(
                    null,
                    circles[0].Center,
                    circles[0].Radius
                );
            }

            TheCircle best = null;
            float largestRadius = 0f;

            for (int i = 0; i < circles.Count; i++)
            {
                for (int j = i + 1; j < circles.Count; j++)
                {
                    TheCircle a = circles[i];
                    TheCircle b = circles[j];

                    float d = Vector2.Distance(a.Center, b.Center);

                    if (d + Mathf.Min(a.Radius, b.Radius) <= Mathf.Max(a.Radius, b.Radius))
                    {
                        TheCircle larger =
                            a.Radius > b.Radius ? a : b;

                        if (larger.Radius > largestRadius)
                        {
                            largestRadius = larger.Radius;
                            best = new TheCircle(
                                null,
                                larger.Center,
                                larger.Radius
                            );
                        }

                        continue;
                    }

                    float R = (d + a.Radius + b.Radius) / 2f;

                    Vector2 dir = (b.Center - a.Center).normalized;

                    Vector2 center =
                        a.Center +
                        dir * (R - a.Radius);

                    if (R > largestRadius)
                    {
                        largestRadius = R;
                        best = new TheCircle(
                            null,
                            center,
                            R
                        );
                    }
                }
            }

            return best;
        }

        /// <summary>
        /// Computes the smallest circle that can surround all the given circles,
        /// and resets the positions of all circles to be relative to the surrounding circle.
        /// </summary>
        /// <param name="circles"></param>
        /// <returns></returns>
        internal TheCircle ComputeSurroundingCircleAndResetCircles(List<TheCircle> circles)
        {
            if (circles.Count == 0) { return new TheCircle(null, Vector2.zero, 0); }


            if (circles.Count == 1)
            {
                return new TheCircle(
                    null,
                    circles[0].Center,
                    circles[0].Radius
                );
            }

            TheCircle best = null;
            float largestRadius = 0f;

            for (int i = 0; i < circles.Count; i++)
            {
                for (int j = i + 1; j < circles.Count; j++)
                {
                    TheCircle a = circles[i];
                    TheCircle b = circles[j];

                    float d = Vector2.Distance(a.Center, b.Center);

                    if (d + Mathf.Min(a.Radius, b.Radius) <= Mathf.Max(a.Radius, b.Radius))
                    {
                        TheCircle larger =
                            a.Radius > b.Radius ? a : b;

                        if (larger.Radius > largestRadius)
                        {
                            largestRadius = larger.Radius;
                            best = new TheCircle(
                                null,
                                larger.Center,
                                larger.Radius
                            );
                        }
                        continue;
                    }

                    float R = (d + a.Radius + b.Radius) / 2f;

                    Vector2 dir = (b.Center - a.Center).normalized;

                    Vector2 center =
                        a.Center +
                        dir * (R - a.Radius);

                    if (R > largestRadius)
                    {
                        largestRadius = R;
                        best = new TheCircle(
                            null,
                            center,
                            R
                        );
                    }
                }
            }

            if (best != null)
            {
                Vector2 offset = best.Center;

                foreach (TheCircle c in circles)
                {
                    c.Center -= offset;
                }

                best.Center = Vector2.zero;
            }

            return best;
        }
    }
}
