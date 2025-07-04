using SEE.Game.City;
using System;
using SEE.Utils.Paths;
using Cysharp.Threading.Tasks;

namespace SEE.Net.Actions.City
{
    /// <summary>
    /// Loads a part of a <see cref="SEEReflexionCity"/> to all clients.
    /// </summary>
    public class LoadPartOfReflexionCityNetAction : CityNetAction
    {
        /// <summary>
        /// Distinction between whether an architecture or implementation graph should be loaded.
        /// </summary>
        public bool LoadArchitecture;

        /// <summary>
        /// The graph file to be loaded.
        /// </summary>
        public DataPath GraphFileToLoad;

        /// <summary>
        /// The project folder; can be null if an architecture graph should be loaded.
        /// </summary>
        public DataPath ProjectFolder;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="tableID">The unique name of the table on which city the part should be loaded.</param>
        /// <param name="loadArchitecture">Distinction between whether an architecture or implementation graph should be loaded.</param>
        /// <param name="graphFile">The graph file to be loaded.</param>
        /// <param name="projectFolder">The project folder; can be null if a architecture graph should be loaded.</param>
        public LoadPartOfReflexionCityNetAction(string tableID, bool loadArchitecture,
            DataPath graphFile, DataPath projectFolder = null) : base(tableID)
        {
            LoadArchitecture = loadArchitecture;
            GraphFileToLoad = graphFile;
            ProjectFolder = projectFolder;
        }

        /// <summary>
        /// Loads the given graph into the city on the specified table on each client.
        /// </summary>
        public override void ExecuteOnClient()
        {
            base.ExecuteOnClient();
            if (City.GetComponent<SEEReflexionCity>() != null)
            {
                City.GetComponent<SEEReflexionCity>().LoadAndDrawSubgraphAsync(GraphFileToLoad, LoadArchitecture ?
                    null : ProjectFolder).Forget();
            }
            else
            {
                throw new Exception($"Graph can't be loaded because there is no {nameof(SEEReflexionCity)} component.");
            }
        }
    }
}
