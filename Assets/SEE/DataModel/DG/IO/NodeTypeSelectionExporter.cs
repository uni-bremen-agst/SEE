using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using System.Text;
using SEE.DataModel.DG.IO;
using SEE;
using SEE.Game;
using SEE.Utils;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Dynamic;
using Newtonsoft.Json;

public  class NodeTypeSelectionExporter
{
    private static string json = "";

    public static bool Persist(string pathPrefix, Dictionary<string, bool> nodeTypes, string path, string savedProfile, AbstractSEECity city)
    {
        if(savedProfile == null)
        {
            UnityEngine.Debug.LogError("There is no filename given");
            return false;
        }
        json = JsonUtility.ToJson(city,true);
        AddNodeTypes(nodeTypes);
        string dataPath = path + "/" + savedProfile + ".json";
        if (File.Exists(dataPath))
        {
            UnityEngine.Debug.LogError("There already exists a file with this filename in the given directory");
            return false;
        }
        else
        {
            System.IO.File.WriteAllText(dataPath, json);
            UnityEngine.Debug.Log("Export sucessfully\n");
            return true;
        }
    }


    private static void AddNodeTypes(Dictionary<string,bool> nodeTypes)
    {
        JsonSerializer serializer = new JsonSerializer();

        StringBuilder sb = new StringBuilder();
        StringWriter swr = new StringWriter(sb);
        JsonWriter writer = new JsonTextWriter(swr);

        writer.Formatting = Formatting.Indented;
        writer.WriteStartObject();

        foreach (KeyValuePair<string, bool> nodeType in nodeTypes)
        {
            writer.WritePropertyName(nodeType.Key);
            writer.WriteValue(nodeType.Value);
        }

        writer.WriteEndObject();
        json += sb.ToString();
    }
   
    public static void RestoreData(string importPath)
    {

    }

}