using System;
using System.Collections.Generic;
using SEE.Game.HolisticMetrics;
using SEE.Utils;
using Sirenix.OdinInspector;
using UnityEngine;

namespace SEE.Game.City
{
    /// <summary>
    /// Attributes relating to the holistic metrics boards.
    /// </summary>
    [Serializable]
    public class BoardAttributes: VisualAttributes
    {
        [Tooltip("Whether a holistic metric board shall be loaded on startup.")]
        public bool LoadBoardOnStartup;
        
        [SerializeField, Tooltip("Path to the board that shall be loaded."), ShowIf(nameof(LoadBoardOnStartup))]
        public FilePath BoardPath = new FilePath();

        /// <summary>
        /// Loads the board specified at <see cref="BoardPath"/> if
        /// <see cref="LoadBoardOnStartup"/> is true.
        /// </summary>
        public void LoadBoard()
        {
            if (LoadBoardOnStartup)
            {
                BoardConfig boardConfiguration = ConfigManager.LoadBoard(BoardPath);
                BoardsManager.Create(boardConfiguration);
            }
        }
        
        public override void Save(ConfigWriter writer, string label)
        {
            writer.BeginGroup(label);
            BoardPath.Save(writer, BoardPathLabel);
            writer.EndGroup();
        }

        public override void Restore(Dictionary<string, object> attributes, string label)
        {
            if (attributes.TryGetValue(label, out object dictionary))
            {
                Dictionary<string, object> values = dictionary as Dictionary<string, object>;
                
                ConfigIO.Restore(values, BoardPathLabel, ref BoardPath);
            }
        }

        private const string BoardPathLabel = "BoardPath";
    }
}