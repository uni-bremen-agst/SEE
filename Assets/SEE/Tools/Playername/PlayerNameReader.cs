using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;


class PlayerNameReader
{
    private static string jsonFilePath = Application.dataPath + "/StreamingAssets/playername.json";
    /// <summary>
    /// Reads the playername which is in the playername.json
    /// </summary>
    /// <returns>Return null if an error occurs</returns>
    public static string ReadPlayerName()
    {
        try
        {
            string jsonContent = File.ReadAllText(jsonFilePath);
            PlayerData playerData = JsonConvert.DeserializeObject<PlayerData>(jsonContent);
            return playerData.playerName;
        }
        catch (FileNotFoundException)
        {
            Debug.LogError("File not found: " + jsonFilePath);
        }
        catch (JsonReaderException)
        {
            Debug.LogError("Invalid JSON format in file: " + jsonFilePath);
        }
        catch (Exception ex)
        {
            Debug.LogError("Error occurred: " + ex.Message);
        }

        return null;
    }

    /// <summary>
    /// Create the json-file with playername.
    /// </summary>
    /// <param name="playerName">Playername which was typed in the user settings</param>
    public static void CreatePlayerJson(string playerName)
    {
        try
        {
            PlayerData playerData = new() { playerName = playerName };
            string jsonContent = JsonConvert.SerializeObject(playerData);
            File.WriteAllText(jsonFilePath, jsonContent);
        }
        catch (Exception ex)
        {
            Debug.LogError("Error occurred: " + ex.Message);
        }
    }

    // Class representing the structure of JSON data
    private class PlayerData
    {
        public string playerName { get; set; }
    }
}

