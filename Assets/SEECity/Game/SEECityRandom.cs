using System.Collections.Generic;
using UnityEngine;
using System;

using SEE.Tools;
using SEE.DataModel;
using System.Linq;

namespace SEE.Game
{
    /// <summary>
    /// Manages settings of generating a random graph data.
    /// </summary>
    public class SEECityRandom : SEECity
    {
        /// <summary>
        /// Node type of generated leaves.
        /// </summary>
        [SerializeField]
        public string LeafNodeType = "File";

        /// <summary>
        /// Node type of generated inner nodes.
        /// </summary>
        [SerializeField]
        public string InnerNodeType = "Directory";

        /// <summary>
        /// The number of leaf nodes to be generated.
        /// </summary>
        [SerializeField]
        public int NumberOfLeaves = 100;

        /// <summary>
        /// The number of inner nodes to be generated.
        /// </summary>
        [SerializeField]
        public int NumberOfInnerNodes = 20;

        /// <summary>
        /// The leaf node attributes and their constraints for the random generation of their values.
        /// </summary>
        [SerializeField]
        public List<RandomAttributeDescriptor> LeafAttributes = Defaults();

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
                result.Add(new RandomAttributeDescriptor(attribute, 100, 30));
            }
            return result;
        }

        public override void LoadData()
        {
            // generate graph randomly
            RandomGraphs randomGraphs = new RandomGraphs(LeafNodeType, InnerNodeType);

            graph = randomGraphs.Create(NumberOfLeaves, 0.15f, LeafAttributes);
            DrawGraph();
        }
    }
}