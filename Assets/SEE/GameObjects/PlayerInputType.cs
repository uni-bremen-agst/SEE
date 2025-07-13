namespace SEE.GO
{
    /// <summary>
    /// What kind of input devices the player uses.
    /// </summary>
    public enum PlayerInputType
    {
        DesktopPlayer = 0,      // player for desktop and mouse input
        VRPlayer = 1,           // player for virtual reality devices
        TouchGamepadPlayer = 2, // player for touch devices or gamepads using InControl
        None = 3,               // no player at all
    }
}
