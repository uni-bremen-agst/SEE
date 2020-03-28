using SEE.DataModel;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SEE.Layout
{
    /// <summary>
    /// Calculates a simple grid layout for leaf nodes (only). The order is
    /// alphabetic with respect to the Linkname of the nodes. 
    /// </summary>
    public class ManhattanLayout : FlatNodeLayout
    {
        public ManhattanLayout(float groundLevel, float Unit)
            : base(groundLevel)
        {
            name = "Manhattan";
            this.Unit = Unit;
        }

        private readonly float Unit;

        public override Dictionary<ILayoutNode, NodeTransform> Layout(ICollection<ILayoutNode> gameNodes)
        {
            Dictionary<ILayoutNode, NodeTransform> result = new Dictionary<ILayoutNode, NodeTransform>();

            // Simple grid layout with the same number of blocks in each row (roughly).
            int numberOfBuildingsPerRow = (int)Mathf.Sqrt(gameNodes.Count);
            int column = 0;
            int row = 1;
            float distanceBetweenBuildings = Unit * 3.0f;
            float maxZ = 0.0f;         // maximal depth of a building in a row
            float positionX = 0.0f;    // co-ordinate in a column of the grid
            float positionZ = 0.0f;    // co-ordinate in a row of the grid
            // Note: (position.X, position.Y) is the left lower corner of the game object in the X,Z plane

            // Draw all nodes in a grid in ascending alphabetic order of their linkname.
            foreach (ILayoutNode gameNode in gameNodes.OrderBy<ILayoutNode, string>(gameObject => gameObject.LinkName))
            {
                column++;
                if (column > numberOfBuildingsPerRow)
                {
                    // exceeded length of the square => start a new row
                    row++;
                    column = 1;
                    positionZ += maxZ + distanceBetweenBuildings;
                    maxZ = 0.0f;
                    positionX = 0.0f;
                }
                Vector3 size = gameNode.Scale;
                if (size.z > maxZ)
                {
                    maxZ = size.z;
                }

                // center position of the block to be placed
                positionX += size.x / 2.0f;
                // The x,z position in a NodeTransform is the center of a GameObject, whereas 
                // (position.X, position.Y) is the left lower corner of the game object in the X,Z plane.
                // We want all GameObjects be placed at the same ground level 0. 
                // We maintain the original scaleof the gameNode.
                result[gameNode] = new NodeTransform(new Vector3(positionX, groundLevel, positionZ + size.z / 2.0f), size);
                // right border position of the block to be placed + space in between buildings
                positionX += size.x / 2.0f + distanceBetweenBuildings;
            }
            return result;
        }

        public override Dictionary<ILayoutNode, NodeTransform> Layout(ICollection<ILayoutNode> layoutNodes, ICollection<Edge> edges, ICollection<SublayoutNode> sublayouts)
        {
            throw new System.NotImplementedException();
        }

        public override bool UsesEdgesAndSublayoutNodes()
        {
            return false; 
        }
    }
}