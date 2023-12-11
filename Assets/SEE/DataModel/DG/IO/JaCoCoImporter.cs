using SEE.DataModel.DG;
using System;


namespace Assets.SEE.DataModel.DG.IO
{
    /// <summary>
    /// Reads a testreport from a xml file and add information to GraphElement/Nodes. This class should be similar to GraphReader.
    /// </summary>
    public class JaCoCoImporter : IDisposable
    {
        // graph where to add the XML-Testreport information
        private Graph graph;

        // current used Graphelement to add the attribute information
        private GraphElement currentElement;

        // Filename of the xml-file which has the testreport
        private String filename;

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}