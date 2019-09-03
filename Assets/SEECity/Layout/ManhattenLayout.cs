using SEE.DataModel;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Layout
{
    public class ManhattenLayout : ILayout
    {
        public ManhattenLayout(string widthMetric, string heightMetric, string breadthMetric)
            : base(widthMetric, heightMetric, breadthMetric)
        {
            name = "Manhattan";
        }

        public override void Draw(Graph graph)
        {
            AddMeshes(graph);
            base.Draw(graph);
        }

        private void AddMeshes(Graph graph)
        {
            foreach (GameObject node in graph.GetNodes())
            {
                MeshFactory.AddCube(node);
            }
        }

        // The minimal length of any axis (width, breadth, height) of a block.
        // Must not exceed 1.0f.
        protected const float minimalLength = 0.1f;

        // precondition: the GameObjects and their meshes have already been created for all nodes
        protected override void DrawNodes(Graph graph, Dictionary<string, float> metricMaxima)
        {
            //DumpMetricMaxima(metricMaxima);

            int length = (int)Mathf.Sqrt(graph.NodeCount);
            float column = 0f;
            float row = 1f;
            const float relativeOffset = 0.1f;

            //Vector3 maxSize = Vector3.zero;
            foreach (GameObject sceneNode in graph.GetNodes())
            {
                column++;
                if (column > length)
                {
                    // exceeded length of the square => start a new row
                    column = 1f;
                    row++;
                }
                Node node = sceneNode.GetComponent<Node>();
                if (node == null)
                {
                    Debug.LogError("Scene node " + sceneNode.name + " does not have a graph node component.\n");
                }
                Vector3 scale = ScaleNode(node, metricMaxima, minimalLength, 1.0f);
                node.gameObject.transform.localScale = scale;

                // The position is the center of a GameObject. We want all GameObjects
                // be placed at the same ground level 0. That is why we need to "lift"
                // every building by half of its height.
                sceneNode.transform.position = new Vector3(row + row * relativeOffset, scale.y / 2.0f, column + column * relativeOffset);
                /*
                {
                    Renderer renderer;
                    //Fetch the GameObject's Renderer component
                    renderer = house.GetComponent<Renderer>();
                    //Change the GameObject's Material Color to red
                    //m_ObjectRenderer.material.color = Color.red;
                    Debug.Log("house size: " + renderer.bounds.size + "\n");
                }
                */
                //Vector3 size = GetSize(sceneNode);
                //if (size.x > maxSize.x) maxSize.x = size.x;
                //if (size.y > maxSize.y) maxSize.y = size.y;
                //if (size.z > maxSize.z) maxSize.z = size.z;
            }
            //Debug.Log("Maxima: " + maxSize + "\n");
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

            foreach (GameObject gameEdge in graph.GetEdges())
            {
                Edge edge = gameEdge.GetComponent<Edge>();

                if (edge != null)
                {
                    Node source = edge.Source;
                    Node target = edge.Target;
                    if (source != null && target != null)
                    {
                        GameObject sourceObject = source.gameObject;
                        GameObject targetObject = target.gameObject;

                        LineRenderer line = gameEdge.GetComponent<LineRenderer>();
                        if (line == null)
                        {
                            // gameEdge does not yet have a renderer; we add a new one
                            line = gameEdge.AddComponent<LineRenderer>();
                        }
                        if (line != null)
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
                            Vector3 sourceCenterToBorder = GetExtent(sourceObject);
                            Vector3 targetCenterToBorder = GetExtent(targetObject);
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
                        else
                        {
                            Debug.LogError("Cannot attach renderer on scene edge " + gameEdge.name + ".\n");
                        }
                    }
                    else
                    {
                        Debug.LogError("Scene edge " + gameEdge.name + " has a missing source or target.\n");
                    }
                }
                else
                {
                    Debug.LogError("Scene edge " + gameEdge.name + " does not have a graph edge component.\n");
                }
            }
        }

        public override void Reset() { /* Does not need to do anything. */}
    }
}