// This file contains all the definition for special metric value types. If you need to, you can add any new value type
// here, but bear in mind that you will also need to add an implementation for this type in each widget that is
// supposed to be compatible with the new type.

namespace SEE.Game.HolisticMetrics
{
    /// <summary>
    /// Depending on which widget is supposed to display the average lines of code metric, simply passing the float
    /// value to the widget's Display() method is not enough. Some widgets also need to know the range.
    /// </summary>
    public class RangeValue
    {
        public (float, float) Range;
        public float Value;

        public RangeValue(float value, float rangeLower = 0f, float rangeHigher = 1000f)
        {
            Value = value;
            Range = (rangeLower, rangeHigher);
        }
    }
}