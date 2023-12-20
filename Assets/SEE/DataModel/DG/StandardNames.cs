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
        public const string InstructionMissed = "Metric.INSTRUCTION_missed";
        public const string InstructionCovered = "Metric.INSTRUCTION_covered";
        public const string LineMissed = "Metric.LINE_missed";
        public const string LineCovered = "Metric.LINE_covered";
        public const string ComplexityMissed = "Metric.COMPLEXITY_missed";
        public const string ComplexityCovered = "Metric.COMPLEXITY_covered";
        public const string MethodMissed = "Metric.METHOD_missed";
        public const string MethodCovered = "Metric.METHOD_covered";

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