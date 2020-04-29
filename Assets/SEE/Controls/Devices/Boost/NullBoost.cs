namespace SEE.Controls.Devices
{
    /// <summary>
    /// An input device providing a constant boost factor 0 for movements.
    /// It can be used when no boost is required.
    /// </summary>
    public class NullBoost : Boost
    {
        public override float Value
        {
            get => 0;
        }
    }
}
