// Copyright 2020 Lennart Kipka
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS
// BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR
// IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using SEE.DataModel;
using System.IO;
using UnityEngine;

namespace SEE.Game
{
    public class SEEJlgCity : SEECity
    {
        /// <summary>
        /// Path to the JLG file containing the runtime trace data.
        /// </summary>
        /// <returns>path of JLG file</returns>
        public DataPath JLGPath = new DataPath();

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
            string path = JLGPath.Path;

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
                AddJLGVisualizationIfNecessary(path);
            }
        }

        private void AddJLGVisualizationIfNecessary(string path)
        {
            // do we already have a JLGVisualization child?
            foreach (Transform child in transform)
            {
                if (child.name == Tags.JLGVisualization && child.CompareTag(Tags.JLGVisualization))
                {
                    // make sure this child has the necessary Runtime.JLGVisualizer component
                    if (!child.TryGetComponent<Runtime.JLGVisualizer>(out Runtime.JLGVisualizer component))
                    {
                        component = child.gameObject.AddComponent<Runtime.JLGVisualizer>();
                    }
                    component.jlgFilePath = path;
                    return;
                }
            }
            // no such child exists; we need to add one
            GameObject jlgVisualisationGameObject = new GameObject();
            jlgVisualisationGameObject.transform.parent = transform;
            jlgVisualisationGameObject.name = Tags.JLGVisualization;
            jlgVisualisationGameObject.tag = Tags.JLGVisualization;

            jlgVisualisationGameObject.AddComponent<Runtime.JLGVisualizer>().jlgFilePath = path;
        }
    }
}
