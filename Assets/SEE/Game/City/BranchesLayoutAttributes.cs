using SEE.Game.City;
using SEE.Utils.Config;
using SEE.Utils.Paths;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Game.City
{
    /// <summary>
    /// The settings for the layout of the edges.
    /// </summary>
    public class BranchesLayoutAttributes : LayoutSettings
    {
        public FilePath FilePath = new();

        public string BranchA = "";

        public string BranchB = "";

        public override void Save(ConfigWriter writer, string label)
        {
            writer.BeginGroup(label);
            writer.Save(FilePath.Path, dataLayoutLable);
            writer.Save(BranchA, dataLayoutLable);
            writer.Save(BranchB, dataLayoutLable);
            writer.EndGroup();
        }

        public override void Restore(Dictionary<string, object> attributes, string label)
        {
            if (attributes.TryGetValue(label, out object dictionary))
            {
                Dictionary<string, object> values = dictionary as Dictionary<string, object>;

                ConfigIO.Restore(values, dataLayoutLable, ref FilePath);
                ConfigIO.Restore(values, dataLayoutLable, ref BranchA);
                ConfigIO.Restore(values, dataLayoutLable, ref BranchB);
            }
        }

        private const string dataLayoutLable = "DataLayout";
    }
}

