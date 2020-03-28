using System.Collections.Generic;
using UnityEngine;
using System;

using SEE.Tools;
using SEE.DataModel;
using System.Linq;

namespace SEE.Game
{
    /// <summary>
    /// Manages settings for generating random graphs.
    /// </summary>
    public class SEECityRandom : SEECity
    {
        /// <summary>
        /// Constraints for the random generation of leaf nodes.
        /// </summary>
        public Constraint LeafConstraint = new Constraint("Class", 300, "calls", 0.01f);

        /// <summary>
        /// Constraints for the random generation of inner nodes.
        /// </summary>
        public Constraint InnerNodeConstraint = new Constraint("Package", 50, "uses", 0.005f);

        /// <summary>
        /// The leaf node attributes and their constraints for the random generation of their values.
        /// </summary>
        [SerializeField]
        public List<RandomAttributeDescriptor> LeafAttributes = Defaults();

        /// <summary>
        /// Returns the default settings for leaf node attribute constraints (for the random 
        /// generation of their values).
        /// </summary>
        /// <returns>default settings for leaf node attribute constraints</returns>
        private static List<RandomAttributeDescriptor> Defaults()
        {
            // We are using a set because the same name could be used more than once
            // in the settings below.
            HashSet<string> leafAttributeNames = new HashSet<string>();
            foreach (var value in Enum.GetValues(typeof(SEE.DataModel.NumericAttributeNames)).Cast<SEE.DataModel.NumericAttributeNames>())
            {
                leafAttributeNames.Add(value.Name());
            }
            List<RandomAttributeDescriptor> result = new List<RandomAttributeDescriptor>();

            foreach (string attribute in leafAttributeNames)
            {
                result.Add(new RandomAttributeDescriptor(attribute, 10, 3));
            }
            return result;
        }

        public override void LoadData()
        {
            // generate graph randomly
            RandomGraphs randomGraphs = new RandomGraphs();
            graph = randomGraphs.Create(LeafConstraint, InnerNodeConstraint, LeafAttributes);
            DrawGraph();
        }
    }
}