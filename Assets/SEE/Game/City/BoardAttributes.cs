using System;
using System.Collections.Generic;
using SEE.Game.HolisticMetrics;
using SEE.Utils.Paths;
using SEE.Utils.Config;
using Sirenix.OdinInspector;
using UnityEngine;

namespace SEE.Game.City
{
    /// <summary>
    /// Attributes relating to the holistic metrics boards.
    /// </summary>
    [Serializable]
    public class BoardAttributes : VisualAttributes
    {
        [Tooltip("Whether a holistic metric board shall be loaded on startup.")]
        public bool LoadBoardOnStartup;

        [SerializeField, Tooltip("Path to the board that shall be loaded."), ShowIf(nameof(LoadBoardOnStartup))]
        public DataPath BoardPath = new();

        /// <summary>
        /// Loads the board specified at <see cref="BoardPath"/> if
        /// <see cref="LoadBoardOnStartup"/> is true.
        /// </summary>
        public void LoadBoard()
        {
            if (LoadBoardOnStartup)
            {
                BoardsManager.Create(ConfigManager.LoadBoard(BoardPath));
            }
        }

        /// <summary>
        /// Saves the board attributes using <paramref name="writer"/> under the
        /// given <paramref name="label"/>.
        /// </summary>
        /// <param name="writer">writer to be used to write the board</param>
        /// <param name="label">the label under which to store the attributes</param>
        public override void Save(ConfigWriter writer, string label)
        {
            writer.BeginGroup(label);
            writer.Save(LoadBoardOnStartup, loadBoardOnStartupLabel);
            BoardPath.Save(writer, boardPathLabel);
            writer.EndGroup();
        }

        /// <summary>
        /// Restores the attributes of the board from <paramref name="attributes"/> under
        /// the key <paramref name="label"/>.
        /// </summary>
        /// <param name="attributes">saved configuration attributes from which to retrieve the
        /// board attributes</param>
        /// <param name="label">the label under which to look up the attributes</param>
        public override void Restore(Dictionary<string, object> attributes, string label)
        {
            if (attributes.TryGetValue(label, out object dictionary))
            {
                Dictionary<string, object> values = dictionary as Dictionary<string, object>;
                ConfigIO.Restore(attributes, loadBoardOnStartupLabel, ref LoadBoardOnStartup);
                BoardPath.Restore(values, boardPathLabel);
            }
        }

        /// <summary>
        /// Label for <see cref="BoardPath"/> in the configuration file.
        /// </summary>
        private const string boardPathLabel = "BoardPath";

        /// <summary>
        /// Label for <see cref="LoadBoardOnStartup"/> in the configuration file.
        /// </summary>
        private const string loadBoardOnStartupLabel = "LoadBoardOnStartup";
    }
}