using SEE.Layout.NodeLayouts;
using SEE.Utils;
using System;
using System.Collections.Generic;
using System.IO;

namespace SEE.Layout.IO
{
    /// <summary>
    /// Reader for reading saved layout information. The actual reading is
    /// deferred to specialized readers such as <see cref="GVLReader"/> and
    /// <see cref="SLDReader"/> depending on the file extension. Using this
    /// class frees all clients from making the distinction of the specific format
    /// themselves.
    /// </summary>
    internal static class LayoutReader
    {
        /// <summary>
        /// Reads the layout information for all <paramref name="gameNodes"/>
        /// from a file named <paramref name="filename"/>.
        ///
        /// The exact format is determined by the file extension: <see cref="Filenames.GVLExtension"/>
        /// for GVL format and <see cref="Filenames.SLDExtension"/> for the SLD format. If the file
        /// extension is unknown, an exception is thrown.
        ///
        /// If the file does not exists, an exception is thrown.
        /// </summary>
        /// <param name="filename">name of the layout file</param>
        /// <param name="gameNodes">the nodes whose layout is to be stored</param>
        /// <exception cref="Exception">thrown in case the extension is unknown or the file
        /// cannot be read</exception>
        public static void Read(string filename, ICollection<IGameNode> gameNodes)
        {
            if (!File.Exists(filename))
            {
                throw new Exception($"Layout file {filename} does not exist. No layout could be loaded.");
            }
            if (Filenames.HasExtension(filename, Filenames.GVLExtension))
            {
                new GVLReader(filename, gameNodes, NodeLayout.GroundLevel, new SEELogger());
            }
            else if (Filenames.HasExtension(filename, Filenames.SLDExtension))
            {
                SLDReader.Read(filename, gameNodes);
            }
            else
            {
                throw new Exception($"Unknown layout file format for file extension of {filename}.");
            }
        }
    }
}
