using SEE.DataModel.DG;
using System.IO;
using UnityEngine;

namespace Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions
{
    /// <summary>
    /// Default Class used by the <see cref="LanguageAttract"/> function object to read source code regions from nodes.
    /// </summary>
    public class NodeRegionReader : INodeReader
    {
        /// <summary>
        /// Attribute used to retrieve the start line of a source code region
        /// </summary>
        private static readonly string SourceStartLine = "SourceRange_StartLine";

        /// <summary>
        /// Attribute used to retrieve the end line of a source code region
        /// </summary>
        private static readonly string SourceStartLength = "SourceRange_EndLine";

        /// <summary>
        /// Reads the source code region from a Node .
        /// </summary>
        /// <param name="node">Given node</param>
        /// <returns>Source code region as a string</returns>
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