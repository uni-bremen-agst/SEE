using System;
using mini.Calculator.Operators;

namespace mini.Calculator
{
    public class Calculator
    {
        /// <summary>
        /// Calculates an operation of two numbers and an operand.
        ///
        /// The supported operants are:
        /// <see cref="Plus"/> and <see cref="Minus"/>
        /// 
        /// If the operand is not known or the numbers cant be parsed an Exception is thrown 
        /// </summary>
        /// <param name="num1">The first operand</param>
        /// <param name="num2">The seconds operand</param>
        /// <param name="calculationOperator">The operand of the calculation</param>
        /// <returns>The result of the operation, which is also an <see cref="Double"/></returns>
        /// <exception cref="Exception">If the operator in unknown or the <paramref name="num1"/> or <paramref name="num2"/> cant be parsed.</exception>
        public Double Calculate(string num1, string num2, string calculationOperator)
        {
            Operator op;
            try
            {
                switch (calculationOperator)
                {
                    case "+":
                        op = new Plus(Double.Parse(num1), Double.Parse(num2));
                        break;
                    case "-":
                        op = new Minus(Double.Parse(num1), Double.Parse(num2));
                        break;
                    default:
                        Console.WriteLine("Upsie wupsie");
                        throw new Exception("Unknown operation");
                }
            }
            catch (Exception e)
            {
                throw e;
            }

            return op.Execute();
        }
    }
}