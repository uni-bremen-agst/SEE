using SEE.Utils;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Layout.IO
{
    /// <summary>
    /// Writer for saving layout information to files. The actual writing is
    /// deferred to specialized writers such as <see cref="GVLWriter"/> and
    /// <see cref="SLDWriter"/> depending on the file extension. Using this
    /// class frees all clients from making the distinction of the specific format
    /// themselves.
    /// </summary>
    public static class Writer
    {
        /// <summary>
        /// Writes the layout information of all root <paramref name="gameNodes"/> and their descendants
        /// to a new file named <paramref name="filename"/>. The exact format is determined by the file
        /// extension: <see cref="Filenames.GVLExtension"/> for GVL format, anything else for SLD format.
        /// </summary>
        /// <param name="filename">name of the GVL file</param>
        /// <param name="graphName">name of the graph</param>
        /// <param name="gameNodes">the nodes whose layout is to be stored</param>
        public static void Save(string filename, string graphName, ICollection<GameObject> gameNodes)
        {
            if (Filenames.HasExtension(filename, Filenames.GVLExtension))
            {
                GVLWriter.Save(filename, graphName, gameNodes);
            }
            else
            {
                SLDWriter.Save(filename, gameNodes);
            }
        }
    }
}
