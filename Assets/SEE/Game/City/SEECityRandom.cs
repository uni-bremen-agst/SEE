using System;
using System.Collections.Generic;
using System.Linq;
using SEE.DataModel.DG;
using SEE.GO;
using SEE.Tools;
using SEE.Utils;
using Sirenix.OdinInspector;
using UnityEngine;

namespace SEE.Game.City
{
    /// <summary>
    /// Manages settings for generating random graphs.
    /// </summary>
    [Serializable]
    public class SEECityRandom : SEECity
    {
        /// IMPORTANT NOTE: If you add any attribute that should be persisted in a
        /// configuration file, make sure you save and restore it in
        /// <see cref="SEECityRandom.Save(ConfigWriter)"/> and
        /// <see cref="SEECityRandom.Restore(Dictionary{string,object})"/>,
        /// respectively. You should also extend the test cases in TestConfigIO.

        /// <summary>
        /// Constraints for the random generation of leaf nodes.
        /// </summary>
        [SerializeField, ShowInInspector, Tooltip("Constraints for the random generation of leaf nodes")]
        public Constraint LeafConstraint = new Constraint("Class", 300, "calls", 0.01f);

        /// <summary>
        /// Constraints for the random generation of inner nodes.
        /// </summary>
        [SerializeField, ShowInInspector, Tooltip("Constraints for the random generation of inner nodes")]
        public Constraint InnerNodeConstraint = new Constraint("Package", 50, "uses", 0.005f);

        /// <summary>
        /// The leaf node attributes and their constraints for the random generation of their values.
        ///
        /// Note: The type of this attribute must be <see cref="List{T}"/> because that type
        /// can be serialized by Unity. It cannot be a generic <see cref="IList{T}"/>.
        /// The serialization is used in <see cref="SEEEditor.SEECityRandomEditor"/>.
        /// </summary>
        [SerializeField, ShowInInspector, Tooltip("The leaf node attributes and their constraints for the random generation of their values")]
        public List<RandomAttributeDescriptor> LeafAttributes = Defaults();

        /// <summary>
        /// The default value for the mean of the distribution from which to generate
        /// leaf metrics randomly.
        /// </summary>
        public static int DefaultAttributeMean = 10;

        /// <summary>
        /// The default value for the standard deviation of the distribution from which to generate
        /// leaf metrics randomly.
        /// </summary>
        public static int DefaultAttributeStandardDeviation = 3;

        /// <summary>
        /// Returns the default settings for leaf node attribute constraints (for the random
        /// generation of their values).
        /// </summary>
        /// <returns>default settings for leaf node attribute constraints</returns>
        public static List<RandomAttributeDescriptor> Defaults()
        {
            // We are using a set because the same name could be used more than once
            // in the settings below.
            HashSet<string> leafAttributeNames = new HashSet<string>();
            foreach (NumericAttributeNames value in Enum.GetValues(typeof(NumericAttributeNames)).Cast<NumericAttributeNames>())
            {
                leafAttributeNames.Add(value.Name());
            }
            List<RandomAttributeDescriptor> result = new List<RandomAttributeDescriptor>();

            foreach (string attribute in leafAttributeNames)
            {
                result.Add(new RandomAttributeDescriptor(attribute, DefaultAttributeMean, DefaultAttributeStandardDeviation));
            }
            return result;
        }

        /// <summary>
        /// Loads the graph data and draws the graph.
        /// </summary>
        public override void LoadAndDrawGraph()
        {
            LoadData();
            DrawGraph();
        }

        /// <summary>
        /// Generates the graph randomly according <see cref="LeafConstraint"/>,
        /// <see cref="InnerNodeConstraint"/>, and <see cref="LeafAttributes"/>.
        /// </summary>
        [Button(ButtonSizes.Small)]
        [ButtonGroup(DataButtonsGroup)]
        [PropertyOrder(DataButtonsGroupOrderLoad)]
        public override void LoadData()
        {
            // generate graph randomly
            RandomGraphs randomGraphs = new RandomGraphs();
            LoadedGraph = randomGraphs.Create(LeafConstraint, InnerNodeConstraint, LeafAttributes, true);
        }

        [Button(ButtonSizes.Small)]
        [ButtonGroup(DataButtonsGroup)]
        [PropertyOrder(DataButtonsGroupOrderLoad)]
        private void AddCloneEdges()
        {
            float threshold = 2.75f;

            // FIXME: To be removed after the VISSOFT paper submission.
            if (LoadedGraph != null)
            {
                ISet<string> metrics = LoadedGraph.AllNumericNodeAttributes();
                ZScoreScale zscore = new ZScoreScale(new List<Graph> { LoadedGraph }, metrics, true);
                IList<Node> nodes = LoadedGraph.Nodes();
                int numberOfEdgesAdded = 0;
                int numberOfComparisons = 0;
                float minimumDistance = float.MaxValue;
                int numberOfClones = 0;
                int numberOfLeaves = 0;

                for (int i = 0; i < nodes.Count; i++)
                {
                    if (nodes[i].IsLeaf())
                    {
                        numberOfLeaves++;
                        bool isClone = false;
                        for (int j = i + 1; j < nodes.Count; j++)
                        {
                            if (nodes[j].IsLeaf())
                            {
                                numberOfComparisons++;
                                float distance = Distance(metrics, zscore, nodes[i], nodes[j]);
                                if (distance < minimumDistance)
                                {
                                    minimumDistance = distance;
                                }
                                if (distance <= threshold)
                                {
                                    //Debug.Log($"Distance({nodes[i].ID}, {nodes[j].ID}) = {distance} <= {threshold}: {distance <= threshold}.\n");
                                    Debug.Log($"{nodes[i].ID};{nodes[j].ID};{distance}\n");
                                    LoadedGraph.AddEdge(new Edge(nodes[i], nodes[j], "clone-" + distance));
                                    numberOfEdgesAdded++;
                                    isClone = true;
                                }
                            }
                        }
                        if (isClone)
                        {
                            numberOfClones++;
                        }
                    }
                }
                Debug.Log($"Added {numberOfEdgesAdded} clone edges for {numberOfComparisons} comparisons. Minimum distance = {minimumDistance}.\n");
                Debug.Log($"Clone rate  {numberOfClones}/{numberOfLeaves} =  {(float)numberOfClones / numberOfLeaves}.\n");
            }

            float Distance(ISet<string> metrics, ZScoreScale zscore, Node left, Node right)
            {
                return Euclidean(GetMetrics(metrics, zscore, left), GetMetrics(metrics, zscore, right));
            }

            float[] GetMetrics(ISet<string> metrics, ZScoreScale zscore, Node node)
            {
                float[] result = new float[metrics.Count];
                int i = 0;
                foreach (string metric in metrics)
                {
                    result[i] = zscore.GetMetricValue(node, metric);
                    i++;
                }
                return result;
            }

            float Euclidean(float[] leftVector, float[] rightVector)
            {
                float result = 0;
                for (int i = 0; i < leftVector.Length; i++)
                {
                    float diff = leftVector[i] - rightVector[i];
                    result += diff * diff;
                }
                return Mathf.Sqrt(result);
            }
        }

        //----------------------------------------------------------------------------
        // Input/output of configuration attributes
        //----------------------------------------------------------------------------

        /// <summary>
        /// Label of LeafConstraint in the configuration file.
        /// </summary>
        private const string LeafConstraintLabel = "LeafConstraint";
        /// <summary>
        /// Label of InnerNodeConstraint in the configuration file.
        /// </summary>
        private const string InnerNodeConstraintLabel = "InnerNodeConstraint";
        /// <summary>
        /// Label of LeafAttributes in the configuration file.
        /// </summary>
        private const string LeafAttributesLabel = "LeafAttributes";

        /// <summary>
        /// <see cref="City.AbstractSEECity.Save(ConfigWriter)"/>
        /// </summary>
        protected override void Save(ConfigWriter writer)
        {
            base.Save(writer);
            LeafConstraint.Save(writer, LeafConstraintLabel);
            InnerNodeConstraint.Save(writer, InnerNodeConstraintLabel);
            writer.Save(LeafAttributes, LeafAttributesLabel); // LeafAttributes are stored as a list
        }

        /// <summary>
        /// <see cref="City.AbstractSEECity.Restore(Dictionary{string, object})"/>.
        /// </summary>
        protected override void Restore(Dictionary<string, object> attributes)
        {
            base.Restore(attributes);
            LeafConstraint.Restore(attributes, LeafConstraintLabel);
            InnerNodeConstraint.Restore(attributes, InnerNodeConstraintLabel);
            // LeafAttributes are stored as a list
            {
                /// This is a bit akward because attribute <see cref="LeafAttributes"/>
                /// must be a <see cref="List{T}"/> and cannot be a <see cref="IList{T}"/>.
                IList<RandomAttributeDescriptor> leafAttributes = LeafAttributes;
                ConfigIO.RestoreList(attributes, LeafAttributesLabel, ref leafAttributes);
                LeafAttributes = leafAttributes as List<RandomAttributeDescriptor>;
            }
        }
    }
}