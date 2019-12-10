using System.Collections.Generic;
using UnityEngine;
using SEE.Layout.EvoStreets;

namespace SEE.Layout
{
    public class EvoStreetsNodeLayout : NodeLayout
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="groundLevel">the y co-ordinate setting the ground level; all nodes will be
        /// placed on this level</param>
        /// <param name="leafNodeFactory">the factory used to create leaf nodes</param>
        public EvoStreetsNodeLayout(float groundLevel,
                                      NodeFactory leafNodeFactory, 
                                      IScale scaler, 
                                      GraphSettings graphSettings)
        : base(groundLevel, leafNodeFactory)
        {
            name = "EvoStreets";
            this.graphSettings = graphSettings;
            this.scaler = scaler;
        }

        /// <summary>
        /// The distance between two neighboring leaf-node representations.
        /// </summary>
        public float OffsetBetweenBuildings = 4.5f;

        /// <summary>
        /// The street width that will be adjusted by the "depth" of the street.
        /// </summary>
        public float StreetWidth = 2.0f;

        /// <summary>
        /// The height of the street (y co-ordinate).
        /// </summary>
        public static float StreetHeight = 0.1f;

        /// <summary>
        /// Scaling used for the node metrics.
        /// </summary>
        private readonly IScale scaler;

        private const float CM_TO_M = 1.0f;

        /// <summary>
        /// The set of children of each node. This is a subset of the node's children
        /// in the graph, limited to the children for which a layout is requested.
        /// </summary>
        private Dictionary<DataModel.Node, List<DataModel.Node>> children;

        /// <summary>
        /// The roots of the subtrees of the original graph that are to be laid out.
        /// A node is considered a root if it has either no parent in the original
        /// graph or its parent is not contained in the set of nodes to be laid out.
        /// </summary>
        private List<DataModel.Node> roots;

        /// <summary>
        /// The settings to be considered for the layout.
        /// </summary>
        private readonly GraphSettings graphSettings;

        private ENode rootNode;

        public ENode GetRoot()
        {
            return rootNode;
        }

        /// <summary>
        /// The node layout we compute as a result.
        /// </summary>
        private Dictionary<GameObject, NodeTransform> layout_result;

        public override Dictionary<GameObject, NodeTransform> Layout(ICollection<GameObject> gameNodes)
        {
            layout_result = new Dictionary<GameObject, NodeTransform>();

            if (gameNodes.Count == 0)
            {
                throw new System.Exception("No nodes to be laid out.");
            }
            else if (gameNodes.Count == 1)
            {
                GameObject gameNode = gameNodes.GetEnumerator().Current;
                layout_result[gameNode] = new NodeTransform(Vector3.zero, gameNode.transform.localScale);
            }
            else
            {
                to_game_node = NodeMapping(gameNodes);
                CreateTree(to_game_node.Keys, out roots, out children);
                if (roots.Count == 0)
                {
                    throw new System.Exception("Graph has no root node.");
                }
                else if (roots.Count > 1)
                {
                    throw new System.Exception("Graph has multiple roots.");
                }
                rootNode = GenerateHierarchy(roots[0]);
                maximalDepth = MaxDepth(roots[0], children);
                GenerateNode(rootNode);
                CalculationNodeLocation(rootNode, Vector3.zero);
                SwapZWithY(rootNode);
            }
            return layout_result;
        }

        private ENode GenerateHierarchy(DataModel.Node root)
        {
            ENode result = new ENode(root)
            {
                Depth = 0
            };
            foreach (DataModel.Node child in children[root])
            {
                ENode kid = GenerateHierarchy(result, child);
                result.Children.Add(kid);
            }
            return result;
        }

        private ENode GenerateHierarchy(ENode parent, DataModel.Node node)
        {
            ENode result = new ENode(node)
            {
                Depth = parent.Depth + 1,
                ParentNode = parent
            };
            foreach (DataModel.Node child in children[node])
            {
                ENode kid = GenerateHierarchy(result, child);
                result.Children.Add(kid);
            }
            return result;
        }

        private void CalculationNodeLocation(ENode node, Vector3 newLoc)
        {
            if (node == null)
            {
                Debug.Log("InParentNode = Nullptr in USCOOPCityGeneratorComponent::CalculationNodeLocation\n");
                return;
            }

            //FRotator rot = FRotator(0, node.RotationZ, 0);

            float nextX;
            float nextY;

            Vector2 fromPivot = new Vector2(node.Scale.x / 2, node.Scale.y / 2) * CM_TO_M;
            Vector2 rotatedfromPivot = fromPivot.GetRotated(node.Rotation);
            Vector2 toPivot = rotatedfromPivot;
            Vector3 toGoal = new Vector3(toPivot.x, toPivot.y, (node.Scale.z / 2.0f) * CM_TO_M);

            if (node.IsHouse())
            {
                node.Location = newLoc * CM_TO_M + toGoal;
            }
            else
            {
                // street
                //surrounding plane
                //~~~~// spawnBox(ISMPlane, (InNewLoc + Vector3(0.f, 0.f, -MAX_LEVELS + InParentNode.Depth)) * CM_TO_M + toGoal, InParentNode.RotationZ, Vector3(InParentNode.Scale.X, InParentNode.Scale.Y, 0.2));

                //the street
                Vector2 StreetfromPivo = new Vector2(node.Scale.x / 2, node.ZPivot) * CM_TO_M;
                Vector2 StreetRotatedfromPivo = StreetfromPivo.GetRotated(node.Rotation);
                float relStreetWidth = RelativeStreetWidth(node);
                Vector3 StreetToGoal = new Vector3(StreetRotatedfromPivo.x, StreetRotatedfromPivo.y, (StreetHeight / 2.0f) * CM_TO_M);
                //~~~~// spawnBox(ISMStreet, InNewLoc*CM_TO_M + StreetToGoal, InParentNode.RotationZ, Vector3(InParentNode.Scale.X, relStreetWidth, StreetHeight));

                node.Location = newLoc * CM_TO_M + StreetToGoal;
                node.Scale = new Vector3(node.Scale.x, relStreetWidth, node.Scale.z);

                for (int i = 0; i < node.Children.Count; i++)
                {
                    float streetMod = (node.Children[i].Left) ? -relStreetWidth / 2 : +relStreetWidth / 2;
                    Vector2 relChild = new Vector2(node.Children[i].XPivot, 0.0f);
                    relChild = relChild.GetRotated(node.Rotation);
                    Vector2 relMy = new Vector2(0.0f, node.ZPivot + streetMod);
                    relMy = relMy.GetRotated(node.Rotation);

                    nextX = newLoc.x + relChild.x + relMy.x;
                    nextY = newLoc.y + relChild.y + relMy.y;

                    Vector3 nextLoc = new Vector3(nextX, nextY, 0);
                    CalculationNodeLocation(node.Children[i], nextLoc);
                }
            }
        }

        private void GenerateNode(ENode node)
        {
            if (node == null)
            {
                Debug.Log("Node ist null in EvostreetCityGenerator::GenerateNode\n");
                return;
            }

            if (node.GraphNode.IsLeaf())
            {
                CalcScale(node);
            }
            else
            {
                // street
                float leftPivotX = OffsetBetweenBuildings;
                float RightPivotX = OffsetBetweenBuildings;
                ENode newChildNode;
                for (int i = 0; i < node.Children.Count; i++)
                {
                    newChildNode = (node.Children[i]);
                    newChildNode.Rotation =
                        (leftPivotX <= RightPivotX) ? node.Rotation - 90.0f : node.Rotation + 90.0f; // could be a street
                    newChildNode.Rotation = (Mathf.FloorToInt(newChildNode.Rotation) + 360) % 360;
                    GenerateNode(newChildNode);
                    // Pivot setting
                    if (leftPivotX <= RightPivotX)
                    {
                        // left
                        newChildNode.Left = true; // is default value
                        if (newChildNode.GraphNode.IsLeaf())
                        {
                            // house
                            leftPivotX += newChildNode.Scale.x;
                            newChildNode.XPivot = leftPivotX;
                            leftPivotX += OffsetBetweenBuildings;
                        }
                        else 
                        {   // street
                            newChildNode.XPivot = leftPivotX;
                            leftPivotX += newChildNode.Scale.y;
                            leftPivotX += OffsetBetweenBuildings;
                        }
                    }
                    else
                    {
                        // right
                        newChildNode.Left = false;
                        if (newChildNode.GraphNode.IsLeaf())
                        {
                            // house
                            newChildNode.XPivot = RightPivotX;
                            RightPivotX += newChildNode.Scale.x;
                            RightPivotX += OffsetBetweenBuildings;
                        }
                        else
                        {
                            // street
                            RightPivotX += newChildNode.Scale.y;
                            newChildNode.XPivot = RightPivotX;
                            RightPivotX += OffsetBetweenBuildings;
                        }
                    }

                    if (newChildNode.GraphNode.IsLeaf())
                    {   // house
                        newChildNode.Rotation =
                            (newChildNode.Left) ? node.Rotation - 180.0f : node.Rotation; //is not a street
                        newChildNode.Rotation = (Mathf.FloorToInt(newChildNode.Rotation) + 360) % 360;
                    }
                }
                //for InParentNode is a street calculate its size

                node.Scale = new Vector3(MaxXOfChildren(node, OffsetBetweenBuildings),
                                        MaxYOfChildNodes(node, OffsetBetweenBuildings), 
                                        node.MaxChildZ);
                node.ZPivot = MaxLeftY(node, OffsetBetweenBuildings);
            }
        }

        private void CalcScale(ENode node)
        {
            // Scaled metric values for the dimensions.
            // FIXME: Currently, y and z axes are swapped (verbatim Unreal -> Unity migration) that is why
            // we also need the HeightMetric and DepthMetric metric. We need to revert this swapping
            // as soon as we have adjusted the code here to Unity's co-ordinate system.
            node.Scale = new Vector3(scaler.GetNormalizedValue(graphSettings.WidthMetric,  node.GraphNode),
                                     scaler.GetNormalizedValue(graphSettings.DepthMetric,  node.GraphNode),
                                     scaler.GetNormalizedValue(graphSettings.HeightMetric, node.GraphNode));
        }

        private float MaxLeftY(ENode node, float offset)
        {
            float sum = 0.0f;
            for (int i = 0; i < node.Children.Count; i++)
            {
                //Left children only
                if (node.Children[i].Left)
                {
                    if (node.Children[i].IsHouse())
                    {
                        if (node.Children[i].Scale.y > sum)
                        {
                            sum = node.Children[i].Scale.y;
                        }
                    }
                    else if (node.Children[i].IsStreet())
                    {
                        if (node.Children[i].Scale.x > sum)
                        {
                            sum = node.Children[i].Scale.x;
                        }
                    }
                }
            }
            return sum;
        }

        private float MaxYOfChildNodes(ENode node, float offset)
        {
            float left = 0.0f;
            float right = 0.0f;

            for (int i = 0; i < node.Children.Count; i++)
            {
                if (node.Children[i].Left)
                {
                    if (node.Children[i].IsHouse())
                    {
                        if (node.Children[i].Scale.y > left)
                        {
                            left = node.Children[i].Scale.y;
                        }
                    }
                    else
                    {
                        if (node.Children[i].Scale.x > left)
                        {
                            left = node.Children[i].Scale.x;
                        }
                    }
                }
                else
                {
                    if (node.Children[i].IsHouse())
                    {
                        if (node.Children[i].Scale.y > right) right = node.Children[i].Scale.y;
                    }
                    else
                    {
                        if (node.Children[i].Scale.x > right) right = node.Children[i].Scale.x;
                    }
                }
            }
            return left + right + RelativeStreetWidth(node);
        }

        private float MaxXOfChildren(ENode node, float offset)
        {
            float left = SumXOfChildren(node, offset, true);
            float right = SumXOfChildren(node, offset, false);
            return left < right ? right : left;
        }

        private float SumXOfChildren(ENode node, float offset, bool left)
        {
            float sum = offset;

            for (int i = 0; i < node.Children.Count; i++)
            {
                if (node.Children[i].Left == left)
                {
                    if (node.Children[i].IsHouse())
                    {
                        sum += node.Children[i].Scale.x + offset;
                    }
                    else if (node.Children[i].IsStreet())
                    {
                        sum += node.Children[i].Scale.y + offset;
                    }
                }
            }
            return sum + RelativeStreetWidth(node);
        }

        private int maximalDepth;

        private float RelativeStreetWidth(ENode node)
        {
            return StreetWidth * ((maximalDepth + 1) - node.Depth) / (maximalDepth + 1);
        }

        /// <summary>
        /// Swaps z and y co-ordinate for given node and all its descendants.
        /// This fixes the fact that height in unity is the y component of a 
        /// vector while in unreal it's the z component.
        /// </summary>
        /// <param name="node">node whose z and y are to be swapped</param>
        private void SwapZWithY(ENode node)
        {
            // Swap scale
            var origScaleZ = node.Scale.z;
            var origScaleX = node.Scale.x;
            node.Scale.x = node.Scale.y;
            node.Scale.y = origScaleZ;
            node.Scale.z = origScaleX;

            // Swap location
            var origLocationZ = node.Location.z;
            var origLocationX = node.Location.x;
            node.Location.x = node.Location.y;
            node.Location.y = origLocationZ;
            node.Location.z = origLocationX;

            foreach (var child in node.Children)
            {
                SwapZWithY(child);
            }
        }
    }
}