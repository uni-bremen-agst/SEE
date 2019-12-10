using System;
using System.Collections.Generic;
using SEE.DataModel;
using UnityEngine;

namespace SEE.Layout.EvoStreets
{
    public class SoftwareCity
    {
        /// <summary>
        /// The leaf and inner nodes to be laid out.
        /// </summary>
        private List<GameObject> gameObjects = new List<GameObject>();

        /// <summary>
        /// Determines how to scale the node metrics.
        /// </summary>
        private IScale scaler;

        /// <summary>
        /// The settings to be considered for the layout.
        /// </summary>
        private GraphSettings graphSettings;

        /// <summary>
        /// The resulting layout.
        /// </summary>
        private Dictionary<GameObject, NodeTransform> layout_result;

        public Dictionary<GameObject, NodeTransform> GenerateCity(DataModel.Graph graph, IScale scaler, GraphSettings graphSettings)
        {
            layout_result = new Dictionary<GameObject, NodeTransform>();

            DateTime before = DateTime.Now;

            this.graphSettings = graphSettings;
            this.scaler = scaler;

            NodeFactory leafNodeFactory = new CubeFactory(); // FIXME
            float groundLevel = 0.0f; // FIXME

            gameObjects = AllNodes(graph);

            EvoStreetsNodeLayout evoStreetLayout = new EvoStreetsNodeLayout(groundLevel, leafNodeFactory, scaler, graphSettings);
            Dictionary<GameObject, NodeTransform> layout = evoStreetLayout.Layout(gameObjects);
            ENode rootNode = evoStreetLayout.GetRoot();

            //ENode rootNode = cityGenerator.GenerateCity(graph, scaler, graphSettings);
            bool rootStreetDimensionless = rootNode.Depth != 0;

            GameObject gameObject = new GameObject();
            gameObject.tag = DataModel.Tags.Block;
            gameObject.name = "ROOT";
            SpawnNode(rootNode, gameObject);

            DateTime after = DateTime.Now;
            TimeSpan duration = after.Subtract(before);

            Debug.Log("Duration in milliseconds: " + duration.Milliseconds + "\n");

            return layout_result;
        }

        // FIXME: Can be removed later.
        private List<GameObject> AllNodes(Graph graph)
        {
            List<GameObject> gameObjects = new List<GameObject>();

            foreach (Node node in graph.Nodes())
            {
                GameObject gameObject = new GameObject(node.LinkName);
                gameObject.tag = Tags.Block;
                NodeRef nodeRef = gameObject.AddComponent<NodeRef>();
                nodeRef.node = node;
                gameObjects.Add(gameObject);
            }
            return gameObjects;
        }

        private NodeFactory leafNodeFactory = new CubeFactory();

        private GameObject NewLeaf(DataModel.Node node)
        {
            float metricMaximum = scaler.GetNormalizedMaximum(graphSettings.ColorMetric);

            int material = Mathf.RoundToInt(Mathf.Lerp(0.0f,
                                                       (float)(leafNodeFactory.NumberOfMaterials() - 1),
                                                       scaler.GetNormalizedValue(graphSettings.ColorMetric, node)
                                                                 / metricMaximum));
            return leafNodeFactory.NewBlock(material);
        }

        private void SpawnNode(ENode node, GameObject parentGameObject)
        {
            if (node == null)
            {
                Debug.LogAssertion("Node cannot be null");
                return;
            }

            if (node.IsHouse())
            {
                var spawnedHouse = SpawnHouse(node);

                // TODO: Root Point Calculation for RelationshipGenerator

                if (spawnedHouse == null)
                {
                    Debug.LogError($"House could not be spawned: {node}");
                    return;
                }

                gameObjects.Add(spawnedHouse);
            } // End isHouse

            if (node.IsStreet())
            {
                var spawnedStreet = SpawnStreet(node);

                if (spawnedStreet == null)
                {
                    Debug.LogError($"Street could not be spawned: {node}");
                    return;
                }

                gameObjects.Add(spawnedStreet);

                foreach (var child in node.Children)
                {
                    SpawnNode(child, spawnedStreet);
                }

                // TODO: district generation
            } // End isStreet
        }

        private GameObject SpawnHouse(ENode node)
        {
            if (node == null)
            {
                Debug.LogError("Node must not be null");
                return null;
            }

            // TODO: instantiate some kind of actor/gameobj to display house
            var rotation = Quaternion.Euler(0, node.Rotation, 0); // this needs to be assigned to y due to difference between unity and unreal
            var o = NewLeaf(node.GraphNode);
            o.transform.localScale = node.Scale;
            o.transform.position = node.Location + graphSettings.origin;
            o.transform.rotation = rotation;
            o.name = (node.IsHouse() ? "House " : (node.IsStreet()) ? "Street " : "None ") + node.GraphNode.LinkName;
            AttachNode(o, node.GraphNode);
            layout_result[o] = new NodeTransform(o.transform.position, o.transform.localScale);

            return o;
        }

        /// <summary>
        /// Adds a NodeRef component to given game node referencing to given graph node.
        /// </summary>
        /// <param name="gameNode"></param>
        /// <param name="node"></param>
        protected void AttachNode(GameObject gameNode, DataModel.Node node)
        {
            NodeRef nodeRef = gameNode.AddComponent<NodeRef>();
            nodeRef.node = node;
        }

        private CubeFactory streetFactory = new CubeFactory();

        private GameObject SpawnStreet(ENode node)
        {
            if (node == null)
            {
                Debug.LogError("Node must not be null");
                return null;
            }

            // this needs to be assigned to y due to difference between unity and unreal
            var rotation = Quaternion.Euler(0, node.Rotation, 0);
            var o = streetFactory.NewBlock(0);
            o.transform.position = node.Location + graphSettings.origin;
            o.transform.rotation = rotation;
            o.transform.localScale = new Vector3(node.Scale.x, EvoStreetsNodeLayout.StreetHeight, node.Scale.z);
            o.name = (node.IsHouse() ? "House " : (node.IsStreet()) ? "Street " : "None ") + node.GraphNode.LinkName;
            AttachNode(o, node.GraphNode);
            layout_result[o] = new NodeTransform(o.transform.position, o.transform.localScale);

            return o;
        }
    }
}
