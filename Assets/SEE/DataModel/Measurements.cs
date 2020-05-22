using SEE.DataModel;
using SEE.Game;
using SEE.Layout;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http.Headers;
using UnityEngine;

namespace SEE
{

    public class EdgesMeasurements {

        /// <summary>
        /// The maximum length of an edge
        /// </summary>
        public readonly float lengthMax;

        /// <summary>
        /// The minimum length of an edge
        /// </summary>
        public readonly float lengthMin;

        /// <summary>
        /// The total length of all edges
        /// </summary>
        public readonly float lengthTotal;

        /// <summary>
        /// The maximum length of an edge in relation to the area (height + width / 2)
        /// </summary>
        public readonly float lengthMaxArea;

        /// <summary>
        /// The minimum length of an edge in relation to the area (height + width / 2)
        /// </summary>
        public readonly float lengthMinArea;

        /// <summary>
        /// The total length of all edges in relation to the area (height + width / 2)
        /// </summary>
        public readonly float lengthTotalArea;

        /// <summary>
        /// The average length of an edge
        /// </summary>
        public readonly float lengthAverage;

        /// <summary>
        /// The average length of an edge in relation to the area (height + width / 2)
        /// </summary>
        public readonly float lengthAverageArea;

        /// <summary>
        /// The variance length of any edge
        /// </summary>
        public readonly float lengthVariance;

        /// <summary>
        /// The standart deviation of any edge length
        /// </summary>
        public readonly float lengthStandardDeviation;

        /// <summary>
        /// The variance length of any edge in relation to the area (height + width / 2)
        /// </summary>
        public readonly float lengthVarianceArea;

        /// <summary>
        /// The standart deviation of any edge length in relation to the area (height + width / 2)
        /// </summary>
        public readonly float lengthStandardDeviationArea;

        /// <summary>
        /// class holding all measurements value of the edges
        /// </summary>
        /// <param name="lengthMax"></param>
        /// <param name="lengthMin"></param>
        /// <param name="lengthTotal"></param>
        /// <param name="lengthMaxArea"></param>
        /// <param name="lengthMinArea"></param>
        /// <param name="lengthTotalArea"></param>
        /// <param name="lengthAverage"></param>
        /// <param name="lengthVariance"></param>
        /// <param name="lengthStandardDeviation"></param>
        public EdgesMeasurements(float lengthMax, float lengthMin, float lengthTotal, float lengthMaxArea, float lengthMinArea, float lengthTotalArea, float lengthAverage, float lengthAverageArea, float lengthVariance, float lengthStandardDeviation, float lengthVarianceArea, float lengthStandardDeviationArea)
        {
            this.lengthMax = lengthMax;
            this.lengthMin = lengthMin;
            this.lengthTotal = lengthTotal;
            this.lengthMaxArea = lengthMaxArea;
            this.lengthMinArea = lengthMinArea;
            this.lengthTotalArea = lengthTotalArea;
            this.lengthAverage = lengthAverage;
            this.lengthAverageArea = lengthAverageArea;
            this.lengthVariance = lengthVariance;
            this.lengthStandardDeviation = lengthStandardDeviation;
            this.lengthVarianceArea = lengthVarianceArea;
            this.lengthStandardDeviationArea = lengthStandardDeviationArea;
        }
    }

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
        /// width of this graph visualisation
        /// </summary>
        private readonly float width = 0.0f;

        /// <summary>
        /// height of this graph visualisation
        /// </summary>
        private readonly float height = 0.0f;

        /// <summary>
        /// Dictonary with nodes and corresonding gameobjects
        /// </summary>
        protected ICollection<ILayoutNode> layoutNodes;

        public EdgesMeasurements EdgesMeasurements
        {
            get => MeasurementsEdges();
        }

        public int OverlappingGameNodes 
        {
            get => CalcOverlappingGameNodes();
        }

        public float Area
        {
            get => MeasurementsArea();
        }

        public int EdgeCrossings
        {
            get => MeasureEdgeCrossing();
        }

        public string NodesPerformance
        {
            get => GetNodesPerformance();
        }

        public SortedDictionary<string, string> measurementsDict = new SortedDictionary<string, string>();

        private Performance nodePerformance = null;

        /// <summary>
        /// the constructor
        /// </summary>
        /// <param name="nodeMap">Dictonary with nodes and corresonding gameobjects</param>
        /// <param name="graph"> the graph displayed by the layout</param>
        /// <param name="leftFrontCorner">2D co-ordinate of the left front corner</param>
        /// <param name="rightBackCorner">2D co-ordinate of the right back corner</param>
        public Measurements(ICollection<ILayoutNode> layoutNodes, Graph graph, Vector2 leftFrontCorner, Vector2 rightBackCorner, Performance performance = null)
        {
            this.graph = graph;
            this.width = Distance(leftFrontCorner.x, rightBackCorner.x);
            this.height = Distance(leftFrontCorner.y, rightBackCorner.y);
            this.layoutNodes = new List<ILayoutNode>(layoutNodes);
            this.edges = graph.ConnectingEdges(layoutNodes);
            this.nodePerformance = performance;
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="layoutNodes"></param>
        /// <param name="graph"></param>
        public Measurements(ICollection<ILayoutNode> layoutNodes, List<Edge> edges)
        {
            this.layoutNodes = new List<ILayoutNode>(layoutNodes);
            this.edges = edges; 
        }

        /// <summary>
        /// Adds the time needed for calculating the node layout to the measurements
        /// </summary>
        /// <param name="performance"></param>
        public string GetNodesPerformance()
        {
            if (nodePerformance != null )
            {
                return nodePerformance.GetElapsedTime();
            }
            return "";
        }

        public SortedDictionary<string, string> ToStringDictionary()
        {
            if (measurementsDict.Count > 0)
            {
                return measurementsDict;
            }

            SortedDictionary<string, string> measurements = new SortedDictionary<string, string>
            {
                { "Area", Math.Round (Area, 2).ToString() },
                { "Nodes overlapping", OverlappingGameNodes.ToString() },
                { "Number of edge crossings", EdgeCrossings.ToString() },
                { "Straight Edge length max", Math.Round(EdgesMeasurements.lengthMax, 2).ToString() },
                { "Straight Edge length min", Math.Round(EdgesMeasurements.lengthMin, 2).ToString() },
                { "Straight Edge length total", Math.Round(EdgesMeasurements.lengthTotal, 2).ToString() },
                { "Straight Edge length max (area)", Math.Round(EdgesMeasurements.lengthMaxArea, 2).ToString() },
                { "Straight Edge length min (area)", Math.Round(EdgesMeasurements.lengthMinArea, 2).ToString() },
                { "Straight Edge length total (area)", Math.Round(EdgesMeasurements.lengthTotalArea, 2).ToString() },
                { "Straight Edge length average", Math.Round(EdgesMeasurements.lengthAverage, 2).ToString() },
                { "Straight Edge length average (area)", Math.Round(EdgesMeasurements.lengthAverageArea, 2).ToString() },
                { "Straight Edge length variance", Math.Round(EdgesMeasurements.lengthVariance, 2).ToString() },
                { "Straight Edge length standard deviation", Math.Round(EdgesMeasurements.lengthStandardDeviation, 2).ToString() },
                { "Straight Edge length variance (area)", Math.Round(EdgesMeasurements.lengthVarianceArea, 2).ToString() },
                { "Straight Edge length standard deviation (area)", Math.Round(EdgesMeasurements.lengthStandardDeviationArea, 2).ToString() },
            };

            string nodePerformance = NodesPerformance;
            if (nodePerformance.Length > 0)
            {
                measurements.Add("Time for node layout", nodePerformance);
            }

            measurementsDict = measurements;
            return measurementsDict;
        }

        /// <summary>
        /// Measures the number of edge crossings
        /// </summary>
        private int MeasureEdgeCrossing()
        {
            List<Edge> edgesToIterate = new List<Edge>();
            edgesToIterate.AddRange(edges);
            int totalCrossings = 0;

            foreach (Edge edge in edges)
            {
                edgesToIterate.Remove(edge);

                foreach (Edge edge2 in edgesToIterate)
                {

                    Vector3 sourcePosition = FilterListForGameNode(edge.Source.ID).CenterPosition;
                    Vector3 targetPosition = FilterListForGameNode(edge.Target.ID).CenterPosition;
                    Vector3 sourcePosition2 = FilterListForGameNode(edge2.Source.ID).CenterPosition;
                    Vector3 targetPosition2 = FilterListForGameNode(edge2.Target.ID).CenterPosition;
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
            return totalCrossings;
        }

        private ILayoutNode FilterListForGameNode(String ID)
        {
            foreach (ILayoutNode gameNode in layoutNodes)
            {
                if (gameNode.ID == ID)
                {
                    return gameNode;
                }
            }
            return null;
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
        private EdgesMeasurements MeasurementsEdges()
        {
            float minDistEdge = 0;
            float maxDistEdge = 0;
            float totalDist = 0;
            float relationMinDistEdge;
            float relationMaxDistEdge;
            float relationTotalDist;
            float relationAvgDist; 
            float averageDistEdge = 0;
            float varianceDistEdge = 0;
            float relativeVarianceDistEdge = 0;
            float standardDeviation;
            float relativeStandardDeviation;

            if (edges.Count > 0)
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

            var areaLength = (height + width) / 2;
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


            relationAvgDist = averageDistEdge / areaLength;
            if (edges.Count > 0)
            {
                float varianceRelative = 0f;

                foreach (Edge edge in edges)
                {
                    var edgeDistRelative = edge.dist / areaLength;
                    varianceRelative += (edgeDistRelative - relationAvgDist) * (edgeDistRelative - relationAvgDist);
                }
                relativeVarianceDistEdge = varianceRelative / (edges.Count - 1);
            }
            relativeStandardDeviation = Mathf.Sqrt(relativeVarianceDistEdge);

            EdgesMeasurements edgesMeasurements = new EdgesMeasurements(lengthMax: maxDistEdge, 
                                                                        lengthMin: minDistEdge, 
                                                                        lengthTotal:  totalDist,
                                                                        lengthMaxArea: relationMaxDistEdge,
                                                                        lengthMinArea: relationMinDistEdge,
                                                                        lengthTotalArea: relationTotalDist,
                                                                        lengthAverage: averageDistEdge,
                                                                        lengthAverageArea: relationAvgDist,
                                                                        lengthVariance: varianceDistEdge,
                                                                        lengthStandardDeviation: standardDeviation, 
                                                                        lengthVarianceArea: relativeVarianceDistEdge, 
                                                                        lengthStandardDeviationArea: relativeStandardDeviation);

            return edgesMeasurements;
        }

        /// <summary>
        /// calculates measurement for the area
        /// </summary>
        private float MeasurementsArea()
        {
            return height * width;
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
        private int CalcOverlappingGameNodes()
        {
            int overlapAmount = 0;

            List<ILayoutNode> nodesToIterate = new List<ILayoutNode>();
            nodesToIterate.AddRange(layoutNodes);

            foreach (ILayoutNode node in layoutNodes)
            {
                nodesToIterate.Remove(node);

                foreach (ILayoutNode node2 in nodesToIterate)
                {
                    bool doOverlap = CheckOverlapping(node, node2);
                    bool inSameHierarchie = CheckIfInHierarchie(node, node2);
                    if (CheckOverlapping(node, node2) && !CheckIfInHierarchie(node, node2))
                    {
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
        private bool CheckIfInHierarchie(ILayoutNode node1, ILayoutNode node2)
        {
            if (node1.Level == node2.Level || node1.Equals(node2))
            {
                return false;
            }

            ILayoutNode startNode = node1.Level <= node2.Level ? node1 : node2;
            ILayoutNode childNode = node1.Level <= node2.Level ? node2 : node1;

            return CheckIfChild(startNode.Children(), childNode);
        }

        /// <summary>
        /// checks if a node is a children of a list of nodes
        /// </summary>
        /// <param name="children">list of nodes</param>
        /// <param name="node2">the node</param>
        /// <returns></returns>
        private bool CheckIfChild(ICollection<ILayoutNode> children, ILayoutNode node2)
        {
            foreach (ILayoutNode childNode in children)
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
        private bool CheckOverlapping(ILayoutNode node1, ILayoutNode node2)
        {
            Bounds bounds1 = new Bounds(node1.CenterPosition, node1.Scale);
            Bounds bounds2 = new Bounds(node2.CenterPosition, node2.Scale);

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
    }
}