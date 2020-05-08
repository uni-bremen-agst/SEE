namespace SEE.Controls.Devices
{
    public class NullTransformation : Transformation
    {
        public override float ZoomFactor => 1.0f;

        public override Kind Recognize()
        {
            return Kind.None;
        }
    }
}