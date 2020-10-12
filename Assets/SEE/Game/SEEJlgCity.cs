
using Assets.SEE.DataModel;
using Assets.SEE.DataModel.IO;
using SEE.DataModel;
using System.IO;
using UnityEngine;

namespace SEE.Game
{
    public class SEEJlgCity : SEECity
    {
        /// <summary>
        /// The full path to the jlg source file.
        /// </summary>
        public string jlgPath;

        public override void LoadData()
        {
            base.LoadData();
            LoadJLG();
        }

        /// <summary>
        /// Loads the data from the given jlg file into a parsedJLG object and gives the object to a GameObject, that has a component to visualize it in the running game.
        /// </summary>
        private void LoadJLG()
        {
            if (string.IsNullOrEmpty(jlgPath))
            {
                Debug.LogError("Path to JLG source file cannot be empty.\n");
            }
            else if (!File.Exists(jlgPath))
            {
                Debug.LogError("Source file does not exist at that path.\n");
            }
            else
            {
                GameObject jlgVisualisationGameObject = new GameObject();
                jlgVisualisationGameObject.transform.parent = transform;
                jlgVisualisationGameObject.name = Tags.JLGVisualization;
                jlgVisualisationGameObject.tag = Tags.JLGVisualization;

                jlgVisualisationGameObject.AddComponent<Runtime.JLGVisualizer>().jlgFilePath = jlgPath;
            }

        }
    }
}
