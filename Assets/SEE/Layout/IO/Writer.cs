using SEE.Utils;
using System;
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
        /// extension: <see cref="Filenames.GVLExtension"/> for GVL format and <see cref="Filenames.SLDExtension"/>
        /// for the SLD format. If the file extension is unknown, an exception is thrown.
        ///
        /// If the file already exists, it will be overwritten.
        /// </summary>
        /// <param name="filename">Name of the file.</param>
        /// <param name="graphName">Name of the graph.</param>
        /// <param name="gameNodes">The nodes whose layout is to be stored.</param>
        /// <exception cref="Exception">Thrown in case the extension is unknown.</exception>
        public static void Save(string filename, string graphName, ICollection<GameObject> gameNodes)
        {
            if (Filenames.HasExtension(filename, Filenames.GVLExtension))
            {
                GVLWriter.Save(filename, graphName, gameNodes);
            }
            else if (Filenames.HasExtension(filename, Filenames.SLDExtension))
            {
                SLDWriter.Save(filename, gameNodes);
            }
            else
            {
                throw new Exception($"Unknown layout file format for file extension of {filename}.");
            }
        }
    }
}
