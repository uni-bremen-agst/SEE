namespace Dissonance.Audio.Capture
{
    /// <summary>
    /// Levels specifying how sensitive the Voice Activation Detector (VAD) is.
    /// This is a direct tradeoff: higher levels are more likely to classify a frame of audio as speech, which means they will let through more non-speech audio.
    /// </summary>
    public enum VadSensitivityLevels
    {
        // Implementation note - these specific values are important - the WebRtcPreprocessor uses these exact same
        // int values. Don't change them without also changing them there and recompiling on all platforms!

        LowSensitivity = 0,
        MediumSensitivity = 1,
        HighSensitivity = 2,
        VeryHighSensitivity = 3
    }
}
