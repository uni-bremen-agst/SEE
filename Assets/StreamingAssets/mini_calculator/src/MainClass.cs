using System;
using mini.Calculator;

namespace mini
{
    class MainClass
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Bitte Zahl 1 eingeben:\n");
            string number1 = Console.ReadLine();

            Console.WriteLine("Bitte Zahl 2 eingeben:\n");
            string number2 = Console.ReadLine();

            Console.WriteLine("Bitte + oder - angeben");
            string oper = Console.ReadLine();

            Calculator.Calculator c = new Calculator.Calculator();
            try
            {
                Console.WriteLine($"Die Lösung ist: {c.Calculate(number1, number2, oper)}");
            }
            catch (Exception e)
            {
                Console.WriteLine("Upsie wupsie, i think this calculator Just broke :(");
            }
        }
    }
}