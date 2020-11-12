using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System.IO;

public class NodeTypeExporter
{ 

    
    public static void WriteFile(string filename, Dictionary<string,bool> nodeTypes, char separator)
    {
        StreamWriter outputFile = new StreamWriter("test");
        StringBuilder sb = new StringBuilder();
        foreach(KeyValuePair<string,bool> nodeType in nodeTypes)
        {
            sb.AppendLine(nodeType.Key + separator + nodeType.Value + separator);
        }
        UnityEngine.Debug.Log("Done");
        outputFile.WriteLine(sb.ToString());

    }
}
