using System;

namespace mini.Calculator.Operators
{
    /// <summary>
    /// This class represents an abstract operator for basic math stuff.
    ///
    /// It contains two numbers, which should be processed in the calculation.
    ///
    /// This class can be extended to include more math operations.
    /// </summary>
    public abstract class Operator
    {
        /// <summary>
        /// The Left operator.
        /// </summary>
        public Double Left { get; }
        
        /// <summary>
        /// The right operator.
        /// </summary>
        public Double Right { get; }

        /// <summary>
        /// Constructs a new Operator from two operands.
        ///
        /// The two operands should be of the <see cref="Double"/> type
        /// </summary>
        /// <param name="left">The left operand</param>
        /// <param name="right">The right operand</param>
        protected Operator(double left, double right)
        {
            Left = left;
            Right = right;
        }

        /// <summary>
        /// Executes the operation.
        ///
        /// The implementation should calculate the result.
        /// </summary>
        /// <returns>The result of the operation as <see cref="Double"/></returns>
        public abstract Double Execute();
    }
}