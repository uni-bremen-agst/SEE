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
using Cysharp.Threading.Tasks;
using SEE.Utils.Paths;
using SEE.Utils.Config;
using Sirenix.OdinInspector;
using UnityEngine;

namespace SEE.Game.City
{
    /// <summary>
    /// Configuration of a code city for the visualization of dynamic data in
    /// traced at the level of statements.
    /// Declared public because it will be used by editor code.
    /// </summary>
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
        [ShowInInspector, Tooltip("Path of JLG file"), FoldoutGroup(DataFoldoutGroup)]
        public DataPath JLGPath = new();

        /// <summary>
        /// Loads all city data as in <see cref="SEECity.LoadDataAsync"/> plus the
        /// JLG tracing data.
        /// </summary>
        /// <returns>True if the menus need to be adjusted; otherwise, false.</returns>
        [Button(ButtonSizes.Small, Name = "Load Data")]
        [ButtonGroup(DataButtonsGroup)]
        [PropertyOrder(DataButtonsGroupOrderLoad)]
        public override async UniTask<bool> LoadDataAsync()
        {
            await base.LoadDataAsync();
            LoadJLG();
            return false;
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
                Debug.LogError($"Source file does not exist at that path {path}.\n");
                enabled = false;
            }
        }

        #region Config I/O

        //----------------------------------------------------------------------------
        // Input/output of configuration attributes
        //----------------------------------------------------------------------------

        /// <summary>
        /// Label for attribute <see cref="JLGPath"/> in configuration file.
        /// </summary>
        private const string jlgPathLabel = "JLGPath";

        /// <summary>
        /// Label for attribute <see cref="SEEJlgCity.BreakpointClass"/> in configuration file.
        /// </summary>
        private const string breakpointClassLabel = "BreakpointClass";

        /// <summary>
        /// Label for attribute <see cref="SEEJlgCity.BreakpointLine"/> in configuration file.
        /// </summary>
        private const string breakpointLineLabel = "BreakpointLine";

        /// <summary>
        /// Label for attribute <see cref="SEEJlgCity.DistanceAboveCity"/> in configuration file.
        /// </summary>
        private const string distanceAboveCityLabel = "DistanceAboveCity";

        /// <summary>
        /// Label for attribute <see cref="SEEJlgCity.DistanceBehindCity"/> in configuration file.
        /// </summary>
        private const string distanceBehindCityLabel = "DistanceBehindCity";

        /// <summary>
        /// Label for attribute <see cref="SEEJlgCity.LineWidth"/> in configuration file.
        /// </summary>
        private const string lineWidthLabel = "LineWidth";

        /// <summary>
        /// Label for attribute <see cref="SEEJlgCity.ShowOnlyCalls"/> in configuration file.
        /// </summary>
        private const string showOnlyCallsLabel = "ShowOnlyCalls";

        /// <summary>
        /// <see cref="City.AbstractSEECity.Save(ConfigWriter)"/>
        /// </summary>
        protected override void Save(ConfigWriter writer)
        {
            base.Save(writer);
            JLGPath.Save(writer, jlgPathLabel);
            // Configuration attributes relating to the animation
            writer.Save(BreakpointClass, breakpointClassLabel);
            writer.Save(BreakpointLine, breakpointLineLabel);
            writer.Save(DistanceAboveCity, distanceAboveCityLabel);
            writer.Save(DistanceBehindCity, distanceBehindCityLabel);
            writer.Save(LineWidth, lineWidthLabel);
            writer.Save(ShowOnlyCalls, showOnlyCallsLabel);
        }

        /// <summary>
        /// <see cref="City.AbstractSEECity.Restore(Dictionary{string, object})"/>.
        /// </summary>
        protected override void Restore(Dictionary<string, object> attributes)
        {
            base.Restore(attributes);
            JLGPath.Restore(attributes, jlgPathLabel);
            // Configuration attributes relating to the animation
            ConfigIO.Restore(attributes, breakpointClassLabel, ref BreakpointClass);
            ConfigIO.Restore(attributes, breakpointLineLabel, ref BreakpointLine);
            ConfigIO.Restore(attributes, distanceAboveCityLabel, ref DistanceAboveCity);
            ConfigIO.Restore(attributes, distanceBehindCityLabel, ref DistanceBehindCity);
            ConfigIO.Restore(attributes, lineWidthLabel, ref LineWidth);
            ConfigIO.Restore(attributes, showOnlyCallsLabel, ref ShowOnlyCalls);
        }
        #endregion
    }
}
