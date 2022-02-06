namespace Dissonance
{
    /// <summary>
    /// Describes the tradeoff of latency and bandwidth
    /// </summary>
    public enum FrameSize
    {
        /// <summary>
        /// Lowest latency, but extreme bandwidth overhead (only suitable for LAN)
        /// </summary>
        Tiny = -1,

        /// <summary>
        /// Low latency, but highest bandwidth overhead
        /// </summary>
        Small = 0,

        /// <summary>
        /// Average latency, average bandwidth usage
        /// </summary>
        Medium = 1,

        /// <summary>
        /// Worst latency, but minimal bandwidth overhead
        /// </summary>
        Large = 2
    }
}