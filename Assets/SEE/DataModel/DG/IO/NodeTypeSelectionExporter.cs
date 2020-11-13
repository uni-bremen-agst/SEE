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
using System.Diagnostics;
using System.Dynamic;

public class NodeTypeSelectionExporter
{
    private Dictionary<string, bool> nodeTypes = new Dictionary<string, bool>();

    public static bool Persist(string pathPrefix, Dictionary<string, bool> nodeTypes, string path, string savedProfile)
    {
        //pathPrefix is set before and thus not verified again
        if (savedProfile == null || savedProfile.Equals("") || !(Directory.Exists(path)))
        {
            return false;
        }

        if (path == null)
        {
            return false;
        }

        string csvExtension = ".csv";
        string directoryDelimiter = "/";
        string directory = path + directoryDelimiter;
        string nameOfGraph = storedGraphName(pathPrefix);
        string fileName = path + directoryDelimiter + savedProfile + csvExtension;
        UnityEngine.Debug.Log("fileName" + fileName);

        //Filter every specific nodetype the user has selected before
        ICollection<string> matches = nodeTypes.Where(pair => pair.Value == true)
              .Select(pair => pair.Key).ToList();
        List<string> selected = new List<string>();

        if (matches.Count == 0 || matches.Count == nodeTypes.Count)
        {
            UnityEngine.Debug.LogError("No comprehensable selection to be stored");
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
            FileStream fs = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.Write);
            fs.Close();

            StreamWriter sw = new StreamWriter(fileName);

            sw.WriteLine(nameOfGraph + ",");
            sw.WriteLine("Number of all nodetypes of the graph" + "," + "\n" + nodeTypes.Count + ",");
            sw.Write("Number of selected nodetypes of the graph" + "," + "\n" + amountOfSelectedNodeTypes + "," + "\n");
            sw.Flush();
            sw.Close();

            System.IO.File.AppendAllLines(fileName, selected);


        }
        catch (FileNotFoundException e)
        {
            return false;
        }
        return true;
    }

    public static bool readProfiles(String fileName)
    {
        if (fileName == null || !(compareFile(fileName)))
        {
            return false;
        }

        Dictionary<string, bool> allTypes = new Dictionary<string, bool>();
        List<string> relevantNodeTypes = new List<string>();
        try
        {
            FileStream fs = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.Write);
            fs.Close();
            StreamReader strReader = new StreamReader(fileName);
            while (!(strReader.ReadLine() == null))
            {
                //parsieren- first line Name 2nd ... Ab Line 6 Die Nodetypes ... 
            }
        }
        catch (FileNotFoundException)
        {
            //
            return false;
        }


        return setNodetypes(allTypes, relevantNodeTypes);
    }


    public static bool setNodetypes(Dictionary<string, bool> nodeTypes, List<string> relevantNodeTypes)
    {
        //iterieren , in case false setzen...
        return false;
    }

    public static bool compareFile(string pathPrefix)
    {
        string localGraph = storedGraphName(pathPrefix);
        string loadedGraph = loadedGraphName(pathPrefix);

        if (localGraph.Equals(loadedGraph))
        {
            return true;
        }
        return false;
    }

    public static string storedGraphName(string path)
    {
        IEnumerable<string> gxlFileName = Filenames.GXLFilenames(path);
        gxlFileName.ToList();

        //Dissect the exact name of the given graph without the specific user directory it is stored in 
        StringBuilder strBuilder = new StringBuilder(gxlFileName.ElementAt(0));
        string nameOfGraph = strBuilder.ToString();

        UnityEngine.Debug.Log("in storedGraphName" + nameOfGraph);
        return nameOfGraph;
    }

    public static string loadedGraphName(string fileName)
    {
        
        
        string storedGraph = null;
        try
        {
            FileStream fs = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.Write); // hardCoded fileName fehlt 
            fs.Close();
            StreamReader strReader = new StreamReader(fileName);
            storedGraph = strReader.ReadLine();

        }
        catch (FileNotFoundException e)
        {
            UnityEngine.Debug.LogError("No file found");
        }
        //Cut the name of the specific directory the GXL is saved in and the delimiter, i.e. the last index
        storedGraph = storedGraph.Remove(0, fileName.Length);
        storedGraph = storedGraph.Remove(0, 1);
        storedGraph = storedGraph.Remove(storedGraph.Length - 1, 1);

        return storedGraph;
    }
}
