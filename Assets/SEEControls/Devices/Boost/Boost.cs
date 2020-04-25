namespace SEE.Controls.Devices
{
    /// <summary>
    /// Abstract super class of all input devices providing a boost factor for movements.
    /// </summary>
    public abstract class Boost : InputDevice
    {
        public abstract float Value { get; }
    }
}
