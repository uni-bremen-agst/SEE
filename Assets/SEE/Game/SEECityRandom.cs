﻿using SEE.DataModel.DG;
using SEE.Tools;
using System;
using System.Collections.Generic;
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
        public List<RandomAttributeDescriptor> LeafAttributes = Defaults();

        public static int DefaultAttributeMean = 10;

        public static int DefaultAttributeStandardDerivation = 3;

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
            foreach (var value in Enum.GetValues(typeof(NumericAttributeNames)).Cast<NumericAttributeNames>())
            {
                leafAttributeNames.Add(value.Name());
            }
            List<RandomAttributeDescriptor> result = new List<RandomAttributeDescriptor>();

            foreach (string attribute in leafAttributeNames)
            {
                result.Add(new RandomAttributeDescriptor(attribute, DefaultAttributeMean, DefaultAttributeStandardDerivation));
            }
            return result;
        }

        public override void LoadAndDrawGraph()
        {
            LoadData();
            DrawGraph();
        }

        public override void LoadData()
        {
            // generate graph randomly
            RandomGraphs randomGraphs = new RandomGraphs();
            LoadedGraph = randomGraphs.Create(LeafConstraint, InnerNodeConstraint, LeafAttributes);
        }
    }
}