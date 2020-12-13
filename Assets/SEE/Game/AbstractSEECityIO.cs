using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;

namespace SEE.Game
{
    /// <summary>
    /// This class is responsible for the export and the import of instances of AbstractSEECity.
    /// The required data is stored in a JSON file.
    /// The object stores values and selections such as specific metrics of a city or selected nodetypes as well which were chosen  
    /// by the user before saving his or her profile.
    /// </summary>
    public class AbstractSEECityIO
    {
        /// <summary>
        ///  Saves the attributes of the given <paramref name="city"/> to a file with given <paramref name="filename"/>
        ///  in JSON format.
        /// </summary>
        /// <param name="city">The city which will be stored in the JSON file</param>
        /// <param name="filename">Name of the output JSON file</param>
        public static void Save(AbstractSEECity city, string filename)
        {
            string citySettingsJson = JsonUtility.ToJson(city, true);
            File.WriteAllText(filename, citySettingsJson);
            Debug.LogFormat("Settings successfully exported to {0}\n", filename);
        }

        /// <summary>
        /// Loads the attributes for given <paramref name="city"/> from the given <paramref name="filename"/>.
        /// </summary>
        /// <param name="city">The city whose attributes are to be read and set</param>
        /// <param name="filename">Name of ths JSON file from whicht to read the attributes</param>
        public static void Load(AbstractSEECity city, string filename)
        {
            string jsonContent = File.ReadAllText(filename);
            if (!(VerifyCityType(city, jsonContent)))
            {
                return;
            }
            JsonUtility.FromJsonOverwrite(jsonContent, city);

            ResolveNodeTypeSelection(city, jsonContent);

            Debug.LogFormat("Settings successfully imported from {0}\n", filename);
        }

        private static void ResolveNodeTypeSelection(AbstractSEECity city, string jsonContent)
        {
            string dataPath = getDataPath(city);
            Dictionary<string, bool> oldNodetypes = city.SelectedNodeTypes;
            Dictionary<string, bool> newNodeTypes = new Dictionary<string, bool>();

            // FIXME: We shouldn't need to use these kinds of type checks.
            if (city is SEECityEvolution)
            {
                SEECityEvolution evoCity = new SEECityEvolution();
                evoCity.GXLDirectory.Set(dataPath);
                if (!(ReloadGraphByCityType(evoCity)))
                {
                    return;
                }
                newNodeTypes = evoCity.SelectedNodeTypes;
            }
            else if (city is SEECity)
            {
                if (!(ReloadGraphByCityType(city)))
                {
                    return;
                }
                newNodeTypes = city.SelectedNodeTypes;
            }
            // We have to store the current enumeration of the nodetypes of the current version in order to compare 
            // it afterwards with the stored one in the method DifferentNodeTypes
            // As the user picks the directory via a directory picker/ the GUI , no specific error handling is needed at this point.
            DifferentNodeTypes(oldNodetypes, jsonContent, newNodeTypes, city);
        }

        /// <summary>
        /// Veryfies whether the types of the city and the json-file are matching.
        /// </summary>
        /// <param name="city">the city, which has to be overwritten</param>
        /// <param name="jsonContent">the content of the .json-file as a string</param>
        /// <returns>true, if the types are matching, otherwise false</returns>
        private static bool VerifyCityType(AbstractSEECity city, string jsonContent)
        {
            if (jsonContent.Contains("isAnSEECityObject") && city is SEECity)
            {
                return true;
            }
            if (jsonContent.Contains("isAnSEECityEvolutionObject") && city is SEECityEvolution)
            {
                return true;
            }
            else
            {
                UnityEngine.Debug.LogErrorFormat("The types of the scene and the loaded .json-file are not matching\n");
                return false;
            }
        }

        /// <summary>
        /// Analyzes if there is a difference between the stored nodetypes and the current nodetypes.
        /// </summary>
        /// <param name="jsonFile">the .json-file with the settings for the city</param>
        /// <param name="oldNodeTypes>a dictionary of the stored nodeTypes</param>
        /// <param name="newNodes> the city, which has to be overwritten</param>
        private static void DifferentNodeTypes(Dictionary<string, bool> oldNodeTypes, string jsonFile, Dictionary<string, bool> newNodeTypes, AbstractSEECity city)
        {
            List<string> oldNodes = oldNodeTypes.Keys.ToList();
            List<string> newNodes = newNodeTypes.Keys.ToList();
            List<string> deletedNodeTypes = oldNodes.Except(newNodes).ToList();
            List<string> addedNodeTypes = newNodes.Except(oldNodes).ToList();

            //shows deleted nodetypes
            if (deletedNodeTypes.Count > 0)
            {
                string deletedOutput = "";

                foreach (string nodeType in deletedNodeTypes)
                {
                    deletedOutput += nodeType + ",";
                }
                deletedOutput = deletedOutput.Substring(0, deletedOutput.Length - 1);
                UnityEngine.Debug.Log("Deleted Nodetypes in the .gxl-file since saving your settings: " + deletedOutput + "\n");
            }
            //shows added nodetypes
            if (addedNodeTypes.Count > 0)
            {
                string addedOutput = "";

                foreach (string nodeType in addedNodeTypes)
                {
                    addedOutput += nodeType + ",";
                }
                addedOutput = addedOutput.Substring(0, addedOutput.Length - 1);
                AddNodeTypes(city, addedNodeTypes);
                UnityEngine.Debug.Log("Added Nodetypes in the .gxl-file since saving your settings: " + addedOutput + "\n");
            }
            //if there are no changes, this message will be shown
            if (deletedNodeTypes.Count == 0 && addedNodeTypes.Count == 0)
            {
                UnityEngine.Debug.Log("Nothing changed in the .gxl-file since saving your settings\n");
            }
        }

        /// <summary>
        /// Reloads the graph - and thus the nodetypes - depending on the objecttype of the specific AbstractSEECity object.
        /// </summary>
        /// <param name="city">The current city object- either a SEECityEvolution or an SEECity object</param>
        /// <returns> "true" - in case the reloaded graph is not null, else "false".
        private static bool ReloadGraphByCityType(AbstractSEECity city)
        {
            if (city is SEECityEvolution)
            {
                SEECityEvolution evoCity = (SEECityEvolution)city;

                evoCity.InspectSchema(evoCity.LoadFirstGraph());
                return (evoCity.LoadFirstGraph() != null);
            }
            else
            {
                SEECity seeCity = (SEECity)city;
                if (!File.Exists(seeCity.GXLPath.Path))
                {
                    Debug.LogError("The .gxl-file does not exist anymore in the given directory\n");
                    return false;
                }
                seeCity.LoadData();
                return (seeCity.LoadedGraph != null);
            }
        }

        /// <summary>
        /// Adds new Nodetypes to the current version of the city- if not already stored.
        /// </summary>
        /// <param name="city">the current city</param>
        /// <param name="newNodeTypes"> A list of strings which are added to the dictionary "SelectedNodetypes" - the types are per default selected, thus "true" </param>
        /// <returns>  
        private static void AddNodeTypes(AbstractSEECity city, List<string> newNodeTypes)
        {
            if (newNodeTypes != null)
            {
                foreach (string node in newNodeTypes)
                {
                    if (!(city.SelectedNodeTypes.Keys.Contains(node)))
                    {
                        city.SelectedNodeTypes.Add(node, true);
                    }
                }
            }
        }

        /// <summary>
        /// Returns either the GXL Directory of a SEECity or the SEECityEvolution object, depending on the specific
        /// type of the AbstractSEECity object.
        /// </summary>
        /// <param name="city">the current city</param>
        /// <returns>path = the name of the specific datapath, the .gxl file is saved into. 
        private static string getDataPath(AbstractSEECity city)
        {
            string path = null;
            if (city is SEECity)
            {
                SEECity seeCity = (SEECity)city;
                path = seeCity.GXLPath.Path;
            }
            else
            {
                SEECityEvolution sCityEvo = (SEECityEvolution)city;
                path = sCityEvo.GXLDirectory.Path;
            }
            return path;
        }
    }
}