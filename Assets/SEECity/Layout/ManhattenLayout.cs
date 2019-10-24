using SEE.DataModel;
using UnityEngine;

namespace SEE.Layout
{
    public class ManhattenLayout : INodeLayout
    {
        public ManhattenLayout(string widthMetric, string heightMetric, string breadthMetric, 
                               SerializableDictionary<string, IconFactory.Erosion> issueMap,
                               BlockFactory blockFactory,
                               IScale scaler,
                               bool showErosions)
            : base(widthMetric, heightMetric, breadthMetric, issueMap, blockFactory, scaler, showErosions)
        {
            name = "Manhattan";
        }

        // The minimal length of any axis (width, breadth, height) of a block.
        // Must not exceed 1.0f.
        protected const float minimalLength = 0.1f;

        // The maximal height of all blocks. If edges are drawn above the blocks, they
        // will be drawn somewhat above this value relative to the blocks ground.
        private float maxBlockHeight = 0.0f;

        // precondition: the GameObjects and their meshes have already been created for all nodes
        public override void Draw(Graph graph)
        {
            int numberOfBuildingsPerRow = (int)Mathf.Sqrt(graph.NodeCount);
            int column = 0;
            int row = 1;
            const float distanceBetweenBuildings = 1.0f;
            float minX = 0.0f;         // minimal x co-ordinate of a block
            float maxZ = 0.0f;         // maximal depth of a building in a row
            float maxZinFirstRow = 0.0f; // the value of maxZ in the first row
            float positionX = 0.0f;    // co-ordinate in a column of the grid
            float positionZ = 0.0f;    // co-ordinate in a row of the grid
            float maxPositionX = 0.0f; // maximal value of any positionX

            // Draw all nodes on a grid. position
            foreach (Node node in graph.Nodes())
            { 
                if (node.IsLeaf())
                {
                    // We only draw leaves.

                    GameObject block = blockFactory.NewBlock();
                    block.name = node.LinkName;
                    gameNodes[node] = block;
                    AttachNode(block, node);

                    column++;
                    if (column > numberOfBuildingsPerRow)
                    {
                        // exceeded length of the square => start a new row
                        if (row == 1)
                        {
                            // we are about to start the first column in the second row;
                            // thus, we have seen a blocks in the first row and can set
                            // maxZinFirstRow accordingly
                            maxZinFirstRow = maxZ;
                        }
                        row++;
                        column = 1;
                        positionZ += maxZ + distanceBetweenBuildings;
                        maxZ = 0.0f;
                        if (positionX > maxPositionX)
                        {
                            maxPositionX = positionX;
                        }
                        positionX = 0.0f;
                    }
                    // Scaled metric values for the dimensions.
                    Vector3 scale = new Vector3(scaler.GetNormalizedValue(node, widthMetric),
                                                scaler.GetNormalizedValue(node, heightMetric),
                                                scaler.GetNormalizedValue(node, breadthMetric));

                    // Scale according to the metrics.
                    blockFactory.ScaleBlock(block, scale);

                    // size is independent of the sceneNode
                    Vector3 size = blockFactory.GetSize(block);
                    if (size.z > maxZ)
                    {
                        maxZ = size.z;
                    }
                    if (size.y > maxBlockHeight)
                    {
                        maxBlockHeight = size.y;
                    }
                    
                    positionX += size.x / 2.0f;
                    // The position is the center of a GameObject. We want all GameObjects
                    // be placed at the same ground level 0. 
                    blockFactory.SetPosition(block, new Vector3(positionX, groundLevel, positionZ));

                    positionX += size.x / 2.0f + distanceBetweenBuildings;

                    if (showErosions)
                    {
                        AddErosionIssues(node);
                    }
                }
            }
            // positionZ is the last row in which a block was added
            PlaneFactory.NewPlane(0.0f, -maxZinFirstRow / 2.0f, maxPositionX - distanceBetweenBuildings, positionZ + maxZ / 2.0f,
                                  groundLevel, Color.gray);
        }
    }
}