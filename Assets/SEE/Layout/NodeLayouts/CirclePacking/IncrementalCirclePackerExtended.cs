using System;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Layout.NodeLayouts.CirclePacking
{

    /// <summary>
    /// Implements a mass-weighted centripetal relaxation algorithm to pack circles densely in a 2D space.
    /// Utilizing Position-Based Dynamics (PBD), this class creates a tightly nested, center-seeking cluster 
    /// while strictly enforcing non-overlap constraints.
    /// </summary>
    /// <remarks>
    /// The packing style mimics a microscopic physics simulation:
    /// <list type="bullet">
    /// <item><description><c>Centripetal Gravity:</c> All circles are constantly pulled toward a central focal point, forming a dense, roughly spherical layout.</description></item>
    /// <item><description><c>Mass-Weighting:</c> A circle's resistance to movement is based on its area. Large circles act as stable structural anchors, while smaller circles behave fluidly, weaving through gaps to fill empty space.</description></item>
    /// <item><description><c>Iterative Relaxation:</c> The layout organically "jiggles" into its lowest-energy state by rapidly alternating between an inward pull and rigid outward collision resolution.</description></item>
    /// </list>
    /// </remarks>
    public class IncrementalCirclePackerExtended
    {
        /// <summary>
        /// A spatial partitioning structure that divides the 2D space into cells. 
        /// This drastically reduces collision-check overhead from O(N^2) to near O(N) by only checking adjacent cells.
        /// </summary>
        private SpatialHashGrid _grid;

        /// <summary>
        /// The X-coordinate of the gravitational focal point. All circles will naturally gravitate toward this axis.
        /// </summary>
        public float CenterX { get; set; } = 0f;

        /// <summary>
        /// The Y-coordinate of the gravitational focal point. All circles will naturally gravitate toward this axis.
        /// </summary>
        public float CenterY { get; set; } = 0f;

        /// <summary>
        /// The intensity of the centripetal pull per simulation step. 
        /// For microscopic layouts (radii &lt; 1.0f), this should be kept very small (e.g., 0.01f) to prevent circles from shooting through each other.
        /// </summary>
        public float GravityStrength { get; set; } = 0.01f;
        /// <summary>
        /// The number of Position-Based Dynamics (PBD) collision resolution passes per single layout step.
        /// Higher values result in "stiffer" circles and strict non-overlap, while lower values are faster but may result in spongy/overlapping layouts.
        /// </summary>
        public int PbdIterations { get; set; } = 10; 

        /// <summary>
        /// The calculated radius of the smallest enclosing circle (centered at CenterX, CenterY) that perfectly 
        /// contains the outermost edges of the packed cluster.
        /// </summary>
        public float BoundingRadius { get; private set; }


        /// <summary>
        /// Initializes a new instance of the <see cref="IncrementalCirclePackerExtended"/> class.
        /// Configures the underlying spatial hash grid based on the largest expected circle diameter to optimize collision detection.
        /// </summary>
        /// <param name="maxCircleDiameter">The maximum expected diameter of a circle, used to define the grid's optimal cell size.</param>
        public IncrementalCirclePackerExtended(float maxCircleDiameter)
        {
            _grid = new SpatialHashGrid(maxCircleDiameter);
        }

        /// <summary>
        /// Clears the spatial hash grid and repopulates it based on the current coordinates of the provided circles.
        /// This must be called immediately before resolving collisions so the engine knows exactly which circles are near each other.
        /// </summary>
        /// <param name="circles">The list of circles to map into the spatial grid.</param>
        public void RebuildGrid(List<TheCircle> circles)
        {
            _grid.Clear();
            foreach (TheCircle circle in circles)
            {
                _grid.Insert(circle);
            }
        }

        /// <summary>
        /// Executes the discrete packing algorithm. Iteratively applies inward gravity and resolves rigid-body collisions 
        /// until the layout reaches a stable equilibrium (circles stop moving and all overlaps are resolved) or hits the max steps.
        /// </summary>
        /// <param name="maxSteps">The hard limit for simulation steps to prevent an infinite loop if the layout cannot settle.</param>
        /// <param name="circles">The target list of circles to pack. If null or empty, the method safely returns.</param>
        public void ComputePacking(int maxSteps = 5, List<TheCircle> circles = null) 
        {
            if (circles == null || circles.Count == 0) { return; }

            float stopMovementThreshold = 0.001f;
            float stopMovementSq = stopMovementThreshold * stopMovementThreshold;
            float allowedOverlapTolerance = 0.00000001f; 

            bool settledSuccessfully = false;

            for (int step = 0; step < maxSteps; step++)
            {
                float maxMovementSq = 0f;
                float maxOverlapThisStep = 0f;

                foreach (TheCircle c in circles)
                {
                    c.PrevX = c.X;
                    c.PrevY = c.Y;
                }

                foreach (TheCircle c in circles)
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


                for (int i = 0; i < PbdIterations; i++)
                {
                    RebuildGrid(circles);
                    float currentOverlap = ResolveCollisions(circles);

                    if (currentOverlap > maxOverlapThisStep)
                    {
                        maxOverlapThisStep = currentOverlap;
                    }
                }

                foreach (TheCircle c in circles)
                {
                    float moveX = c.X - c.PrevX;
                    float moveY = c.Y - c.PrevY;
                    float moveSq = (moveX * moveX) + (moveY * moveY);

                    if (moveSq > maxMovementSq)
                    {
                        maxMovementSq = moveSq;
                    }
                }

                if (maxMovementSq <= stopMovementSq && maxOverlapThisStep <= allowedOverlapTolerance)
                {
                    settledSuccessfully = true;
                    break;
                }
            }
            ///<summary>
            /// This method computes the packing of the circles.
            /// It iteratively adjusts the positions of the circles to minimize overlaps and settle into a stable configuration.
            /// if the maximum movement of any circle is below a certain threshold and the maximum overlap is within an allowed tolerance
            /// , the packing is considered successful.
            /// </summary>


            UpdateBoundingCircle(circles);
        }


        /// <summary>
        /// Detects physical intersections between neighboring circles and enforces strict non-overlap constraints.
        /// It pushes colliding circles apart along their axis of intersection, weighting the displacement 
        /// proportionally by their squared radii to simulate mass (larger circles move less).
        /// </summary>
        /// <param name="circles">The list of circles to evaluate for collisions.</param>
        /// <returns>The depth of the single worst (largest) overlap found during this iteration.</returns>
        public float ResolveCollisions(List<TheCircle> circles)
        {
            float maxOverlapFound = 0f;

            foreach (TheCircle c1 in circles)
            {
                List<TheCircle> neighbors = _grid.GetNearby(c1);

                foreach (TheCircle c2 in neighbors)
                {
                    if (c1.ID == c2.ID) { continue; }

                    double dx = c2.X - c1.X;
                    double dy = c2.Y - c1.Y;
                    double distSq = dx * dx + dy * dy;
                    double radSum = c1.Radius + c2.Radius;


                    if (distSq < radSum * radSum)
                    {
                        double dist = Math.Sqrt(distSq);

                        if (dist < 0.0000001)
                        {
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

        /// <summary>
        /// Recomputes the BoundingRadius property by finding the circle farthest from the central focal point.
        /// The resulting radius tightly wraps the outermost edge of the current cluster.
        /// </summary>
        /// <param name="circles">The list of currently packed circles to encompass.</param>
        private void UpdateBoundingCircle(List<TheCircle> circles)
        {
            float maxDistSq = 0f;
            foreach (TheCircle c in circles)
            {
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
