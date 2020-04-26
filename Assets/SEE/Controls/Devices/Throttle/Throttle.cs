namespace SEE.Controls.Devices
{
    /// <summary>
    /// A throttle input device providing data relevant for movements.
    /// </summary>
    public abstract class Throttle : InputDevice
    {
        /// <summary>
        /// A value >= 0 indicating the speed request by the user.
        /// Depending on the actual device this value could be discrete
        /// or continuous.
        /// </summary>
        public abstract float Value { get; }
    }
}
