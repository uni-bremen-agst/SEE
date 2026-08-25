using LibGit2Sharp;
using Markdig.Helpers;
using SEE.Layout.NodeLayouts.RectanglePacking;
using Sirenix.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SEE.Layout.NodeLayouts.CirclePacking
{
    internal class Circle1
    {
        public Vector2 Center;
        public float X
        {
            get => Center.x;
            set => Center.x = value;
        }

        public float Y
        {
            get => Center.y;
            set => Center.y = value;
        }
        public float Radius;
        public ILayoutNode GameObject;
        public List<Circle1> Children;
        public string ID;
        public bool IsPlaced { get; set; }

        public float PrevX { get; set; }
        public float PrevY { get; set; }

        public Vector2 nextCenter { get; set; }
        public float nextRadius { get; set; }

        public Circle1(ILayoutNode gameObject, Vector2 center, float radius)
        {
            this.GameObject = gameObject;
            this.Center = center;
            this.Radius = radius;
            Children = new List<Circle1>();
            ID = gameObject != null ? gameObject.ID : null;
            X = center.x;
            Y = center.y;
            IsPlaced = false;
        }


        public override string ToString()
        {
            return "(ID=" + ID + " center= " + Center.ToString() + ", radius=" + Radius + ")";
        }
    }

    /// <summary>
    /// This class holds a list of <see cref="Circle"/> objects and packs them closely.
    /// The original source can be found
    /// <see href="https://www.codeproject.com/Articles/42067/D-Circle-Packing-Algorithm-Ported-to-Csharp">HERE</see>.
    /// </summary>
    public static class IncrementalCirclePacker
    {

        public static Dictionary<string, List<(string, float, Vector2)>> lastPositions;

        //********************************************************************************************************************************

        internal static void PackCircles(List<Circle1> circles, Vector2 containerCenter, out float containerRadius, bool useOldLayout, string parentID)
        {
            if (useOldLayout)
            {
                lastPositions = new Dictionary<string, List<(string, float, Vector2)>>();
            }
            PerformHistory(circles, parentID, containerCenter, out containerRadius);

            float maxCircleDiame = 0f;
            foreach (Circle1 circle in circles)
            {
                if (circle.Radius * 2f > maxCircleDiame)
                {
                    maxCircleDiame = circle.Radius * 2f;
                }
            }

            CirclePacker2 packer = new CirclePacker2(maxCircleDiame);
            packer.PbdIterations = 10;
            packer.ComputePacking(100, circles);

            for (int i = 0; i < circles.Count; i++)
            {
                //Debug.Log(circles[i].ToString());
            }
            //Debug.Log("containerRadius " + containerRadius + "containerCenter " + containerCenter + "++++++++++++++++++++++++++++++++++++++++++");
            containerRadius = ComputeSurroundingCircle11ResetCircles(circles).Radius;
            //containerRadius = 1f;

        }

        private static void PerformHistory(List<Circle1> circles, string parent, Vector2 containerCenter, out float containerRadius)
        {
            containerRadius = 0f;

            List<(string, float)> newNodeIDsSizes = new List<(string, float)>();

            var bufferLastPos = lastPositions.FirstOrDefault(p => p.Key == parent).Value;

            if (bufferLastPos != default)
            {
                List<Circle1> dealingCircles = new List<Circle1>();
                //public static List<(string, List<(string, float, Vector2)>)> lastPositions;
                foreach (Circle1 c in circles)
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

                foreach (Circle1 c in dealingCircles)
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

                    var notPlacedCircles = circles.Where(c => !c.IsPlaced).ToList();

                    newNodeIDsSizes = notPlacedCircles.Select(n => (n.ID, n.Radius)).ToList();

                    PackCircles1(circles, containerCenter, out containerRadius, newNodeIDsSizes);

                    var placedCircles = circles.Select(c => (c.ID, c.Radius, c.Center)).ToList();

                    lastPositions[parent] = placedCircles;
                }
            }
            else
            {
                newNodeIDsSizes = circles.Select(n => (n.ID, n.Radius)).ToList();
                PackCircles1(circles, containerCenter, out containerRadius, newNodeIDsSizes);

                lastPositions[parent] = circles.Select(c => (c.ID, c.Radius, c.Center)).ToList();
                //Debug.Log("6");
            }
        }

        #region case5 helper methods

        internal static void ExpandFromCircleA(List<Circle1> circles, Circle1 A, float newRadius)
        {
            float oldRadius = A.Radius;
            float rem = newRadius - oldRadius;

            // Update A first
            A.Radius = newRadius;

            // If no growth, nothing to do
            if (rem <= 0f)
                return;

            Vector2 centerA = A.Center;

            foreach (Circle1 c in circles)
            {
                if (c == A)
                    continue;

                Vector2 dir = c.Center - centerA;
                float dist = dir.magnitude;

                //Debug.Log("Expanding from circle " + A.ID + " to circle " + c.ID + " with rem " + rem + " and dist " + dist + " " + dir);

                // If exactly at center, choose fixed direction (deterministic)
                if (dist == 0f)
                {
                    dir = new Vector2(1f, 0f);
                }
                else
                {
                    dir /= dist; // normalize
                }

                // Move outward by rem
                c.Center += dir * rem;
            }
        }


        internal static void PackCircles1(List<Circle1> circles, Vector2 containerCenter, out float containerRadius, List<(string, float)> newNodeIDsSizes = null)
        {

            List<Circle1> placed = new List<Circle1>();
            containerRadius = 0f;

            placed.AddRange(circles.Where(c => c.IsPlaced));

            circles = circles.Except(placed).ToList();

            foreach (Circle1 circle in circles)
            {
                Vector2 pos = FindEmptyPlace12(
                    placed,
                    circle,
                    containerCenter
                );

                circle.Center = pos;
                placed.Add(circle);

            }


        }
        private static Vector2 FindEmptyPlace12(List<Circle1> placedCircles, Circle1 circle, Vector2 containerCenter)
        {
            List<Vector2> candidates = new List<Vector2>();

            // 1. Try center
            candidates.Add(containerCenter);

            // 2. Generate candidates around existing circles
            foreach (Circle1 c in placedCircles)
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

            // 3. Try normal candidates
            Vector2 bestPos = Vector2.zero;
            float bestScore = float.MaxValue;
            bool found = false;

            foreach (Vector2 pos in candidates)
            {
                if (IsOverlapping(pos, circle.Radius, placedCircles))
                    continue;


                circle.Center = pos; // temporarily set for scoring
                float score = ComputeSurroundingCircle11(placedCircles.Concat(new[] { circle }).ToList()).Radius;
                //float score = (pos - containerCenter).sqrMagnitude;

                if (score < bestScore)
                {
                    bestScore = score;
                    bestPos = pos;
                    found = true;
                }
            }

            if (found)
                return bestPos;

            // 4. Deterministic fallback (expanding rings)
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
                        //Debug.Log("found deterministic fallback");
                        return pos;

                    }
                }
            }

            Debug.LogError("No valid position found even after expanding!");
            return containerCenter; // fallback fallback
        }
        private static bool IsOverlapping(Vector2 pos, float radius, List<Circle1> placedCircles)
        {
            foreach (Circle1 c in placedCircles)
            {
                float dist = Vector2.Distance(pos, c.Center);
                if (dist < (radius + c.Radius))
                    return true;
            }
            return false;
        }
        internal static Circle1 ComputeSurroundingCircle11(List<Circle1> circles)
        {
            if (circles.Count == 0)
                return new Circle1(null, Vector2.zero, 0);

            if (circles.Count == 1)
                return new Circle1(
                    null,
                    circles[0].Center,
                    circles[0].Radius
                );

            Circle1 best = null;
            float largestRadius = 0f;

            // Check every pair
            for (int i = 0; i < circles.Count; i++)
            {
                for (int j = i + 1; j < circles.Count; j++)
                {
                    Circle1 a = circles[i];
                    Circle1 b = circles[j];

                    float d = Vector2.Distance(a.Center, b.Center);

                    // one circle fully contains the other
                    if (d + Mathf.Min(a.Radius, b.Radius) <= Mathf.Max(a.Radius, b.Radius))
                    {
                        Circle1 larger =
                            a.Radius > b.Radius ? a : b;

                        if (larger.Radius > largestRadius)
                        {
                            largestRadius = larger.Radius;
                            best = new Circle1(
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
                        best = new Circle1(
                            null,
                            center,
                            R
                        );
                    }
                }
            }

            return best;
        }
        internal static Circle1 ComputeSurroundingCircle11ResetCircles(List<Circle1> circles)
        {
            if (circles.Count == 0)
                return new Circle1(null, Vector2.zero, 0);

            if (circles.Count == 1)
                return new Circle1(
                    null,
                    circles[0].Center,
                    circles[0].Radius
                );

            Circle1 best = null;
            float largestRadius = 0f;

            // Check every pair
            for (int i = 0; i < circles.Count; i++)
            {
                for (int j = i + 1; j < circles.Count; j++)
                {
                    Circle1 a = circles[i];
                    Circle1 b = circles[j];

                    float d = Vector2.Distance(a.Center, b.Center);

                    // one circle fully contains the other
                    if (d + Mathf.Min(a.Radius, b.Radius) <= Mathf.Max(a.Radius, b.Radius))
                    {
                        Circle1 larger =
                            a.Radius > b.Radius ? a : b;

                        if (larger.Radius > largestRadius)
                        {
                            largestRadius = larger.Radius;
                            best = new Circle1(
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
                        best = new Circle1(
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

                foreach (Circle1 c in circles)
                {
                    c.Center -= offset;
                }

                best.Center = Vector2.zero;
            }

            return best;
        }
        #endregion
        //********************************************************************************************************************************

    }


    internal class SpatialHashGrid
    {
        public float _cellSize;
        public Dictionary<Tuple<int, int>, List<Circle1>> _cells;

        public SpatialHashGrid(float cellSize)
        {
            _cellSize = cellSize;
            _cells = new Dictionary<Tuple<int, int>, List<Circle1>>();
        }

        public void Clear()
        {
            _cells.Clear();
        }

        public void Insert(Circle1 circle)
        {
            int cellX = (int)MathF.Floor(circle.X / _cellSize);
            int cellY = (int)MathF.Floor(circle.Y / _cellSize);
            Tuple<int, int> key = new Tuple<int, int>(cellX, cellY);

            if (!_cells.ContainsKey(key))
            {
                _cells[key] = new List<Circle1>();
            }
            _cells[key].Add(circle);
        }

        // Retrieves circles in the target cell and the 8 surrounding cells
        public List<Circle1> GetNearby(Circle1 circle)
        {
            List<Circle1> nearby = new List<Circle1>();
            int cellX = (int)MathF.Floor(circle.X / _cellSize);
            int cellY = (int)MathF.Floor(circle.Y / _cellSize);

            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    Tuple<int, int> key = new Tuple<int, int>(cellX + i, cellY + j);
                    if (_cells.TryGetValue(key, out List<Circle1> cellCircles))
                    {
                        nearby.AddRange(cellCircles);
                    }
                }
            }
            return nearby;
        }
    }

    internal class CirclePacker2
    {
        public List<Circle1> Circles { get; private set; }
        private SpatialHashGrid _grid;

        // Simulation parameters
        public float CenterX { get; set; } = 0f;
        public float CenterY { get; set; } = 0f;
        public float GravityStrength { get; set; } = 0.01f;
        public int PbdIterations { get; set; } = 10; // Higher = stiffer, more accurate packing
        public float BoundingRadius { get; private set; }

        public CirclePacker2(float maxCircleDiameter)
        {
            _grid = new SpatialHashGrid(maxCircleDiameter);
        }

        public void AddCircle(Circle1 c)
        {
            Circles.Add(c);
        }



        public void RebuildGrid(List<Circle1> circles)
        {
            _grid.Clear();
            foreach (Circle1 circle in circles)
            {
                _grid.Insert(circle);
            }
        }

        // Runs the packing algorithm until the circles settle into place

        public void ComputePacking(int maxSteps = 5, List<Circle1> circles = null) // Increased max steps drastically
        {
            if (circles == null || circles.Count == 0) return;

            // Microscopic thresholds
            float stopMovementThreshold = 0.001f;
            float stopMovementSq = stopMovementThreshold * stopMovementThreshold;
            float allowedOverlapTolerance = 0.00000001f; // Tighter tolerance

            bool settledSuccessfully = false;

            for (int step = 0; step < maxSteps; step++)
            {
                float maxMovementSq = 0f;
                float maxOverlapThisStep = 0f;

                foreach (Circle1 c in circles)
                {
                    c.PrevX = c.X;
                    c.PrevY = c.Y;
                }

                // 1. Apply Gravity
                foreach (Circle1 c in circles)
                {
                    float dx = CenterX - c.X;
                    float dy = CenterY - c.Y;
                    float dist = MathF.Sqrt(dx * dx + dy * dy);

                    if (dist > 0)
                    {
                        c.X += (dx / dist) * GravityStrength;
                        c.Y += (dy / dist) * GravityStrength;
                    }
                }


                // 2. Resolve Collisions
                for (int i = 0; i < PbdIterations; i++)
                {
                    RebuildGrid(circles);
                    float currentOverlap = ResolveCollisions(circles);

                    if (currentOverlap > maxOverlapThisStep)
                    {
                        maxOverlapThisStep = currentOverlap;
                    }
                }

                // 3. Check movements
                foreach (Circle1 c in circles)
                {
                    float moveX = c.X - c.PrevX;
                    float moveY = c.Y - c.PrevY;
                    float moveSq = (moveX * moveX) + (moveY * moveY);

                    if (moveSq > maxMovementSq)
                    {
                        maxMovementSq = moveSq;
                    }
                }

                // 4. Strict Exit Condition
                if (maxMovementSq <= stopMovementSq && maxOverlapThisStep <= allowedOverlapTolerance)
                {
                    //Debug.Log($"[SUCCESS] Packing settled perfectly in {step} steps. Max Overlap remaining: {maxOverlapThisStep}");
                    settledSuccessfully = true;
                    break;
                }
            }

            if (!settledSuccessfully)
            {
                // If you see this in your console, your Gravity is too high, or Iterations are too low!
                //Debug.Log($"[WARNING] Reached {maxSteps} limit WITHOUT settling perfectly. Overlaps likely remain.");
            }

            UpdateBoundingCircle(circles);
        }

        public float ResolveCollisions(List<Circle1> circles)
        {
            float maxOverlapFound = 0f;
            //Debug.Log("grid celll size: " + _grid._cellSize);

            foreach (Circle1 c1 in circles)
            {
                List<Circle1> neighbors = _grid.GetNearby(c1);
                //Debug.Log($"Circle {c1.ID} has {neighbors.Count} neighbors to check for collisions.");

                foreach (Circle1 c2 in neighbors)
                {
                    //if (string.CompareOrdinal(c1.ID, c2.ID) >= 0) continue;
                    if (c1.ID == c2.ID) continue;

                    // Use DOUBLE precision internally for tiny coordinate math
                    double dx = c2.X - c1.X;
                    double dy = c2.Y - c1.Y;
                    double distSq = dx * dx + dy * dy;
                    double radSum = c1.Radius + c2.Radius;


                    if (distSq < radSum * radSum)
                    {
                        double dist = Math.Sqrt(distSq);

                        // Handle the edge case of exact same coordinates perfectly without epsilon
                        if (dist < 0.0000001)
                        {
                            // Push them apart slightly on a random axis so the next pass can catch them
                            c1.X -= 0.001f;
                            c2.X += 0.001f;
                            continue;
                        }

                        double overlap = radSum - dist;

                        if (overlap > maxOverlapFound)
                        {
                            maxOverlapFound = (float)overlap;
                        }

                        double nx = dx / dist;
                        double ny = dy / dist;

                        // Mass calculations
                        double mass1 = c1.Radius * c1.Radius;
                        double mass2 = c2.Radius * c2.Radius;
                        double totalMass = mass1 + mass2;

                        double ratio1 = mass2 / totalMass;
                        double ratio2 = mass1 / totalMass;

                        c1.X -= (float)(nx * overlap * ratio1);
                        c1.Y -= (float)(ny * overlap * ratio1);


                        c2.X += (float)(nx * overlap * ratio2);
                        c2.Y += (float)(ny * overlap * ratio2);

                    }
                }
            }


            return maxOverlapFound;
        }


        private void UpdateBoundingCircle(List<Circle1> circles)
        {
            float maxDistSq = 0f;
            foreach (Circle1 c in circles)
            {
                // Distance from center to the outer edge of the circle
                float dx = c.X - CenterX;
                float dy = c.Y - CenterY;
                float dist = MathF.Sqrt(dx * dx + dy * dy) + c.Radius;

                if (dist > maxDistSq)
                {
                    maxDistSq = dist;
                }
            }
            BoundingRadius = maxDistSq;
        }
    }
}
