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
                NumericAttributeNames.LOC => "Metric.Lines.LOC",
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
    /// Defines names of node attributes for JaCoCo code-coverage metrics.
    /// See also: https://www.eclemma.org/jacoco/trunk/doc/counters.html
    /// </summary>
    public static class JaCoCo
    {
        /// <summary>
        /// The prefix of each JaCoCo metric.
        /// </summary>
        public const string Prefix = "Metric.JaCoCo";
        /// <summary>
        /// The number of instructions that were not executed.
        /// </summary>
        public const string InstructionMissed = "Metric.JaCoCo.INSTRUCTION_missed";
        /// <summary>
        /// The number of instructions that were executed.
        /// </summary>
        public const string InstructionCovered = "Metric.JaCoCo.INSTRUCTION_covered";
        /// <summary>
        /// InstructionCovered / (InstructionCovered + InstructionMissed).
        /// </summary>
        public const string PercentageOfInstructionCovered = "Metric.JaCoCo.INSTRUCTION_percentage";

        /// <summary>
        /// The number of branches (conditional statement constitute branches) that were not executed.
        /// </summary>
        public const string BranchMissed = "Metric.JaCoCo.BRANCH_missed";
        /// <summary>
        /// The number of branches (conditional statement constitute branches) that were executed.
        /// </summary>
        public const string BranchCovered = "Metric.JaCoCo.BRANCH_covered";
        /// <summary>
        /// BranchCovered / (BranchMissed + BranchCovered).
        /// </summary>
        public const string PercentageOfBranchCovered = "Metric.JaCoCo.BRANCH_percentage";

        /// <summary>
        /// Cyclomatic complexity is defined for each non-abstract method and is also
        /// aggregated from methods to classes, packages, and groups they are contained.
        /// McCabe1996 cyclomatic complexity is the minimum number of paths that can,
        /// in (linear) combination, generate all possible paths through a method.
        /// This metric counts the missed such paths.
        /// </summary>
        public const string ComplexityMissed = "Metric.JaCoCo.COMPLEXITY_missed";
        /// <summary>
        /// Cyclomatic complexity is defined for each non-abstract method and is also
        /// aggregated from methods to classes, packages, and groups they are contained.
        /// McCabe1996 cyclomatic complexity is the minimum number of paths that can,
        /// in (linear) combination, generate all possible paths through a method.
        /// This metric counts the covered such paths.
        /// </summary>
        public const string ComplexityCovered = "Metric.JaCoCo.COMPLEXITY_covered";
        /// <summary>
        /// ComplexityCovered / (ComplexityMissed + ComplexityCovered).
        /// </summary>
        public const string PercentageOfComplexityCovered = "Metric.JaCoCo.COMPLEXITY_percentage";

        /// <summary>
        /// This metric is defined for all class files that have been compiled with debug information,
        /// such that coverage information for individual lines can be calculated.
        /// A source line is considered executed when at least one instruction that is assigned to
        /// this line has been executed.
        /// This metric counts the number of such lines missed.
        /// </summary>
        public const string LineMissed = "Metric.JaCoCo.LINE_missed";
        /// <summary>
        /// This metric is defined for all class files that have been compiled with debug information,
        /// such that coverage information for individual lines can be calculated.
        /// A source line is considered executed when at least one instruction that is assigned to
        /// this line has been executed
        /// This metric counts the number of such lines covered.
        /// </summary>
        public const string LineCovered = "Metric.JaCoCo.LINE_covered";
        /// <summary>
        /// LineCovered / (LineMissed + LineCovered).
        /// </summary>
        public const string PercentageOfLineCovered = "Metric.JaCoCo.LINE_percentage";

        /// <summary>
        /// A method is considered as executed when at least one of its instructions has been executed.
        /// As JaCoCo works on byte code level also constructors and static initializers are counted as methods.
        /// Some of these methods may not have a direct correspondence in Java source code,
        /// like implicit and thus generated default constructors or initializers for constants.
        /// This metric counts the number of missed methods.
        /// </summary>
        public const string MethodMissed = "Metric.JaCoCo.METHOD_missed";
        /// <summary>
        /// A method is considered as executed when at least one of its instructions has been executed.
        /// As JaCoCo works on byte code level also constructors and static initializers are counted as methods.
        /// Some of these methods may not have a direct correspondence in Java source code,
        /// like implicit and thus generated default constructors or initializers for constants.
        /// This metric counts the number of covered methods.
        /// </summary>
        public const string MethodCovered = "Metric.JaCoCo.METHOD_covered";
        /// <summary>
        /// MethodCovered / (MethodMissed + MethodCovered).
        /// </summary>
        public const string PercentageOfMethodCovered = "Metric.JaCoCo.METHOD_percentage";

        /// <summary>
        /// A class is considered as executed when at least one of its methods has been executed.
        /// Note that JaCoCo considers constructors as well as static initializers as methods.
        /// This metric counts the number of missed classes.
        /// </summary>
        public const string ClassMissed = "Metric.JaCoCo.CLASS_missed";
        /// <summary>
        /// A class is considered as executed when at least one of its methods has been executed.
        /// Note that JaCoCo considers constructors as well as static initializers as methods.
        /// This metric counts the number of covered classes.
        /// </summary>
        public const string ClassCovered = "Metric.JaCoCo.CLASS_covered";
        /// <summary>
        /// ClassCovered / (ClassMissed + ClassCovered).
        /// </summary>
        public const string PercentageOfClassCovered = "Metric.JaCoCo.CLASS_percentage";
    }

    /// <summary>
    /// Defines toggle attributes used to mark nodes as to whether they have been
    /// changed, deleted, or added as new.
    /// </summary>
    public static class ChangeMarkers
    {
        /// <summary>
        /// Name of the toggle marking a graph element as new (existing only in the newer version).
        /// </summary>
        public const string IsNew = "Change.IsNew";
        /// <summary>
        /// Name of the toggle marking a graph element as deleted (existing only in the baseline version).
        /// </summary>
        public const string IsDeleted = "Change.IsDeleted";
        /// <summary>
        /// Name of the toggle marking a graph element as changed (existing in both the newer and baseline
        /// version). At least one numeric attribute has changed between the two (including the addition
        /// or removal of an attribute).
        /// </summary>
        public const string IsChanged = "Change.IsChanged";
    }

    /// <summary>
    /// Defines names of node attributes for Git metrics.
    /// </summary>
    public static class Git
    {
        /// <summary>
        /// The number of lines of code added for a file that was changed between two commits.
        /// </summary>
        public const string LinesAdded = "Metric.Git.Lines_Added";
        /// <summary>
        /// The number of lines of code deleted for a file that was changed between two commits.
        /// </summary>
        public const string LinesDeleted = "Metric.Git.Lines_Deleted";
        /// <summary>
        /// The number of unique developers who contributed to a file that was changed between two commits.
        /// </summary>
        public const string NumberOfDevelopers = "Metric.Git.Number_Of_Developers";
        /// <summary>
        /// The number of times a file was changed between two commits.
        /// </summary>
        public const string CommitFrequency = "Metric.Git.Commit_Frequency";
    }
}
