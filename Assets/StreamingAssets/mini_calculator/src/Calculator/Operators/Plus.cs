namespace mini.Calculator
{
    /// <summary>
    /// This is an implementation for a simple addition calculation.
    /// </summary>
    public class Plus : Operator
    {
        public override double Execute()
        {
            return Left + Right;
        }

        public Plus(double left, double right) : base(left, right)
        {
        }
    }
}