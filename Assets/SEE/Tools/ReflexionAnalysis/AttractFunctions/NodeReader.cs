using SEE.DataModel.DG;
using System.IO;
using UnityEngine;

namespace Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions
{
    public class NodeReader : INodeReader
    {
        private static readonly string SourceStartLine = "SourceRange_StartLine";
        
        private static readonly string SourceStartLength = "SourceRange_EndLine";

        public string ReadRegion(Node node)
        {
            string fileName;
            int sourceRegionStart;
            int sourceColumn;
            int sourceRegionEnd;
            string region = string.Empty;

            if (!(node.IntAttributes.ContainsKey(SourceStartLine)
               && node.IntAttributes.ContainsKey(SourceStartLength)))
            {
                return region;
            }

            try
            {
                sourceColumn = node.StringAttributes.ContainsKey("Source.Column") ? node.GetInt("Source.Column") : 1;

                sourceRegionStart = node.GetInt(SourceStartLine);

                // TODO: Is this length to be interpreted in lines?
                int sourceRegionLength = node.GetInt(SourceStartLength);
                sourceRegionEnd = sourceRegionStart + sourceRegionLength;

                fileName = node.AbsolutePlatformPath();

                // TODO: Is it possible to improve efficiency?
                using (StreamReader streamReader = new StreamReader(new FileStream(fileName, FileMode.Open, FileAccess.Read)))
                {
                    for (int i = 1; i < sourceRegionStart; i++)
                    {
                        streamReader.ReadLine();
                    }

                    region = streamReader.ReadLine().Substring(sourceColumn - 1);

                    for (int i = sourceRegionStart + 1; i < sourceRegionEnd; i++)
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
}