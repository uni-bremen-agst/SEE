namespace SEE.Controls.Devices
{
    /// <summary>
    /// A throttle device yielding the constant value 0. It can be used
    /// in situations where a throttle is not actually needed.
    /// </summary>
    public class NullThrottle : Throttle
    {
        /// <summary>
        /// Always 0.
        /// </summary>
        public override float Value
        {
            get => 0;
        }
    }
}