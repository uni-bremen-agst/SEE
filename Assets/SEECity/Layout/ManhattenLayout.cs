using SEE.DataModel;
using System;
using System.Collections.Generic;
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
                               float edgeWidth)
            : base(showEdges, widthMetric, heightMetric, breadthMetric, issueMap, blockFactory, scaler, edgeWidth)
        {
            name = "Manhattan";
        }

        // The minimal length of any axis (width, breadth, height) of a block.
        // Must not exceed 1.0f.
        protected const float minimalLength = 0.1f;

        // precondition: the GameObjects and their meshes have already been created for all nodes
        protected override void DrawNodes(Graph graph)
        {
            int numberOfBuildingsPerRow = (int)Mathf.Sqrt(graph.NodeCount);
            int column = 0;
            const float distanceBetweenBuildings = 1.0f;
            float maxZ = 0.0f;  // maximal depth of a building in a row
            float positionX = 0.0f;
            float positionZ = 0.0f;

            foreach (Node node in graph.Nodes())
            { 
                if (node.IsLeaf())
                {
                    // We only draw leaves.

                    GameObject block = blockFactory.NewBlock();
                    block.name = node.LinkName;
                    gameObjects[node] = block;

                    column++;
                    if (column > numberOfBuildingsPerRow)
                    {
                        // exceeded length of the square => start a new row
                        column = 1;
                        positionZ += maxZ + distanceBetweenBuildings;
                        maxZ = 0.0f;
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
                    positionX += size.x / 2.0f;
                    // The position is the center of a GameObject. We want all GameObjects
                    // be placed at the same ground level 0. That is why we need to "lift"
                    // every building by half of its height.
                    block.transform.position = new Vector3(positionX, scale.y / 2.0f, positionZ);
                    positionX += size.x / 2.0f + distanceBetweenBuildings;
                }
            }
        }

        // orientation of the edges; 
        // if -1, the edges are drawn below the houses;
        // if 1, the edges are drawn above the houses;
        // use either -1 or 1
        private const float orientation = -1f;

        /// <summary>
        /// Creates the GameObjects representing the edges of the graph.
        /// The graph must have been loaded before via Load().
        /// </summary>
        protected override void DrawEdges(Graph graph)
        {
            // The distance of the edges relative to the houses; the maximal height of
            // a house is 1.0. This offset is used to draw the line somewhat below
            // or above the house (depending on the orientation).
            const float maxHeight = 1f;
            const float offset = maxHeight * 0.1f; // must be positive

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

                    GameObject sourceObject = gameObjects[source];
                    GameObject targetObject = gameObjects[target];

                    {
                        // use sharedMaterial if changes to the original material should affect all
                        // objects using this material; renderer.material instead will create a copy
                        // of the material and will not be affected by changes of the original material
                        line.sharedMaterial = newMat;

                        LineFactory.SetDefaults(line);

                        // If enabled, the lines are defined in world space.
                        // This means the object's position is ignored, and the lines are rendered around 
                        // world origin.
                        line.useWorldSpace = true;

                        // define the points along the line
                        Vector3 sourceCenterToBorder = blockFactory.GetSize(sourceObject) / 2.0f;
                        Vector3 targetCenterToBorder = blockFactory.GetSize(targetObject) / 2.0f;
                        line.positionCount = 4; // number of vertices
                        Vector3[] points = new Vector3[line.positionCount];

                        // starting position
                        points[0] = sourceObject.transform.position; // center of source node
                        points[0].y += orientation * sourceCenterToBorder.y; // floor/ceiling

                        // position below/above starting position
                        points[1] = points[0];
                        points[1].y += orientation * offset;

                        // ending position
                        points[3] = targetObject.transform.position; // center of target node
                        points[3].y += orientation * targetCenterToBorder.y; // floor/ceiling

                        // position below/above ending position
                        points[2] = points[3];
                        points[2].y += orientation * offset;

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
        }
    }
}