using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class StudyDataManager
{
    private static string SavePath =>
        Path.Combine(Application.persistentDataPath, "studydata.json");
  public  class StudyData
    {
       public string highlightedBlockName;
        public String date;
        public int groupID;
    }

    public static void SaveAppend(List<StudyData> newList)
    {
        List<StudyData> existingList = new List<StudyData>();

        if (File.Exists(SavePath))
        {
            string oldJson = File.ReadAllText(SavePath);
            existingList = JsonConvert.DeserializeObject<List<StudyData>>(oldJson) ?? new List<StudyData>();
        }

        // neue Daten anhängen
        existingList.AddRange(newList);

        // alles speichern
        string json = JsonConvert.SerializeObject(existingList, Formatting.Indented);
        File.WriteAllText(SavePath, json);

        Debug.Log("Saved (appended): " + SavePath);
    }


    public static List<StudyData> Load()
    {
        if (!File.Exists(SavePath))
        {
            Debug.LogWarning("No file found. Returning empty list.");
            return new List<StudyData>();
        }

        string json = File.ReadAllText(SavePath);
        return JsonConvert.DeserializeObject<List<StudyData>>(json);
    }
}

