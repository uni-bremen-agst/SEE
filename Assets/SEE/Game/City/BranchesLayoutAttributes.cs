using SEE.Game.City;
using SEE.UI.RuntimeConfigMenu;
using SEE.Utils.Config;
using SEE.Utils.Paths;
using Sirenix.OdinInspector;
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
        /*[SerializeField, ShowInInspector, Tooltip("Path of File Path"), TabGroup(DataFoldoutGroup), RuntimeTab(DataFoldoutGroup)]
        public FilePath FilePath = new();*/

        [SerializeField, ShowInInspector, Tooltip("Path of first GXL file"), TabGroup(DataFoldoutGroup), RuntimeTab(DataFoldoutGroup)]
        public FilePath GLXPath1 = new();

        [SerializeField, ShowInInspector, Tooltip("Path of second GXL file"), TabGroup(DataFoldoutGroup), RuntimeTab(DataFoldoutGroup)]
        public FilePath GLXPath2 = new();


        public override void Save(ConfigWriter writer, string label)
        {
            writer.BeginGroup(label);
            //writer.Save(FilePath.Path, dataLayoutLable);
            writer.Save(GLXPath1.Path, dataLayoutLable);
            writer.Save(GLXPath2.Path, dataLayoutLable);
            writer.EndGroup();
        }

        public override void Restore(Dictionary<string, object> attributes, string label)
        {
            if (attributes.TryGetValue(label, out object dictionary))
            {
                Dictionary<string, object> values = dictionary as Dictionary<string, object>;

                //ConfigIO.Restore(values, dataLayoutLable, ref FilePath);
                ConfigIO.Restore(values, dataLayoutLable, ref GLXPath1);
                ConfigIO.Restore(values, dataLayoutLable, ref GLXPath2);
            }
        }

        private const string dataLayoutLable = "DataLayout";

        /// <summary>
        /// Name of the Inspector foldout group for the data setttings.
        /// </summary>
        protected const string DataFoldoutGroup = "Data";
    }
}

