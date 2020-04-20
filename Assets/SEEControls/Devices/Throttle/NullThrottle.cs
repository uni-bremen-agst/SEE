namespace SEE.Controls.Devices
{
    public class NullThrottle : Throttle
    {
        public override float Value
        {
            get => 0;
        }
    }
}