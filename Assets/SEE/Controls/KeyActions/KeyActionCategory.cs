namespace SEE.Controls.KeyActions
{
    /// <summary>
    /// Categories for the keyboard shortcuts. Each <see cref="KeyActionDescriptor"/>
    /// is an element of exactly one of these categories. These categories are used
    /// to organize the key actions into cohesive groups meaningful to a user.
    /// </summary>
    internal enum KeyActionCategory
    {
        /// <summary>
        /// Actions that are generally applicable in every context.
        /// </summary>
        General,
        /// <summary>
        /// Actions dealing with animations.
        /// </summary>
        Animation,
        /// <summary>
        /// Actions related to architecture verification use case; related to
        /// architecture mapping and analysis.
        /// </summary>
        Architecture,
        /// <summary>
        /// Actions related to browsing a code city (panning, zooming, etc.).
        /// </summary>
        Browsing,
        /// <summary>
        /// Actions related to recording a camera (player) path.
        /// </summary>
        CameraPaths,
        /// <summary>
        /// Actions related to text chatting with other remote players.
        /// </summary>
        Chat,
        /// <summary>
        /// Actions related to the source-code viewer.
        /// </summary>
        CodeViewer,
        /// <summary>
        /// Actions related to the use case debugging.
        /// </summary>
        Debugging,
        /// <summary>
        /// Actions related to the use case evolution; observing the series of revisions of a city.
        /// </summary>
        Evolution,
        /// <summary>
        /// Actions related to showing metric charts.
        /// </summary>
        MetricCharts,
        /// <summary>
        /// Actions related to movements of the player within the world.
        /// </summary>
        Movement,
        /// <summary>
        /// Actions related to interact with a drawable.
        /// </summary>
        Drawable
    }
}
