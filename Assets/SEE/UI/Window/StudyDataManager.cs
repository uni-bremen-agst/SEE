using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class StudyDataManager
{
    private static string SavePathSelect =>
        Path.Combine(Application.dataPath, "studydataSelect.json");
    private static string SavePathHighlighted =>
       Path.Combine(Application.dataPath, "studydataHighlighted.json");

    enum LoggerTypes
    {
        Highlighted,
        SelectedObjekt
    };
    public class StudyDataBasic
    {
        public String date;
        public string Blockname;
    }

    public class Highlighted : StudyDataBasic
    {
        LoggerTypes LoggerTypes = LoggerTypes.Highlighted;

        public int groupID;
    }
    public class Selcted : StudyDataBasic
    {
        LoggerTypes LoggerTypes = LoggerTypes.SelectedObjekt;
        public bool isSelectAction;
    }


    public static void SaveAppend(List<Selcted> newList)
    {
        List<Selcted> existingList = new List<Selcted>();

        if (File.Exists(SavePathSelect))
        {
            string oldJson = File.ReadAllText(SavePathSelect);
            existingList = JsonConvert.DeserializeObject<List<Selcted>>(oldJson) ?? new List<Selcted>();

        }

        // neue Daten anhängen
        existingList.AddRange(newList);

        // alles speichern
        string json = JsonConvert.SerializeObject(existingList, Formatting.Indented);
        File.WriteAllText(SavePathSelect, json);

        Debug.Log("Saved (appended): " + SavePathSelect);
    }

    public static void SaveAppend(List<Highlighted> newList)
    {
        List<Highlighted> existingList = new List<Highlighted>();

        if (File.Exists(SavePathHighlighted))
        {
            string oldJson = File.ReadAllText(SavePathHighlighted);
            existingList = JsonConvert.DeserializeObject<List<Highlighted>>(oldJson) ?? new List<Highlighted>();
        }

        // neue Daten anhängen
        existingList.AddRange(newList);

        // alles speichern
        string json = JsonConvert.SerializeObject(existingList, Formatting.Indented);
        File.WriteAllText(SavePathHighlighted, json);

        Debug.Log("Saved (appended): " + SavePathHighlighted);
    }

    //public static List<StudyData> Load()
    //{
    //    if (!File.Exists(SavePath))
    //    {
    //        Debug.LogWarning("No file found. Returning empty list.");
    //        return new List<StudyData>();
    //    }

    //    string json = File.ReadAllText(SavePath);
    //    return JsonConvert.DeserializeObject<List<StudyData>>(json);
    //}
}

