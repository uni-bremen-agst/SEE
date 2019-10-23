using SEE.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SEE.Layout
{
    public class ManhattenLayout : ILayout
    {
        public ManhattenLayout(bool showEdges,
                               string widthMetric, string heightMetric, string breadthMetric, 
                               SerializableDictionary<string, IconFactory.Erosion> issueMap,
                               BlockFactory blockFactory,
                               IScale scaler,
                               float edgeWidth,
                               bool showErosions,
                               bool edgesAboveBlocks)
            : base(showEdges, widthMetric, heightMetric, breadthMetric, issueMap, blockFactory, scaler, edgeWidth, showErosions, edgesAboveBlocks)
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
        protected override void DrawNodes(Graph graph)
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

        /// <summary>
        /// Creates the GameObjects representing the edges of the graph.
        /// The graph must have been loaded before via Load().
        /// </summary>
        protected override void DrawEdges(Graph graph)
        {
            StraightLineLayout layout = new StraightLineLayout(blockFactory, edgeWidth, edgesAboveBlocks);
            layout.DrawEdges(graph, gameNodes.Values.ToList());

            /*
            // The offset of the edges above or below the ground chosen relative 
            // to the height of the largest block.
            // This offset is used to draw the line somewhat below
            // or above the house (depending on the orientation).
            float offset = maxBlockHeight * 0.1f; // must be positive
            // The level at which edges are drawn. This value is used only if the
            // edges are to be drawn above the blocks.
            float edgeLevel = maxBlockHeight + offset;

            //Material newMat = Resources.Load<Material>(materialPath);
            Material newMat = new Material(defaultLineMaterial);
            if (newMat == null)
            {
                Debug.LogError("Could not find material " + materialPath + "\n");
                return;
            }

            foreach (Edge edge in graph.Edges())
            {
                Node source = edge.Source;
                Node target = edge.Target;
                if (source != null && target != null)
                {
                    GameObject gameEdge = new GameObject
                    {
                        tag = Tags.Edge,
                        isStatic = true,
                        name = edge.Type + "(" + source.LinkName + ", " + target.LinkName + ")"
                    };
                    // gameEdge does not yet have a renderer; we add a new one
                    LineRenderer line = gameEdge.AddComponent<LineRenderer>();

                    GameObject sourceObject = gameNodes[source];
                    GameObject targetObject = gameNodes[target];

                    {
                        // use sharedMaterial if changes to the original material should affect all
                        // objects using this material; renderer.material instead will create a copy
                        // of the material and will not be affected by changes of the original material
                        line.sharedMaterial = newMat;

                        LineFactory.SetDefaults(line);
                        LineFactory.SetWidth(line, edgeWidth * blockFactory.Unit());

                        // If enabled, the lines are defined in world space.
                        // This means the object's position is ignored, and the lines are rendered around 
                        // world origin.
                        line.useWorldSpace = true;

                        // define the points along the line
                        Vector3 sourceCenterToBorder = blockFactory.GetSize(sourceObject) / 2.0f;
                        Vector3 targetCenterToBorder = blockFactory.GetSize(targetObject) / 2.0f;
                        line.positionCount = 4; // number of vertices
                        Vector3[] points = new Vector3[line.positionCount];

                        if (edgesAboveBlocks)
                        {
                            points[0] = blockFactory.Roof(sourceObject); 
                            points[3] = blockFactory.Roof(targetObject);

                            points[1] = blockFactory.Ground(sourceObject);
                            points[1].y = edgeLevel;
                            points[2] = blockFactory.Ground(targetObject);
                            points[2].y = edgeLevel;
                        }
                        else
                        {
                            points[0] = blockFactory.Ground(sourceObject);
                            points[3] = blockFactory.Ground(targetObject);

                            // position below/above starting position
                            points[1] = points[0];
                            points[1].y -= offset;

                            // position below/above ending position
                            points[2] = points[3];
                            points[2].y -= offset;
                        }

                        line.SetPositions(points);

                        // put a capsule collider around the straight main line
                        // (the one from points[1] to points[2]
                        CapsuleCollider capsule = gameEdge.AddComponent<CapsuleCollider>();
                        capsule.radius = Math.Max(line.startWidth, line.endWidth) / 2.0f;
                        capsule.center = Vector3.zero;
                        capsule.direction = 2; // Z-axis for easier "LookAt" orientation
                        capsule.transform.position = points[1] + (points[2] - points[1]) / 2;
                        capsule.transform.LookAt(points[1]);
                        capsule.height = (points[2] - points[1]).magnitude;
                    }
                }
                else
                {
                    Debug.LogErrorFormat("Edge of type {0} has a missing source or target.\n", edge.Type);
                }
            }
            */
        }
    }
}