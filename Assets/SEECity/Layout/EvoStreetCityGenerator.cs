using System.Collections.Generic;
using UnityEngine;
using SEE.Layout.EvoStreets;

namespace SEE.Layout
{
    public class EvostreetCityGenerator
    {
        // The set of nodes already visited.
        private Dictionary<string, ENode> checkedNodes = new Dictionary<string, ENode>();

        // The maximal depth of the tree.
        private int MaxDepth;

        public float OffsetBetweenBuildings = 4.5f;

        public float StreetWidth = 3.0f;

        public float StreetHeight = 0.1f;

        public bool bUseRootX = false;

        private IScale scaler;

        public float RootX = 1.3f;

        private const float CM_TO_M = 1.0f;

        private GraphSettings graphSettings;

        public ENode GenerateCity(SEE.DataModel.Graph graph, IScale scaler, GraphSettings graphSettings)
        {
            checkedNodes.Clear();

            this.graphSettings = graphSettings;
            this.scaler = scaler;

            MaxDepth = graph.GetMaxDepth();

            ENode rootNode = DefineRootNode(GenerateHierarchy(graph));

            if (rootNode == null)
            {
                Debug.Log("RootNode ist nullptr nach DefineRootNode: OverForest hat keine Kinder...\n");
                return null;
            }

            GenerateNode(rootNode);
            CalculationNodeLocation(rootNode, Vector3.zero);

            Debug.Log($"Statistics: Generated unique Nodes: {checkedNodes.Count}\n");

            return rootNode;
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
            Vector2 rotatedfromPivot = fromPivot.GetRotated(node.RotationZ);
            Vector2 toPivot = rotatedfromPivot;
            Vector3 toGoal = new Vector3(toPivot.x, toPivot.y, (node.Scale.z / 2.0f) * CM_TO_M);

            if (node.IsHouse())
            {
                //house		
                node.Location = newLoc * CM_TO_M + toGoal;
            }
            else if (node.IsStreet())
            {
                //street
                //surrounding plane
                //~~~~// spawnBox(ISMPlane, (InNewLoc + Vector3(0.f, 0.f, -MAX_LEVELS + InParentNode.Depth)) * CM_TO_M + toGoal, InParentNode.RotationZ, Vector3(InParentNode.Scale.X, InParentNode.Scale.Y, 0.2));

                //the street
                Vector2 StreetfromPivo = new Vector2(node.Scale.x / 2, node.YPivot) * CM_TO_M;
                Vector2 StreetRotatedfromPivo = StreetfromPivo.GetRotated(node.RotationZ);
                float relStreetWidth = relativeStreetWidth(node);
                Vector3 StreetToGoal = new Vector3(StreetRotatedfromPivo.x, StreetRotatedfromPivo.y,
                    (StreetHeight / 2.0f) * CM_TO_M);
                //~~~~// spawnBox(ISMStreet, InNewLoc*CM_TO_M + StreetToGoal, InParentNode.RotationZ, Vector3(InParentNode.Scale.X, relStreetWidth, StreetHeight));

                node.Location = newLoc * CM_TO_M + StreetToGoal;
                node.Scale = new Vector3(node.Scale.x, relStreetWidth, node.Scale.z);

                for (int i = 0; i < node.Children.Count; i++)
                {
                    float streetMod = (node.Children[i].Left) ? -relStreetWidth / 2 : +relStreetWidth / 2;
                    Vector2 relChild = new Vector2(node.Children[i].XPivot, 0.0f);
                    relChild = relChild.GetRotated(node.RotationZ);
                    Vector2 relMy = new Vector2(0.0f, node.YPivot + streetMod);
                    relMy = relMy.GetRotated(node.RotationZ);


                    nextX = newLoc.x + relChild.x + relMy.x;
                    nextY = newLoc.y + relChild.y + relMy.y;


                    Vector3 nextLoc = new Vector3(nextX, nextY, 0);
                    CalculationNodeLocation(node.Children[i], nextLoc);
                }
            }
        }

        private ENode DefineRootNode(ENode overForest)
        {
            if (overForest.Children.Count == 0)
            {
                return null;
            }

            if (overForest.Children.Count == 1)
            {
                return overForest.Children[0];
            }

            return overForest;
        }

        private ENode GenerateHierarchy(SEE.DataModel.Graph graph) //TODO: Create Hierarchy only once per import
        {
            // 1. Find Node with no children of visible type
            // 2. Find most outer node of given edge type (default: Enclosing)
            // 3. Find all nodes of visible type starting from Node found in 2.

            // var checkedNodes = new Dictionary<string, Graph.Node>(); TODO: check if this is not the cleaner solution

            var overForest = new ENode { IsOverForest = true, Depth = 0 };

            // 1.
            foreach (SEE.DataModel.Node graphNode in graph.Nodes())
            {
                if (checkedNodes.ContainsKey(graphNode.LinkName))
                    continue; // if not already Created as a Child Node of some other Node

                if (graphNode.IsRoot())
                {
                    ENode node = GenerateNodeAndChildren(overForest, graphNode);
                    node.ParentNode = overForest; //TODO: redundant? See GenerateNodeAndChildren
                    overForest.Children.Add(node);

                    Debug.Log($"Attached NewChildNode: {graphNode.LinkName} to OverForest\n");
                }
            }

            return overForest;
        }

        private ENode GenerateNodeAndChildren(ENode parentNode, SEE.DataModel.Node graphNode)
        {
            ENode newNode = new ENode();
            newNode.GraphNode = graphNode;
            newNode.ParentNode = parentNode;
            newNode.Depth = parentNode.Depth + 1;

            checkedNodes.Add(newNode.GraphNode.LinkName, newNode); // Add Created ENode to List of CheckedNodesMap worked on

            AddChildren(newNode);

            return newNode;
        }

        private void AddChildren(ENode parentNode)
        {
            foreach (SEE.DataModel.Node child in parentNode.GraphNode.Children())
            {
                ENode newNode = GenerateNodeAndChildren(parentNode, child);
                parentNode.Children.Add(newNode);
            }
        }

        private void GenerateNode(ENode node)
        {
            if (node == null)
            {
                Debug.Log("node ist null in EvostreetCityGenerator::GenerateNode\n");
                return;
            }

            if (! node.GraphNode.IsLeaf()) // street
            {
                float leftPivotX = OffsetBetweenBuildings;
                float RightPivotX = OffsetBetweenBuildings;
                ENode newChildNode;
                for (int i = 0; i < node.Children.Count; i++)
                {
                    newChildNode = (node.Children[i]);
                    newChildNode.RotationZ =
                        (leftPivotX <= RightPivotX) ? node.RotationZ - 90.0f : node.RotationZ + 90.0f; //could be a street
                    newChildNode.RotationZ = (Mathf.FloorToInt(newChildNode.RotationZ) + 360) % 360;
                    GenerateNode(newChildNode);
                    //Pivo setting
                    if (leftPivotX <= RightPivotX)
                    {
                        // left
                        newChildNode.Left = true; //is default value
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
                        newChildNode.RotationZ =
                            (newChildNode.Left) ? node.RotationZ - 180.0f : node.RotationZ; //is not a street
                        newChildNode.RotationZ = (Mathf.FloorToInt(newChildNode.RotationZ) + 360) % 360;
                    }
                }
                //for InParentNode is a street calculate its size

                node.Scale = new Vector3(maxXOfChildren(node, OffsetBetweenBuildings),
                    maxYOfChildNodes(node, OffsetBetweenBuildings), node.MaxChildZ);
                node.YPivot = maxLeftY(node, OffsetBetweenBuildings);
            }
            else if (node.GraphNode.IsLeaf())
            {
                // house
                CalcScale(node);
            }
        }

        private void CalcScale(ENode node)
        {
            // Scaled metric values for the dimensions.
            node.Scale = new Vector3(scaler.GetNormalizedValue(graphSettings.WidthMetric,  node.GraphNode),
                                     scaler.GetNormalizedValue(graphSettings.HeightMetric, node.GraphNode),
                                     scaler.GetNormalizedValue(graphSettings.DepthMetric,  node.GraphNode));
        }

        private float maxLeftY(ENode node, float offset)
        {
            float sum = 0.0f;
            for (int i = 0; i < node.Children.Count; i++)
            {
                //Left children only
                if (node.Children[i].Left)
                {
                    if (node.Children[i].IsHouse())
                    {
                        if (node.Children[i].Scale.y > sum) sum = node.Children[i].Scale.y;
                    }
                    else if (node.Children[i].IsStreet())
                    {
                        if (node.Children[i].Scale.x > sum) sum = node.Children[i].Scale.x;
                    }
                }
            }

            return sum;
        }

        private float maxYOfChildNodes(ENode node, float offset)
        {
            float left = 0.0f;
            float right = 0.0f;
            for (int i = 0; i < node.Children.Count; i++)
            {
                if (node.Children[i].Left)
                {
                    if (node.Children[i].IsHouse())
                    {
                        if (node.Children[i].Scale.y > left) left = node.Children[i].Scale.y;
                    }
                    else
                    {
                        if (node.Children[i].Scale.x > left) left = node.Children[i].Scale.x;
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

            return left + right + relativeStreetWidth(node);
        }

        private float maxXOfChildren(ENode node, float offset)
        {
            float left = sumXOfChildren(node, offset, true);
            float right = sumXOfChildren(node, offset, false);
            float max = left < right ? right : left;
            return max;
        }

        private float sumXOfChildren(ENode node, float offset, bool left)
        {
            float sum = offset;
            for (int i = 0; i < node.Children.Count; i++)
            {
                if (node.Children[i].Left == left)
                {
                    if (node.Children[i].IsHouse()) sum += node.Children[i].Scale.x + offset;
                    else if (node.Children[i].IsStreet()) sum += node.Children[i].Scale.y + offset;
                }
            }

            return sum + relativeStreetWidth(node);
        }

        private float relativeStreetWidth(ENode node)
        {
            return StreetWidth * ((MaxDepth + 1) - node.Depth) / (MaxDepth + 1);
        }
    }
}