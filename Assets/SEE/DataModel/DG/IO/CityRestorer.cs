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
    /// A city stored in a json-formatted string.
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
        // as the user picks the directory via a directory picker/ the GUI , no specific error handling is needed at this point.
        string jsonString = File.ReadAllText(importPath);
        if (city is SEECityEvolution)
        {
            if (!VerifySEECityEvolution(importPath))
            {
                return;
            }
        }
        else
        {
            if (!VerifySEECity(importPath))
            {
                return;
            }
        }
        JsonUtility.FromJsonOverwrite(jsonString, city);
    }

    public static bool VerifySEECityEvolution(string jsonFile)
    {
        string jsonString = File.ReadAllText(jsonFile);
        if (jsonString.Contains("isAnSEECityEvolutionObject")){
            UnityEngine.Debug.Log("Loaded successfully\n");
            return true;
        }
        else
        {
            UnityEngine.Debug.Log("You can´t load the settings from a SEECity in the EvolutionScene!\n");
            return false;
        }
    }
    

    public static bool VerifySEECity(string jsonFile)
    {   
        string jsonString = File.ReadAllText(jsonFile);
        if (jsonString.Contains("isAnSEECityObject"))
        {
            UnityEngine.Debug.Log("Loaded successfully\n");
            return true;
        }
        else
        {
            UnityEngine.Debug.Log("You can´t load the settings from a SEECityEvolution in the MainScene!\n");
            return false;
        }
    }
}