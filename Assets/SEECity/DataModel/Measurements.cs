using SEE.DataModel;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SEE
{
    public class Measurements
    {
        /// <summary>
        /// the graph displayed by the layout
        /// </summary>
        private Graph graph;

        /// <summary>
        /// all gameobjects 
        /// </summary>
        private ICollection<GameObject> nodes;

        /// <summary>
        /// all edges
        /// </summary>
        private IList<Edge> edges;

        /// <summary>
        /// settings for this graph
        /// </summary>
        private GraphSettings settings;

        /// <summary>
        /// width of this graph visualisation
        /// </summary>
        private float width;

        /// <summary>
        /// height of this graph visualisation
        /// </summary>
        private float height;

        /// <summary>
        /// Dictonary with nodes and corresonding gameobjects
        /// </summary>
        protected Dictionary<Node, GameObject> nodeMap;

        /// <summary>
        /// the constructor
        /// </summary>
        /// <param name="nodeMap">Dictonary with nodes and corresonding gameobjects</param>
        /// <param name="graph"> the graph displayed by the layout</param>
        /// <param name="settings">settings for this graph</param>
        /// <param name="leftFrontCorner">2D co-ordinate of the left front corner</param>
        /// <param name="rightBackCorner">2D co-ordinate of the right back corner</param>
        public Measurements(Dictionary<Node, GameObject> nodeMap, Graph graph, GraphSettings settings, Vector2 leftFrontCorner, Vector2 rightBackCorner)
        {
            this.graph = graph;
            this.nodes = nodeMap.Values;
            this.settings = settings;
            this.height = Distance(leftFrontCorner.y, rightBackCorner.y);
            this.width = Distance(leftFrontCorner.x, rightBackCorner.x);
            this.nodeMap = nodeMap;

            settings.Measurements = new SortedDictionary<string, string>();

            edges = graph.ConnectingEdges(nodeMap.Keys);

            MeasurementsArea();
            MeasurementsEdges();
            MeasureEdgeCrossing();

            List<Node> nodes = new List<Node>(nodeMap.Keys);
            List<Node> roots = new List<Node>();
            foreach (Node node in nodes)
            {
                if (node.IsRoot())
                {
                    roots.Add(node);
                }
            }

            settings.Measurements.Add("Nodes overlapping", OverlappingGameNodes(nodes).ToString());
        }

        /// <summary>
        /// Adds the time needed for calculating the node layout to the measurements
        /// </summary>
        /// <param name="performance"></param>
        public void NodesPerformance(Performance performance)
        {
            settings.Measurements.Add("Time for node layout", performance.GetElapsedTime());
        }

        /// <summary>
        /// Measures the number of edge crossings
        /// </summary>
        private void MeasureEdgeCrossing()
        {
            List<Edge> edgesToIterate = new List<Edge>();
            edgesToIterate.AddRange(edges);
            float totalCrossings = 0;

            foreach (Edge edge in edges)
            {
                edgesToIterate.Remove(edge);

                foreach (Edge edge2 in edgesToIterate)
                {
                    Vector3 sourcePosition = nodeMap[edge.Source].transform.position;
                    Vector3 targetPosition = nodeMap[edge.Target].transform.position;
                    Vector3 sourcePosition2 = nodeMap[edge2.Source].transform.position;
                    Vector3 targetPosition2 = nodeMap[edge2.Target].transform.position;
                    bool doIntersect = FasterLineSegmentIntersection(new Vector2(sourcePosition.x, sourcePosition.z),
                                                  new Vector2(targetPosition.x, targetPosition.z),
                                                  new Vector2(sourcePosition2.x, sourcePosition2.z),
                                                  new Vector2(targetPosition2.x, targetPosition2.z), false);
                    if (doIntersect)
                    {
                        totalCrossings++;
                    }
                }
            }
            settings.Measurements.Add("Number of edge crossings", totalCrossings.ToString());
        }

        /// <summary>
        /// calculates whether two lines intersect
        /// Algorithm: https://www.habrador.com/tutorials/math/5-line-line-intersection/
        /// http://inis.jinr.ru/sl/vol1/CMC/Graphics_Gems_3,ed_D.Kirk.pdf
        /// https://forum.unity.com/threads/line-intersection.17384/
        /// </summary>
        /// <param name="l1_p1">2D coordinate of start position of line 1</param>
        /// <param name="l1_p2">2D coordinate of end position of line 1</param>
        /// <param name="l2_p1">2D coordinate of start position of line 2</param>
        /// <param name="l2_p2">2D coordinate of end position of line 2</param>
        /// <param name="shouldIncludeEndPoints"></param>
        /// <returns></returns>
        private static bool FasterLineSegmentIntersection(Vector2 l1_p1, Vector2 l1_p2, Vector2 l2_p1, Vector2 l2_p2, bool shouldIncludeEndPoints)
        {
            //To avoid floating point precision issues we can add a small value

            float epsilon = 0.00001f;

            bool isIntersecting = false;

            float denominator = (l2_p2.y - l2_p1.y) * (l1_p2.x - l1_p1.x) - (l2_p2.x - l2_p1.x) * (l1_p2.y - l1_p1.y);

            //Make sure the denominator is > 0, if not the lines are parallel
            if (denominator != 0f)
            {
                float u_a = ((l2_p2.x - l2_p1.x) * (l1_p1.y - l2_p1.y) - (l2_p2.y - l2_p1.y) * (l1_p1.x - l2_p1.x)) / denominator;
                float u_b = ((l1_p2.x - l1_p1.x) * (l1_p1.y - l2_p1.y) - (l1_p2.y - l1_p1.y) * (l1_p1.x - l2_p1.x)) / denominator;

                //Are the line segments intersecting if the end points are the same
                if (shouldIncludeEndPoints)
                {
                    //Is intersecting if u_a and u_b are between 0 and 1 or exactly 0 or 1
                    if (u_a >= 0f + epsilon && u_a <= 1f - epsilon && u_b >= 0f + epsilon && u_b <= 1f - epsilon)
                    {
                        isIntersecting = true;
                    }
                }
                else
                {
                    //Is intersecting if u_a and u_b are between 0 and 1
                    if (u_a > 0f + epsilon && u_a < 1f - epsilon && u_b > 0f + epsilon && u_b < 1f - epsilon)
                    {
                        isIntersecting = true;
                    }
                }
            }

            return isIntersecting;
        }

        /// <summary>
        /// calculates measurements for edges, e.g. maximal edge length
        /// </summary>
        public void MeasurementsEdges()
        {
            float minDistEdge = 0;
            float maxDistEdge = 0;
            float totalDist = 0;
            float relationMinDistEdge;
            float relationMaxDistEdge;
            float relationTotalDist;
            float averageDistEdge = 0;
            float varianceDistEdge = 0;
            float standardDeviation = 0;

            if (edges.Count >= 0)
            {
                minDistEdge = edges[0].dist;
                maxDistEdge = edges[0].dist;
            }

            foreach (Edge edge in edges)
            {
                totalDist += edge.dist;

                if (edge.dist < minDistEdge)
                {
                    minDistEdge = edge.dist;
                }

                if (edge.dist > maxDistEdge)
                {
                    maxDistEdge = edge.dist;
                }
            }

            var areaLength = height + width;
            relationMaxDistEdge = maxDistEdge / areaLength;
            relationMinDistEdge = minDistEdge / areaLength;
            relationTotalDist = totalDist / areaLength;

            if (edges.Count > 0)
            {
                averageDistEdge = totalDist / edges.Count;
                float variance = 0f;

                foreach (Edge edge in edges)
                {
                    variance += (edge.dist - averageDistEdge) * (edge.dist - averageDistEdge);
                }
                varianceDistEdge = variance / (edges.Count - 1);
            }

            standardDeviation = Mathf.Sqrt(varianceDistEdge);

            settings.Measurements.Add("Straight Edge length max", Math.Round(maxDistEdge, 2).ToString());
            settings.Measurements.Add("Straight Edge length min", Math.Round(minDistEdge, 2).ToString());
            settings.Measurements.Add("Straight Edge length total", Math.Round(totalDist, 2).ToString());

            settings.Measurements.Add("Straight Edge length max (area)", Math.Round(relationMaxDistEdge, 2).ToString());
            settings.Measurements.Add("Straight Edge length min (area)", Math.Round(relationMinDistEdge, 2).ToString());
            settings.Measurements.Add("Straight Edge length total (area)", Math.Round(relationTotalDist, 2).ToString());

            settings.Measurements.Add("Straight Edge length average", Math.Round(averageDistEdge, 2).ToString());
            settings.Measurements.Add("Straight Edge length variance", Math.Round(varianceDistEdge, 2).ToString());
            settings.Measurements.Add("Straight Edge length standard deviation", Math.Round(standardDeviation, 2).ToString());
        }

        /// <summary>
        /// calculates measurement for the area
        /// </summary>
        public void MeasurementsArea()
        {
            float area = height * width;
            settings.Measurements.Add("Area (Plane)", area.ToString());
        }

        /// <summary>
        /// calcuates the distance between 2 points
        /// </summary>
        /// <param name="v0">2d coordinate</param>
        /// <param name="v1">2d coordinate</param>
        /// <returns>the distance</returns>
        private float Distance(float v0, float v1)
        {
            if (v1 <= v0)
            {
                Debug.AssertFormat(v1 > v0, "v1 > v0 expected. Actual v0 = {0}, v1 = {1}.\n", v0, v1);
                throw new Exception("v1 > v0 expected");
            }
            else
            {
                if (v0 < 0.0f)
                {
                    return v1 + Math.Abs(v0);
                }
                else
                {
                    return v1 - v0;
                }
            }
        }

        /// <summary>
        /// Returns the amount of overlapping of nodes
        /// </summary>
        /// <param name="nodes">all nodes</param>
        /// <returns>the overlapping amount</returns>
        private int OverlappingGameNodes(List<Node> nodes)
        {
            int overlapAmount = 0;

            List<Node> nodesToIterate = new List<Node>();
            nodesToIterate.AddRange(nodes);

            foreach (Node node in nodes)
            {
                nodesToIterate.Remove(node);

                foreach (Node node2 in nodesToIterate)
                {
                    bool doOverlap = CheckOverlapping(node, node2);
                    bool inSameHierarchie = CheckIfInHierarchie(node, node2);
                    if (CheckOverlapping(node, node2) && !CheckIfInHierarchie(node, node2))
                    {
                        Debug.Log("node: " + node.SourceName + " node2: " + node2.SourceName);
                        overlapAmount++;
                    }
                }
            }

            return overlapAmount;
        }

        /// <summary>
        /// checks wheather two nodes are in the same inclusion tree branch
        /// </summary>
        /// <param name="node1">first node</param>
        /// <param name="node2">second node</param>
        /// <returns></returns>
        private bool CheckIfInHierarchie(Node node1, Node node2)
        {
            if (node1.Level == node2.Level || node1.Equals(node2))
            {
                return false;
            }

            Node startNode = node1.Level <= node2.Level ? node1 : node2;
            Node childNode = node1.Level <= node2.Level ? node2 : node1;

            return CheckIfChild(startNode.Children(), childNode);
        }

        /// <summary>
        /// checks if a node is a children of a list of nodes
        /// </summary>
        /// <param name="children">list of nodes</param>
        /// <param name="node2">the node</param>
        /// <returns></returns>
        private bool CheckIfChild(List<Node> children, Node node2)
        {
            foreach (Node childNode in children)
            {
                if (childNode.Equals(node2))
                {
                    return true;
                }
                else
                {
                    if (CheckIfChild(childNode.Children(), node2))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// checks if two nodes overlap
        /// </summary>
        /// <param name="node1">the first node</param>
        /// <param name="node2">the second node</param>
        /// <returns></returns>
        private bool CheckOverlapping(Node node1, Node node2)
        {
            if (node1.SourceName == "dir_B" || node1.SourceName == "dir_A" || node1.SourceName == "dir_B_1" || node1.SourceName == "dir_A_1")
            {
                Debug.Log("HI");
            }
            Bounds bounds1 = GetBounds(nodeMap[node1]);
            Bounds bounds2 = GetBounds(nodeMap[node2]);

            bool intersect = IntersectBounds(bounds1, bounds2);

            return intersect;
        }

        /// <summary>
        /// checks if two bounds intersect
        /// </summary>
        /// <param name="bounds1">the first bound</param>
        /// <param name="bounds2">the second bound</param>
        /// <returns></returns>
        private bool IntersectBounds(Bounds bounds1, Bounds bounds2)
        {
            return !(bounds2.min.x > bounds1.max.x ||
                          bounds2.max.x < bounds1.min.x ||
                          bounds2.min.z > bounds1.max.z ||
                          bounds2.max.z < bounds1.min.z);

        }

        /// <summary>
        /// returns the bounds of a gameobject
        /// </summary>
        /// <param name="gameObject">the gameobject</param>
        /// <returns></returns>
        private Bounds GetBounds(GameObject gameObject)
        {
            Renderer renderer = gameObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                Bounds bounds = renderer.bounds;
                bounds.size = bounds.size / 2.0f;
                return bounds;
            }
            else
            {
                Debug.LogErrorFormat("Node {0} (tag: {1}) without renderer.\n", gameObject.name, gameObject.tag);
                return new Bounds();
            }
        }
    }
}