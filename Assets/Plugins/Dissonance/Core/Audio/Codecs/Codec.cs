namespace Dissonance.Audio.Codecs
{
    public enum Codec
        : byte
    {
        /// <summary>
        /// Identity codec, purely sends raw wave data with NO COMPRESSION.
        /// Not suitable for use in any real world scenario.
        /// </summary>
        Identity = 0,

        /// <summary>
        /// Opus codec
        /// </summary>
        Opus = 1,
    }
}