using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;
using SEE.DataModel.DG.IO;
using SEE.Game;
using SEE.Utils;
using System.Linq;
using System.Diagnostics;
using System.Dynamic;
using Newtonsoft.Json;
using System; 

/// <summary>
/// This class is responsible for the export and the restore of either a SEECity or a SEECityEvolution object. 
/// The required data is stored in a .json-file.
/// The object stores values and selections such as specific metrics of a city or selected nodetypes as well which were chosen  
/// by the user before saving his or her profile.
/// </summary>
public class CityRestorer
{
    /// <summary>
    /// The settings of a city stored in a json-formatted string.
    /// </summary>
    private static string json = "";

    /// <summary>
    ///  Converts the <paramref name="city"/> in a json-formatted string and saves this string in a .json-file in the directory
    ///  <paramref name="path"/>.
    /// </summary>
    /// <param name="path"> The directory in which the json-file will be saved </param>
    /// <param name="city"> The city which will be stored in the json-file</param>
    public static void Persist(string path, AbstractSEECity city)
    {
        json = JsonUtility.ToJson(city,true);
        string extension = ".json";
        string dataPath = path + extension;
        System.IO.File.WriteAllText(dataPath, json);
        UnityEngine.Debug.Log("Export sucessfully\n");
    }

    /// <summary>
    /// Loads a city from the given <paramref name="importPath"/> and overwrites the <paramref name="city"/>
    /// </summary>
    /// <param name="importPath"> The given json-file-path </param>
    /// <param name="city"> The city which is to be overwritten </param>
    public static void RestoreCity(string importPath, AbstractSEECity city)
    {
        // We have to store the current enumeration of the nodetypes of the current version in order to compare 
        // it afterwards with the stored one.
        List<string> newNodeTypes = city.SelectedNodeTypes.Keys.ToList();

        // as the user picks the directory via a directory picker/ the GUI , no specific error handling is needed at this point.
        string jsonString = File.ReadAllText(importPath);
        if(VerifyCityType(importPath, city))
        {
            JsonUtility.FromJsonOverwrite(jsonString, city);
        }
        Dictionary<string, bool> oldNodetypes = city.SelectedNodeTypes; 
        differentNodeTypes(oldNodetypes, jsonString, newNodeTypes);
    }

    /// <summary>
    /// Veryfies wether the types of the city and the json-file are matching.
    /// </summary>
    /// <param name="jsonFile">the .json-file with the settings for the city</param>
    /// <param name="city">the city, which has to be overwritten</param>
    /// <returns>true, if the types are matching, otherwise false</returns>
    public static bool VerifyCityType(string jsonFile, AbstractSEECity city)
    {
        string jsonString = File.ReadAllText(jsonFile);
        if (jsonString.Contains("isAnSEECityObject") && city is SEECity)
        {
            UnityEngine.Debug.Log("Loaded successfully\n");
            return true;
        }
        if (jsonString.Contains("isAnSEECityEvolutionObject") && city is SEECityEvolution)
        {
            UnityEngine.Debug.Log("Loaded successfully\n");
            return true;
        }
        else
        {
            UnityEngine.Debug.LogErrorFormat("The types of the scene and the loaded .json-file are not matching\n");
            return false;
        }
    }

    /// <summary>
    /// Analyazes if there is a difference between the stored nodetypes and the current nodetypes.
    /// </summary>
    /// <param name="jsonFile">the .json-file with the settings for the city</param>
    /// <param name="oldNodeTypes>a dictionary of the stored nodeTypes</param>
    /// <param name="newNodes> the city, which has to be overwritten</param>
    /// <returns> nothing, except a DebugLog to inform the user in case of any changes regarding the nodetypes.
    public static void differentNodeTypes(Dictionary<string, bool> oldNodeTypes, string jsonFile, List<string> newNodes)
    { 
        List<string> oldNodes = oldNodeTypes.Keys.ToList(); 
        List<string> finalResults = new List<string>();
        string difference = "";
  
            // In order to build the "real" difference between the stored parameters and the current one , we have to remove the duplicates.
            // and concatenate the result afterwards in the list finalResults.
            // The result will show if either there are new nodetypes in the latest version of the specific city or 
            // if there are nodetypes stored which are not in the current version of the Evolutioncity anymore.
        if (newNodes.Count() < oldNodes.Count())
        {
            for (int i = 0; i < oldNodeTypes.Count(); i++)
            {
                if (newNodes.Contains(oldNodes[i]))
                {
                    oldNodes.Remove(oldNodes[i]);
                    newNodes.Remove(oldNodes[i]);
                    i--;
                }
            }
        }
        else if ((newNodes.Count()) >= (oldNodes.Count()))
        {
            for (int j = 0; j < newNodes.Count(); j++)
            {
                if (oldNodes.Contains(newNodes[j]))
                {
                    oldNodes.Remove(newNodes[j]);
                    newNodes.Remove(newNodes[j]);
                    j--;
                }
            }
        }
        finalResults = newNodes.Concat(oldNodes).ToList(); 

            // Depending on the actual amount of difference, three cases are possible:
            // First case = There are no differenct objects, thus we can break instantly
            // Second case = There is only one object, so it is possible to print out the result instantly and break.
            // Third case = There are more then two objects which have changed and we have to enumerate them and consequently cut the string for the 
            // printing-log to get a neat output for the user.

            // First case
            if (finalResults.Count == 0)
            {
                UnityEngine.Debug.Log("There are no new nodytpes added since you saved your profile");
                return;
                // Second case 
            }
            else if (finalResults.Count == 1)
            {   if(oldNodes.Count==1)
            {
                UnityEngine.Debug.Log("Since you saved your profile the following Nodetype was deleted in the meantime :\n" + finalResults.First());
                return;
            }   if(newNodes.Count == 1 )
            {
                UnityEngine.Debug.Log("Since you saved your profile the following Nodetype was added in the meantime :\n" + finalResults.First());
                return;
            }
                
            } // Third case
            else
            {
                foreach (string str in finalResults)
                {
                    difference += str + ",";
                }
            }
            difference = difference.Substring(0, difference.Count());
            UnityEngine.Debug.Log("Since you saved your profile and today the following Nodetypes have changed :\n" + difference);
        }
    
}