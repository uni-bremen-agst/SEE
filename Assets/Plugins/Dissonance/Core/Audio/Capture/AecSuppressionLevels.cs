namespace Dissonance.Audio.Capture
{
    public enum AecSuppressionLevels
    {
        // Implementation note - these specific values are important - the WebRtcPreprocessor uses these exact same
        // int values. Don't change them without also changing them there and recompiling on all platforms!

        Disabled = -1,

        Low = 0,
        Moderate = 1,
        High = 2
    }
}
