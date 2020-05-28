using System;

namespace SEE.Controls.Devices
{
    [Obsolete("This functionality is covered by a selection device.")]
    public class NullTransformation : Transformation
    {
        public override float ZoomFactor => 1.0f;

        public override Kind Recognize()
        {
            return Kind.None;
        }
    }
}