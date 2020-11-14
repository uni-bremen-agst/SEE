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

        /// <summary>
        /// Returns the concatenation of pathPrefix and jlgPath. That is the complete
        /// absolute path to the JLG file containing the runtime trace data.
        /// </summary>
        /// <returns>concatenation of pathPrefix and jlgPath</returns>
        public string JLGPath()
        {
            return PathPrefix + jlgPath;
        }

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
            string path = JLGPath();

            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError("Path to JLG source file must not be empty.\n");
            }
            else if (!File.Exists(path))
            {
                Debug.LogErrorFormat("Source file does not exist at that path {0}.\n", path);
            }
            else
            {
                GameObject jlgVisualisationGameObject = new GameObject();
                jlgVisualisationGameObject.transform.parent = transform;
                jlgVisualisationGameObject.name = Tags.JLGVisualization;
                jlgVisualisationGameObject.tag = Tags.JLGVisualization;

                jlgVisualisationGameObject.AddComponent<Runtime.JLGVisualizer>().jlgFilePath = path;
            }

        }
    }
}
