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

    /// <summary>
    /// Defines names of JaCoCo code-coverage metrics.
    /// </summary>
    public static class JaCoCo
    {
        /// <summary>
        /// Names of node attributes provided by JaCoCo.
        /// See also: https://www.eclemma.org/jacoco/trunk/doc/counters.html
        /// </summary>

        /// <summary>
        /// The number of instructions that were not executed.
        /// </summary>
        public const string InstructionMissed = "Metric.INSTRUCTION_missed";
        /// <summary>
        /// The number of instructions that were executed.
        /// </summary>
        public const string InstructionCovered = "Metric.INSTRUCTION_covered";
        /// <summary>
        /// InstructionCovered / (InstructionCovered + InstructionMissed).
        /// </summary>
        public const string PercentageOfInstructionCovered = "Metric.INSTRUCTION_percentage";

        /// <summary>
        /// The number of branches (conditional statement constitute branches) that were not executed.
        /// </summary>
        public const string BranchMissed = "Metric.BRANCH_missed";
        /// <summary>
        /// The number of branches (conditional statement constitute branches) that were executed.
        /// </summary>
        public const string BranchCovered = "Metric.BRANCH_covered";
        /// <summary>
        /// BranchCovered / (BranchMissed + BranchCovered).
        /// </summary>
        public const string PercentageOfBranchCovered = "Metric.BRANCH_percentage";

        /// <summary>
        /// Cyclomatic complexity is defined for each non-abstract method and is also
        /// aggregated from methods to classes, packages, and groups they are contained.
        /// McCabe1996 cyclomatic complexity is the minimum number of paths that can,
        /// in (linear) combination, generate all possible paths through a method.
        /// This metric counts the missed such paths.
        /// </summary>
        public const string ComplexityMissed = "Metric.COMPLEXITY_missed";
        /// <summary>
        /// Cyclomatic complexity is defined for each non-abstract method and is also
        /// aggregated from methods to classes, packages, and groups they are contained.
        /// McCabe1996 cyclomatic complexity is the minimum number of paths that can,
        /// in (linear) combination, generate all possible paths through a method.
        /// This metric counts the covered such paths.
        /// </summary>
        public const string ComplexityCovered = "Metric.COMPLEXITY_covered";
        /// <summary>
        /// ComplexityCovered / (ComplexityMissed + ComplexityCovered).
        /// </summary>
        public const string PercentageOfComplexityCovered = "Metric.COMPLEXITY_percentage";

        /// <summary>
        /// This metric is defined for all class files that have been compiled with debug information,
        /// such that coverage information for individual lines can be calculated.
        /// A source line is considered executed when at least one instruction that is assigned to
        /// this line has been executed
        /// This metric counts the number of such lines missed.
        /// </summary>
        public const string LineMissed = "Metric.LINE_missed";
        /// <summary>
        /// This metric is defined for all class files that have been compiled with debug information,
        /// such that coverage information for individual lines can be calculated.
        /// A source line is considered executed when at least one instruction that is assigned to
        /// this line has been executed
        /// This metric counts the number of such lines covered.
        /// </summary>
        public const string LineCovered = "Metric.LINE_covered";
        /// <summary>
        /// LineCovered / (LineMissed + LineCovered).
        /// </summary>
        public const string PercentageOfLineCovered = "Metric.LINE_percentage";

        /// <summary>
        /// A method is considered as executed when at least one of its instructions has been executed.
        /// As JaCoCo works on byte code level also constructors and static initializers are counted as methods.
        /// Some of these methods may not have a direct correspondence in Java source code,
        /// like implicit and thus generated default constructors or initializers for constants.
        /// This metric counts the number of missed methods.
        /// </summary>
        public const string MethodMissed = "Metric.METHOD_missed";
        /// <summary>
        /// A method is considered as executed when at least one of its instructions has been executed.
        /// As JaCoCo works on byte code level also constructors and static initializers are counted as methods.
        /// Some of these methods may not have a direct correspondence in Java source code,
        /// like implicit and thus generated default constructors or initializers for constants.
        /// This metric counts the number of covered methods.
        /// </summary>
        public const string MethodCovered = "Metric.METHOD_covered";
        /// <summary>
        /// MethodCovered / (MethodMissed + MethodCovered).
        /// </summary>
        public const string PercentageOfMethodCovered = "Metric.METHOD_percentage";

        /// <summary>
        /// A class is considered as executed when at least one of its methods has been executed.
        /// Note that JaCoCo considers constructors as well as static initializers as methods.
        /// This metric counts the number of missed classes.
        /// </summary>
        public const string ClassMissed = "Metric.CLASS_missed";
        /// <summary>
        /// A class is considered as executed when at least one of its methods has been executed.
        /// Note that JaCoCo considers constructors as well as static initializers as methods.
        /// This metric counts the number of covered classes.
        /// </summary>
        public const string ClassCovered = "Metric.CLASS_covered";
        /// <summary>
        /// ClassCovered / (ClassMissed + ClassCovered).
        /// </summary>
        public const string PercentageOfClassCovered = "Metric.CLASS_percentage";
    }
}
