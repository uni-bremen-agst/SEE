namespace SEE.DataModel.DG
{
    /// <summary>
    /// Names of node attributes provided by the Axivion Suite.
    /// </summary>
    public enum NumericAttributeNames
    {
        NumberOfTokens,
        CloneRate,
        LOC,
        Complexity,
        ArchitectureViolations,
        Clone,
        Cycle,
        DeadCode,
        Metric,
        Style,
        Universal,
        IssuesTotal
    }

    /// <summary>
    /// Provides an extension <see cref="Name(NumericAttributeNames)"/> for <see cref="NumericAttributeNames"/>.
    /// </summary>
    public static class AttributeNamesExtensions
    {
        /// <summary>
        /// Names of node attributes provided by JaCoCo Test
        /// </summary>
        ///
        //Description of metrics used from https://www.eclemma.org/jacoco/trunk/doc/counters.html

        // Instruction coverage provides information about the amount of code that has been executed or missed
        public const string InstructionMissed = "Metric.INSTRUCTION_missed"; //
        public const string InstructionCovered = "Metric.INSTRUCTION_covered";
        public const string PercentageOfInstructionCovered = "Metric.INSTRUCTION_percentage";

        //  Branch coverage for all if and switch statements. This metric counts the total number of such branches
        //  in a method and determines the number of executed or missed branches
        public const string BranchMissed = "Metric.BRANCH_missed";
        public const string BranchCovered = "Metric.BRANCH_covered";
        public const string PercentageOfBranchCovered = "Metric.BRANCH_percentage";

        // Cyclomatic complexity for each non-abstract method and summarizes complexity for classes, packages and groups.
        // McCabe1996 cyclomatic complexity is the minimum number of paths that can, in (linear) combination, generate all possible paths through a method.
        public const string ComplexityMissed = "Metric.COMPLEXITY_missed";
        public const string ComplexityCovered = "Metric.COMPLEXITY_covered";
        public const string PercentageOfComplexityCovered = "Metric.COMPLEXITY_percentage";

        // all class files that have been compiled with debug information, coverage information for individual lines can be calculated.
        // A source line is considered executed when at least one instruction that is assigned to this line has been executed
        public const string LineMissed = "Metric.LINE_missed";
        public const string LineCovered = "Metric.LINE_covered";
        public const string PercentageOfLineCovered = "Metric.LINE_percentage";

        //  A method is considered as executed when at least one instruction has been executed.
        //  As JaCoCo works on byte code level also constructors and static initializers are counted as methods.
        //  Some of these methods may not have a direct correspondence in Java source code,
        //  like implicit and thus generated default constructors or initializers for constants.
        public const string MethodMissed = "Metric.METHOD_missed";
        public const string MethodCovered = "Metric.METHOD_covered";
        public const string PercentageOfMethodCovered = "Metric.METHOD_percentage";

        // A class is considered as executed when at least one of its methods has been executed.
        // Note that JaCoCo considers constructors as well as static initializers as methods.
        public const string ClassMissed = "Metric.CLASS_missed";
        public const string ClassCovered = "Metric.CLASS_covered";
        public const string PercentageOfClassCovered = "Metric.CLASS_percentage";

        /// <summary>
        /// Name of the given <paramref name="numericAttributeName"/>.
        /// </summary>
        /// <param name="numericAttributeName">numeric attribute name whose name is requested</param>
        /// <returns>the name of <paramref name="numericAttributeName"/></returns>
        /// <exception cref="System.Exception">thrown if <paramref name="numericAttributeName"/>
        /// is not handled in this method</exception>
        public static string Name(this NumericAttributeNames numericAttributeName)
        {
            return numericAttributeName switch
            {
                NumericAttributeNames.NumberOfTokens => "Metric.Number_of_Tokens",
                NumericAttributeNames.CloneRate => "Metric.Clone_Rate",
                NumericAttributeNames.LOC => "Metric.LOC",
                NumericAttributeNames.Complexity => "Metric.Complexity",
                NumericAttributeNames.ArchitectureViolations => "Metric.Architecture_Violations",
                NumericAttributeNames.Clone => "Metric.Clone",
                NumericAttributeNames.Cycle => "Metric.Cycle",
                NumericAttributeNames.DeadCode => "Metric.Dead_Code",
                NumericAttributeNames.Metric => "Metric.Metric",
                NumericAttributeNames.Style => "Metric.Style",
                NumericAttributeNames.Universal => "Metric.Universal",
                NumericAttributeNames.IssuesTotal => "Metric.IssuesTotal",
                _ => throw new System.Exception("Unknown attribute name " + numericAttributeName)
            };
        }
    }
}