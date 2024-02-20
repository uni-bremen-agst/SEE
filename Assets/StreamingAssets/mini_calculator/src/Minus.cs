namespace mini.Calculator
{
    /// <summary>
    /// Same as in <see cref="Plus"/> just with subtraction.
    /// </summary>
    public class Minus : Operator
    {
        public override double Execute()
        {
            return Left + Right;
        }

        public Minus(double left, double right) : base(left, right)
        {
        }
    }
}