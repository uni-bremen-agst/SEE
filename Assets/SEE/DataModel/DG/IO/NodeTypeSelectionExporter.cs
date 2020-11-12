using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using System.Text;
using SEE.DataModel.DG.IO;
using SEE;
using SEE.Utils;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class NodeTypeSelectionExporter
{


    public static bool Persist(string pathPrefix,int allNodes, Dictionary<string, bool> nodeTypes, string path)
    {
        UnityEngine.Debug.Log("Pfad: " + path);
        GraphsReader gr = new GraphsReader();
        IEnumerable<string> gxlFileName = Filenames.GXLFilenames(pathPrefix);
        gxlFileName.ToList();

        //Dissect the exact name of the given graph without the specific user directory it is stored in 
        //and store it in nameOfGraph
        StringBuilder strBuilder = new StringBuilder(gxlFileName.ElementAt(0));
        strBuilder.Remove(0, pathPrefix.Length + 1);
        string nameOfGraph = strBuilder.ToString();

        //Filter every specific nodetype the user has selected before
        ICollection<string> matches = nodeTypes.Where(pair => pair.Value == true)
              .Select(pair => pair.Key).ToList();
        List<string> selected = new List<string>();

        if (matches.Count == 0 || matches.Count == allNodes)
        {
            UnityEngine.Debug.Log("No comprehensable selection to be stored"); //Fehlerbehandlung
            return false;
        }


        for (int i = 0; i < matches.Count; i++)
        {
            string s = matches.ElementAt(i);
            selected.Insert(i, s + ",");

        }

        int amountOfSelectedNodeTypes = matches.Count;

        try
        {

            FileStream fs = new FileStream(pathPrefix + "/ProfileSettings.txt", FileMode.OpenOrCreate, FileAccess.Write);
            fs.Close();

            StreamWriter sw = new StreamWriter(pathPrefix + "/ProfileSettings.csv");

            sw.WriteLine(nameOfGraph + ",");
            sw.WriteLine("Number of all nodetypes of the graph" + "," + "\n" + allNodes + ",");
            sw.Write("Number of selected nodetypes of the graph" + "," + "\n" + amountOfSelectedNodeTypes + "," + "\n");
            sw.Flush();
            sw.Close();

            System.IO.File.AppendAllLines(pathPrefix + "/ProfileSettings.csv", selected);

        }
        catch (Exception e)
        {

        }

        StreamReader strReader = new StreamReader(pathPrefix + "/ProfileSettings.csv");
        while (strReader.ReadLine() != null)
        {

        }

        string text = System.IO.File.ReadAllText(pathPrefix + "/ProfileSettings.csv");
        UnityEngine.Debug.Log(text);

        return true;
    }
}
