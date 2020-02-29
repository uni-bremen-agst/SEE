//Copyright 2020 Florian Garbade

//Permission is hereby granted, free of charge, to any person obtaining a
//copy of this software and associated documentation files (the "Software"),
//to deal in the Software without restriction, including without limitation
//the rights to use, copy, modify, merge, publish, distribute, sublicense,
//and/or sell copies of the Software, and to permit persons to whom the Software
//is furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in
//all copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
//INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
//PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
//LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE
//USE OR OTHER DEALINGS IN THE SOFTWARE.

using UnityEngine;

namespace SEE.Animation.Internal
{
    /// <summary>
    /// Extension for GraphSettings to simplify the use in Animation.
    /// </summary>
    public static class GraphSettingsExtension
    {
        /// <summary>
        /// Returns a GraphSettings instance with the default settings for usage in Animation.
        /// </summary>
        /// <returns>An initialized GraphSettings instance.</returns>
        public static GraphSettings DefaultAnimationSettings(string gxlFolderName)
        {
            var graphSettings = new GraphSettings();

            graphSettings.pathPrefix = Application.dataPath.Replace('/', '\\') + '\\';

            graphSettings.gxlPath = $"..\\Data\\GXL\\{gxlFolderName}\\";

            return graphSettings;
        }
    }
}
