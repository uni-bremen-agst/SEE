using System;
using System.Collections.Generic;

namespace SEE.Layout.NodeLayouts.CirclePacking
{
    /// <summary>
    /// Represents a spatial hash grid for efficient collision detection and proximity queries of circles in a 2D space.
    /// </summary>
    public class SpatialHashGrid
    {
        /// <summary>
        /// The size of each cell in the grid.
        /// </summary>
        public float cellSize;

        /// <summary>
        /// A dictionary that maps each cell (represented by a tuple of its x and y indices) 
        /// to a list of circles contained within that cell.
        /// </summary>
        public Dictionary<Tuple<int, int>, List<TheCircle>> cells;

        /// <summary>
        /// Initializes a new instance of the <see cref="SpatialHashGrid"/> class with the specified cell size.
        /// </summary>
        /// <param name="cellSize"></param>
        public SpatialHashGrid(float cellSize)
        {
            this.cellSize = cellSize;
            cells = new Dictionary<Tuple<int, int>, List<TheCircle>>();
        }

        /// <summary>
        /// Clears all cells in the grid, effectively removing all circles from the grid.
        /// </summary>
        public void Clear()
        {
            cells.Clear();
        }

        /// <summary>
        /// Inserts a circle into the appropriate cell in the grid based on its position.
        /// </summary>
        /// <param name="circle"></param>
        public void Insert(TheCircle circle)
        {
            int cellX = (int)MathF.Floor(circle.X / cellSize);
            int cellY = (int)MathF.Floor(circle.Y / cellSize);
            Tuple<int, int> key = new Tuple<int, int>(cellX, cellY);

            if (!cells.ContainsKey(key))
            {
                cells[key] = new List<TheCircle>();
            }
            cells[key].Add(circle);
        }

        /// <summary>
        /// Returns all circles stored in the spatial hash grid that are in the same cell as the specified circle
        /// or in any of the eight directly adjacent cells (the 3×3 neighborhood centered on the circle's 
        /// cell).
        /// </summary>
        /// <param name="circle">The circle whose neighborhood to query.
        /// Its X and Y coordinates are used to compute the cell index
        /// (floor(X / cellSize), floor(Y / cellSize)). Must not be null.</param>
        /// <returns> A list containing all circles from the same cell and adjacent cells.
        /// The returned list is a snapshot of the entries found in those cells and may
        /// include the input circle if it is present in the grid.</returns>
        /// <remarks> The method computes the target cell by flooring the circle's coordinates
        /// divided by the configured cellSize, then iterates over the offsets -1..1 in both
        /// X and Y to collect circles from the 3×3 block of cells. This provides an
        /// efficient local-neighborhood lookup (useful for collision checks or proximity
        /// queries) whose cost is proportional to the number of circles stored in those cells. </remarks>
        public List<TheCircle> GetNearby(TheCircle circle)
        {
            List<TheCircle> nearby = new List<TheCircle>();
            int cellX = (int)MathF.Floor(circle.X / cellSize);
            int cellY = (int)MathF.Floor(circle.Y / cellSize);

            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    Tuple<int, int> key = new Tuple<int, int>(cellX + i, cellY + j);
                    if (cells.TryGetValue(key, out List<TheCircle> cellCircles))
                    {
                        nearby.AddRange(cellCircles);
                    }
                }
            }
            return nearby;
        }
    }
}
