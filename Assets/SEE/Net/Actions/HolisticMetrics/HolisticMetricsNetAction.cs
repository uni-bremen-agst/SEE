using System;
using SEE.Game.HolisticMetrics;
using SEE.Game.HolisticMetrics.Components;

namespace SEE.Net.Actions.HolisticMetrics
{
    /// <summary>
    /// The base class for all holistic metrics net actions. This contains some common code.
    /// </summary>
    public abstract class HolisticMetricsNetAction : AbstractNetAction
    {
        /// <summary>
        /// This method tries to find a <see cref="WidgetsManager"/> by its name.
        /// </summary>
        /// <param name="boardName">The name of the board of which the <see cref="WidgetsManager"/> should be
        /// retrieved.</param>
        /// <returns>The <see cref="WidgetsManager"/> of the board with the given name</returns>
        /// <exception cref="Exception">Thrown when no <see cref="WidgetsManager"/> was found</exception>
        internal static WidgetsManager Find(string boardName)
        {
            WidgetsManager widgetsManager = BoardsManager.Find(boardName);
            if (widgetsManager == null)
            {
                throw new Exception($"The board {boardName} could not be found for executing a Net Action.");
            }
            return widgetsManager;
        }
    }
}