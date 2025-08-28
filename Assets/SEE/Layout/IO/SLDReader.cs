using UnityEngine;
using System.Collections.Generic;
using System.Globalization;
using SEE.DataModel.DG;
using SEE.Utils;

namespace SEE.Layout.IO
{
    /// <summary>
    /// Reads layouts in SEE's layout format (SLD = SEE Layout Data).
    /// SLD captures the complete transform of a game object. The syntax
    /// of SLD is described in the documentation of <see cref="SLDWriter"/>.
    ///
    /// <seealso cref="SLDWriter"/>.
    /// </summary>
    public class SLDReader
    {

        /// <summary>
        /// Extracts the position from the given list <paramref name="columns"/> of an SLD file line.
        /// </summary>
        /// <param name="columns">Represents a line in a SLD file.</param>
        /// <returns></returns>
        private static Vector3 ExtractPositionFromColumnList(string[] columns)
        {
            Vector3 position;
            position.x = float.Parse(columns[1], CultureInfo.InvariantCulture);
            position.y = float.Parse(columns[2], CultureInfo.InvariantCulture);
            position.z = float.Parse(columns[3], CultureInfo.InvariantCulture);
            return position;
        }

        /// <summary>
        /// Extracts the Euler angles from the given list <paramref name="columns"/> of an SLD file line.
        /// </summary>
        /// <param name="columns">Represents a line in a SLD file.</param>
        /// <returns></returns>
        private static Vector3 ExtractEulerAnglesFromColumnList(string[] columns)
        {
            Vector3 eulerAngles;
            eulerAngles.x = float.Parse(columns[4], CultureInfo.InvariantCulture);
            eulerAngles.y = float.Parse(columns[5], CultureInfo.InvariantCulture);
            eulerAngles.z = float.Parse(columns[6], CultureInfo.InvariantCulture);
            return eulerAngles;
        }

        /// <summary>
        /// Extracts the scale from the given list <paramref name="columns"/> of an SLD file line.
        /// </summary>
        /// <param name="columns">Represents a line in a SLD file.s</param>
        /// <returns></returns>
        private static Vector3 ExtractScaleFromColumnList(string[] columns)
        {
            Vector3 scale;
            scale.x = float.Parse(columns[7], CultureInfo.InvariantCulture);
            scale.y = float.Parse(columns[8], CultureInfo.InvariantCulture);
            scale.z = float.Parse(columns[9], CultureInfo.InvariantCulture);
            return scale;
        }

        /// <summary>
        /// Analog to <see cref="Read(string, IEnumerable{IGameNode})"/>, but works on <see cref="IEnumerable{Node}"/> instead of <see cref="IEnumerable{IGameNode}"/>.
        ///
        /// This method will load the layout information from the given SLD file at <paramref name="filename"/> and apply it to the nodes in <paramref name="nodes"/>.
        /// </summary>
        /// <param name="filename">Name of the SLD file.</param>
        /// <param name="nodes">The game nodes whose position and scale are to be updated.</param>
        public static void Read(string filename, IEnumerable<Node> nodes)
        {
            Dictionary<string, Node> result = ToMap(nodes);

            string[] data = System.IO.File.ReadAllLines(filename);

            int lineNumber = 0;
            foreach (string line in data)
            {
                lineNumber++;
                string[] columns = line.Split(SLDWriter.Delimiter);

                if (columns.Length < minimalColumns)
                {
                    Debug.LogError
                        ($"{filename}:{lineNumber}: Data format error. Expected at least {minimalColumns} entries separated by {SLDWriter.Delimiter}."
                        + $"Got: {columns.Length} in '{line}'.\n");
                }
                else
                {
                    string id = columns[0];

                    if (result.TryGetValue(id, out Node node))
                    {
                        Vector3 position = ExtractPositionFromColumnList(columns);
                        Vector3 eulerAngles = ExtractEulerAnglesFromColumnList(columns);
                        Vector3 scale = ExtractScaleFromColumnList(columns);
                        // Note: We ignore all remaining columns if there are any.

                        GameObject goOfNode = node.GameObject();
                        goOfNode.transform.position = position;
                        goOfNode.transform.rotation = Quaternion.Euler(eulerAngles);
                        goOfNode.transform.localScale = scale;
                    }
                    else
                    {
                        Debug.LogError($"{filename}:{lineNumber}: Unknown node ID {id}.\n");
                    }
                }
            }
        }

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
        /// <param name="filename">Name of SLD file.</param>
        /// <param name="gameNodes">The game nodes whose position and scale are to be updated.</param>
        public static void Read(string filename, IEnumerable<IGameNode> gameNodes)
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
                    Debug.LogError
                        ($"{filename}:{lineNumber}: Data format error. Expected at least {minimalColumns} entries separated by {SLDWriter.Delimiter}."
                        + $"Got: {columns.Length} in '{line}'.\n");
                }
                else
                {
                    string id = columns[0];

                    if (result.TryGetValue(id, out IGameNode node))
                    {
                        Vector3 position = ExtractPositionFromColumnList(columns);
                        Vector3 eulerAngles = ExtractEulerAnglesFromColumnList(columns);
                        Vector3 scale = ExtractScaleFromColumnList(columns);
                        // Note: We ignore all remaining columns if there are any.

                        node.CenterPosition = position;
                        node.Rotation = eulerAngles.y;
                        node.AbsoluteScale = scale;
                    }
                    else
                    {
                        Debug.LogError($"{filename}:{lineNumber}: Unknown node ID {id}.\n");
                    }
                }
            }
        }

        /// <summary>
        /// The minimal number of columns a CSV file containing path data must have:
        /// 1 (ID) + 3 (position = Vector3) + 3 (rotation = Euler angles) + 3 (scale = Vector3) = 10.
        /// </summary>
        private const int minimalColumns = 10;

        /// <summary>
        /// Returns a mapping from the IDs of all <paramref name="gameNodes"/> onto
        /// those <paramref name="gameNodes"/>. This mapping allows us to quickly
        /// identify the nodes by their IDs.
        /// </summary>
        /// <param name="gameNodes">game nodes that are to be mapped</param>
        /// <returns>mapping from the IDs onto <paramref name="gameNodes"/></returns>
        private static Dictionary<string, IGameNode> ToMap(IEnumerable<IGameNode> gameNodes)
        {
            Dictionary<string, IGameNode> result = new();
            foreach (IGameNode gameNode in gameNodes)
            {
                result[gameNode.ID] = gameNode;
            }
            return result;
        }

        private static Dictionary<string, Node> ToMap(IEnumerable<Node> nodes)
        {
            Dictionary<string, Node> result = new();
            foreach (Node node in nodes)
            {
                if (node is Node gameNode)
                {
                    result[gameNode.ID] = gameNode;
                }
            }
            return result;
        }
    }
}
