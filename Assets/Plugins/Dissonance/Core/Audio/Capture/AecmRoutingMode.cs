namespace Dissonance.Audio.Capture
{
    public enum AecmRoutingMode
    {
        // Implementation note - these specific values are important - the WebRtcPreprocessor uses these exact same
        // int values. Don't change them without also changing them there and recompiling on all platforms!

        Disabled = -1,

        QuietEarpieceOrHeadset = 0,
        Earpiece = 1,
        LoudEarpiece = 2,
        Speakerphone = 3,
        LoudSpeakerphone = 4
    }
}
