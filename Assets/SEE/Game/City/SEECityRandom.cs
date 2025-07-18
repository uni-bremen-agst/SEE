﻿using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using SEE.DataModel.DG;
using SEE.Tools.RandomGraphs;
using SEE.Utils.Config;
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
        public override UniTaskVoid LoadAndDrawGraphAsync()
        {
            LoadDataAsync().Forget();
            DrawGraph();
            return new UniTaskVoid();
        }

        /// <summary>
        /// Generates the graph randomly according <see cref="LeafConstraint"/>,
        /// <see cref="InnerNodeConstraint"/>, and <see cref="LeafAttributes"/>.
        /// </summary>
        /// <returns>True if the menus need to be adjusted; otherwise, false.</returns>
        [Button(ButtonSizes.Small)]
        [ButtonGroup(DataButtonsGroup)]
        [PropertyOrder(DataButtonsGroupOrderLoad)]
        public override UniTask<bool> LoadDataAsync()
        {
            LoadedGraph = RandomGraphs.Create(LeafConstraint, InnerNodeConstraint, LeafAttributes, true);
            return UniTask.FromResult(false);
        }

        //----------------------------------------------------------------------------
        // Input/output of configuration attributes
        //----------------------------------------------------------------------------

        /// <summary>
        /// Label of LeafConstraint in the configuration file.
        /// </summary>
        private const string leafConstraintLabel = "LeafConstraint";
        /// <summary>
        /// Label of InnerNodeConstraint in the configuration file.
        /// </summary>
        private const string innerNodeConstraintLabel = "InnerNodeConstraint";
        /// <summary>
        /// Label of LeafAttributes in the configuration file.
        /// </summary>
        private const string leafAttributesLabel = "LeafAttributes";

        /// <summary>
        /// <see cref="City.AbstractSEECity.Save(ConfigWriter)"/>
        /// </summary>
        protected override void Save(ConfigWriter writer)
        {
            base.Save(writer);
            LeafConstraint.Save(writer, leafConstraintLabel);
            InnerNodeConstraint.Save(writer, innerNodeConstraintLabel);
            writer.Save(LeafAttributes, leafAttributesLabel); // LeafAttributes are stored as a list
        }

        /// <summary>
        /// <see cref="City.AbstractSEECity.Restore(Dictionary{string, object})"/>.
        /// </summary>
        protected override void Restore(Dictionary<string, object> attributes)
        {
            base.Restore(attributes);
            LeafConstraint.Restore(attributes, leafConstraintLabel);
            InnerNodeConstraint.Restore(attributes, innerNodeConstraintLabel);
            // LeafAttributes are stored as a list
            {
                /// This is a bit akward because attribute <see cref="LeafAttributes"/>
                /// must be a <see cref="List{T}"/> and cannot be a <see cref="IList{T}"/>.
                IList<RandomAttributeDescriptor> leafAttributes = LeafAttributes;
                ConfigIO.RestoreList(attributes, leafAttributesLabel, ref leafAttributes);
                LeafAttributes = leafAttributes as List<RandomAttributeDescriptor>;
            }
        }
    }
}
