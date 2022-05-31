// Copyright 2020 Lennart Kipka
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS
// BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR
// IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System.Collections.Generic;
using System.IO;
using SEE.Utils;
using UnityEngine;

namespace SEE.Game.City
{
    /// <summary>
    /// Configuration of a code city for the visualization of dynamic data in
    /// traced at the level of statements.
    /// Declared public because it will be used by editor code.
    /// /// </summary>
    public partial class SEEJlgCity : SEECity
    {
        /// IMPORTANT NOTE: If you add any attribute that should be persisted in a
        /// configuration file, make sure you save and restore it in
        /// <see cref="SEEJlgCity.Save(ConfigWriter)"/> and
        /// <see cref="SEEJlgCity.Restore(Dictionary{string,object})"/>,
        /// respectively. You should also extend the test cases in TestConfigIO.

        /// <summary>
        /// Path to the JLG file containing the runtime trace data.
        /// </summary>
        /// <returns>path of JLG file</returns>
        public FilePath JLGPath = new FilePath();

        /// <summary>
        /// Loads all city data as in <see cref="SEECity.LoadData()"/> plus the
        /// JLG tracing data.
        /// </summary>
        public override void LoadData()
        {
            base.LoadData();
            LoadJLG();
        }

        /// <summary>
        /// Loads the data from the given jlg file into a parsedJLG object and gives the object
        /// to a GameObject, that has a component to visualize it in the running game.
        /// </summary>
        private void LoadJLG()
        {
            string path = JLGPath.Path;

            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError("Path to JLG source file must not be empty.\n");
                enabled = false;
            }
            else if (!File.Exists(path))
            {
                Debug.LogErrorFormat("Source file does not exist at that path {0}.\n", path);
                enabled = false;
            }
        }

        //----------------------------------------------------------------------------
        // Input/output of configuration attributes
        //----------------------------------------------------------------------------

        /// <summary>
        /// Label for attribute <see cref="JLGPath"/> in configuration file.
        /// </summary>
        private const string JLGPathLabel = "JLGPath";

        /// <summary>
        /// Label for attribute <see cref="SEEJlgCity.BreakpointClass"/> in configuration file.
        /// </summary>
        private const string BreakpointClassLabel = "BreakpointClass";

        /// <summary>
        /// Label for attribute <see cref="SEEJlgCity.BreakpointLine"/> in configuration file.
        /// </summary>
        private const string BreakpointLineLabel = "BreakpointLine";

        /// <summary>
        /// Label for attribute <see cref="SEEJlgCity.DistanceAboveCity"/> in configuration file.
        /// </summary>
        private const string DistanceAboveCityLabel = "DistanceAboveCity";

        /// <summary>
        /// Label for attribute <see cref="SEEJlgCity.DistanceBehindCity"/> in configuration file.
        /// </summary>
        private const string DistanceBehindCityLabel = "DistanceBehindCity";

        /// <summary>
        /// Label for attribute <see cref="SEEJlgCity.LineWidth"/> in configuration file.
        /// </summary>
        private const string LineWidthLabel = "LineWidth";

        /// <summary>
        /// Label for attribute <see cref="SEEJlgCity.ShowOnlyCalls"/> in configuration file.
        /// </summary>
        private const string ShowOnlyCallsLabel = "ShowOnlyCalls";

        /// <summary>
        /// <see cref="City.AbstractSEECity.Save(ConfigWriter)"/>
        /// </summary>
        protected override void Save(ConfigWriter writer)
        {
            base.Save(writer);
            JLGPath.Save(writer, JLGPathLabel);
            // Configuration attributes relating to the animation
            writer.Save(BreakpointClass, BreakpointClassLabel);
            writer.Save(BreakpointLine, BreakpointLineLabel);
            writer.Save(DistanceAboveCity, DistanceAboveCityLabel);
            writer.Save(DistanceBehindCity, DistanceBehindCityLabel);
            writer.Save(LineWidth, LineWidthLabel);
            writer.Save(ShowOnlyCalls, ShowOnlyCallsLabel);
        }

        /// <summary>
        /// <see cref="City.AbstractSEECity.Restore(Dictionary{string, object})"/>.
        /// </summary>
        protected override void Restore(Dictionary<string, object> attributes)
        {
            base.Restore(attributes);
            JLGPath.Restore(attributes, JLGPathLabel);
            // Configuration attributes relating to the animation
            ConfigIO.Restore(attributes, BreakpointClassLabel, ref BreakpointClass);
            ConfigIO.Restore(attributes, BreakpointLineLabel, ref BreakpointLine);
            ConfigIO.Restore(attributes, DistanceAboveCityLabel, ref DistanceAboveCity);
            ConfigIO.Restore(attributes, DistanceBehindCityLabel, ref DistanceBehindCity);
            ConfigIO.Restore(attributes, LineWidthLabel, ref LineWidth);
            ConfigIO.Restore(attributes, ShowOnlyCallsLabel, ref ShowOnlyCalls);
        }
    }
}
