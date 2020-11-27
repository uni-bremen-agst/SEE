using UnityEngine;
using System.Collections.Generic;
using System.Globalization;

namespace SEE.Layout.IO
{
    /// <summary>
    /// Reads layouts in SEE's layout format (SLD = SEE Layout Data).
    /// SLD captures the complete transform of a game object.
    /// 
    /// <seealso cref="SLDWriter"/>.     
    /// </summary>
    public class SLDReader
    {
        /// <summary>
        /// Reads layout information from given SLD file with given <paramref name="filename"/>.
        /// The given position and scale of the <paramref name="gameNodes"/> are updated 
        /// according to the layout data contained therein. The IGameNode's rotation around
        /// the y axis is retrieved from the y co-ordinate of the Euler angles contained in 
        /// the file.
        /// 
        /// Precondition: <paramref name="filename"/> must exist and its content conform to SLD.
        /// The format of SLD is described at <see cref="SLDWriter"/>.
        /// 
        /// Precondition: IDs in the SLD file must be contained in <paramref name="gameNodes"/>;
        /// otherwise an error message is emitted for each ID in the loaded file that does not
        /// exist in <paramref name="gameNodes"/>. IGameNodes contained in <paramref name="gameNodes"/>
        /// for which no layout information exist in the layout file will not be updated.
        /// </summary>
        /// <param name="filename">name of GVL file</param>
        /// <param name="gameNodes">the game nodes whose position and scale are to be updated</param>
        public static void Read(string filename, List<IGameNode> gameNodes)
        {
            Dictionary<string, IGameNode> result = ToMap(gameNodes);

            string[] data = System.IO.File.ReadAllLines(filename);
            
            int lineNumber = 0;
            foreach (string line in data)
            {
                lineNumber++;
                string[] columns = line.Split(SLDWriter.Delimiter);

                if (columns.Length < minimalColumns)
                {
                    Debug.LogErrorFormat
                        ("{0}:{1}: Data format error. Expected at least {2} entries separated by {3}. Got: {4} in '{5}'.\n",
                         filename, lineNumber, minimalColumns, SLDWriter.Delimiter, columns.Length, line);
                }
                else
                {
                    string ID = columns[0];

                    if (result.TryGetValue(ID, out IGameNode node))
                    {
                        Vector3 position;
                        position.x = float.Parse(columns[1], CultureInfo.InvariantCulture);
                        position.y = float.Parse(columns[2], CultureInfo.InvariantCulture);
                        position.z = float.Parse(columns[3], CultureInfo.InvariantCulture);

                        Vector3 eulerAngles;
                        eulerAngles.x = float.Parse(columns[4], CultureInfo.InvariantCulture);
                        eulerAngles.y = float.Parse(columns[5], CultureInfo.InvariantCulture);
                        eulerAngles.z = float.Parse(columns[6], CultureInfo.InvariantCulture);

                        Vector3 scale;
                        scale.x = float.Parse(columns[7], CultureInfo.InvariantCulture);
                        scale.y = float.Parse(columns[8], CultureInfo.InvariantCulture);
                        scale.z = float.Parse(columns[9], CultureInfo.InvariantCulture);

                        // Note: We ignore all remaining columns if there are any.

                        node.CenterPosition = position;
                        node.Rotation = eulerAngles.y;
                        node.LocalScale = scale;
                    }
                    else
                    {
                        Debug.LogErrorFormat
                           ("{0}:{1}: Unknown node ID {2}.\n", filename, lineNumber, ID);
                    }
                }
            }
        }

        /// <summary>
        /// The minimal number of columns a CSV file containing path data must have:
        /// 1 (ID) + 3 (position = Vector3) + 3 (rotation = Euler angles) + 3 (scale = Vector3) = 10.
        /// </summary>
        private static readonly int minimalColumns = 10;

        /// <summary>
        /// Returns a mapping from the IDs of all <paramref name="gameNodes"/> onto
        /// those <paramref name="gameNodes"/>. This mapping allows us to quickly
        /// identify the nodes by their IDs.
        /// </summary>
        /// <param name="gameNodes">game nodes that are to be mapped</param>
        /// <returns>mapping from the IDs onto <paramref name="gameNodes"/></returns>
        private static Dictionary<string, IGameNode> ToMap(ICollection<IGameNode> gameNodes)
        {
            Dictionary<string, IGameNode> result = new Dictionary<string, IGameNode>();
            foreach (IGameNode gameNode in gameNodes)
            {
                result[gameNode.ID] = gameNode;
            }
            return result;
        }
    }
}