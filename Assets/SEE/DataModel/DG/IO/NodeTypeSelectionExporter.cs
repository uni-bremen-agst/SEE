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
        UnityEngine.Debug.Log("Pfad: " + path);
        UnityEngine.Debug.Log("Name: " + savedProfile);
        if (path == null)
        {
            return false;
        }
    
        String directoryDelimiter = "/";
        string directory = path + directoryDelimiter;
        string nameOfGraph = graphName(pathPrefix);

        
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
            FileStream fs = new FileStream(directory + "ProfileSettings.csv", FileMode.OpenOrCreate, FileAccess.Write);
            fs.Close();

            StreamWriter sw = new StreamWriter(directory + "ProfileSettings.csv");

            sw.WriteLine(nameOfGraph + ",");
            sw.WriteLine("Number of all nodetypes of the graph" + "," + "\n" + nodeTypes.Count + ",");
            sw.Write("Number of selected nodetypes of the graph" + "," + "\n" + amountOfSelectedNodeTypes + "," + "\n");
            sw.Flush();
            sw.Close();

            System.IO.File.AppendAllLines(directory + "ProfileSettings.csv", selected);
            string test = LoadedGraphName(pathPrefix); 
            UnityEngine.Debug.Log("loadedGraph" + test);
            
        }
        catch (FileNotFoundException e)
        {
            return false;
        }
        return true;
    }

    public static bool readProfiles (String fileName)
    {
        if (fileName == null || !(compareFile(fileName)) )
        {
            return false; 
        }
        return setNodetypes(); 
    }
        

    public static  bool setNodetypes()
    {
        return false; 
    }

    public static bool compareFile(string pathPrefix)
    {
        string localGraphname = graphName(pathPrefix);
        string loadedGraphName = LoadedGraphName(pathPrefix);

        if (localGraphname.Equals(loadedGraphName))
        {
            return true; 
        }
        return false; 
    }
    
    public static string graphName(string path)
    {   
        IEnumerable<string> gxlFileName = Filenames.GXLFilenames(path);
        gxlFileName.ToList();
        
        //Dissect the exact name of the given graph without the specific user directory it is stored in 
        //and store it in nameOfGraph
        StringBuilder strBuilder = new StringBuilder(gxlFileName.ElementAt(0));
        string nameOfGraph = strBuilder.ToString();

        return nameOfGraph; 
    }

    public static string LoadedGraphName(string fileName)       
    {  
        string loreIpsum = fileName + "/ProfileSettings.csv";
        UnityEngine.Debug.Log("fileName ist " + fileName) ;
        string storedGraph = null;
        try
        {
            FileStream fs = new FileStream(loreIpsum, FileMode.OpenOrCreate, FileAccess.Write); // hardCoded fileName fehlt 
            fs.Close();
            StreamReader strReader = new StreamReader(loreIpsum);
            storedGraph = strReader.ReadLine();
           
        }
        catch (FileNotFoundException e)
        {
            UnityEngine.Debug.LogError("No file found");
        }
        //Cut the name of the specific directory the GXL is saved in and the delimiter, i.e. the last index
        storedGraph = storedGraph.Remove(0, fileName.Length);
        storedGraph = storedGraph.Remove(0, 1);
        storedGraph = storedGraph.Remove(storedGraph.Length-1, 1);

        return storedGraph;
    }
}
