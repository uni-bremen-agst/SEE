using SEE.DataModel.DG;
using System.IO;
using UnityEngine;

public class NodeRegionReader
{
    public static string ReadRegion(Node node)
    {
        string fileName;
        int sourceRegionStart;
        int sourceColumn;
        int sourceRegionEnd;
        string region = string.Empty;

        if(!(node.StringAttributes.ContainsKey("Source.Region_Start") 
           && node.StringAttributes.ContainsKey("Source.Region_Length")))
        {
            return region;
        }

        try
        {
            // TODO: Distinguish between Source.Line and Source.Region_Start?
            //int sourceLine = node.GetInt("Source.Line");
            sourceColumn = node.StringAttributes.ContainsKey("Source.Column") ? node.GetInt("Source.Column") : 1;

            sourceRegionStart = node.GetInt("Source.Region_Start");

            // TODO: Is this length to be interpreted in lines?
            int sourceRegionLength = node.GetInt("Source.Region_Length");

            sourceRegionEnd = sourceRegionStart + sourceRegionLength;

            string sourcePath = node.GetString("Source.Path");

            string sourceFileName = node.GetString("Source.File");

            fileName = System.IO.Path.Combine(sourcePath, sourceFileName);

            // TODO: Is it possible to improve efficiency?
            using (StreamReader streamReader = new StreamReader(new FileStream(fileName, FileMode.Open, FileAccess.Read)))
            {
                for(int i = 1; i < sourceRegionStart; i++)
                {
                    streamReader.ReadLine();
                }

                region = streamReader.ReadLine().Substring(sourceColumn - 1);

                for(int i = sourceRegionStart + 1; i < sourceRegionEnd; i++)
                {
                    region += streamReader.ReadLine();
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to read code region of node {node.ID}:" + e.Message);
            return string.Empty;
        }

        return region;
    }
}
