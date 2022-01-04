namespace Dissonance
{
    public enum ChannelPriority
    {
        /// <summary>
        /// No priority assigned
        /// </summary>
        None = -2,

        /// <summary>
        /// Low priority (will be muted by voices at the default priority)
        /// </summary>
        Low = -1,

        /// <summary>
        /// Default priority (will be muted by medium and high priority)
        /// </summary>
        Default = 0,

        /// <summary>
        /// Medium priority (will be muted by high priority)
        /// </summary>
        Medium = 1,

        /// <summary>
        /// High priority (will not be muted by any other voice)
        /// </summary>
        High = 2
    }
}
