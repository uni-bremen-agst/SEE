using SEE.DataModel;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Layout
{
    /// <summary>
    /// Calculates a simple grid layout for leaf nodes (only). The order is
    /// random. 
    /// </summary>
    public class ManhattanLayout : NodeLayout
    {
        public ManhattanLayout(float groundLevel,
                               NodeFactory blockFactory)
            : base(groundLevel, blockFactory)
        {
            name = "Manhattan";
        }

        public override Dictionary<GameObject, NodeTransform> Layout(ICollection<GameObject> gameNodes)
        {
            Dictionary<GameObject, NodeTransform> result = new Dictionary<GameObject, NodeTransform>();

            // Simple grid layout with the same number of blocks in each row and column (roughly).
            int numberOfBuildingsPerRow = (int)Mathf.Sqrt(gameNodes.Count);
            int column = 0;
            int row = 1;
            const float distanceBetweenBuildings = 1.0f;
            float maxZ = 0.0f;         // maximal depth of a building in a row
            float positionX = 0.0f;    // co-ordinate in a column of the grid
            float positionZ = 0.0f;    // co-ordinate in a row of the grid

            // Draw all nodes on a grid. position
            foreach (var gameNode in gameNodes)
            {
                Node node = gameNode.GetComponent<NodeRef>().node;
                // We only draw leaves.
                if (node.IsLeaf())
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

                    // size is independent of the sceneNode
                    Vector3 size = leafNodeFactory.GetSize(gameNode);
                    if (size.z > maxZ)
                    {
                        maxZ = size.z;
                    }

                    // center position of the block to be placed
                    positionX += size.x / 2.0f;
                    // The position is the center of a GameObject. We want all GameObjects
                    // be placed at the same ground level 0.
                    result[gameNode] = new NodeTransform(new Vector3(positionX, groundLevel, positionZ), Vector3.one);
                    // right border position of the block to be placed + space in between buildings
                    positionX += size.x / 2.0f + distanceBetweenBuildings;
                }
            }
            return result;
        }
    }
}