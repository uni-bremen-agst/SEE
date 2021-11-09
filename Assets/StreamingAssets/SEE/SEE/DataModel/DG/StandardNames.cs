namespace SEE.DataModel.DG
{
    public enum NumericAttributeNames
    {
        Number_Of_Tokens,
        Clone_Rate,
        LOC,
        Complexity,
        Architecture_Violations,
        Clone,
        Cycle,
        Dead_Code,
        Metric,
        Style,
        Universal,
        IssuesTotal
    }

    public static class AttributeNamesExtensions
    {
        public static string Name(this NumericAttributeNames me)
        {
            return me switch
            {
                NumericAttributeNames.Number_Of_Tokens => "Metric.Number_of_Tokens",
                NumericAttributeNames.Clone_Rate => "Metric.Clone_Rate",
                NumericAttributeNames.LOC => "Metric.LOC",
                NumericAttributeNames.Complexity => "Metric.Complexity",
                NumericAttributeNames.Architecture_Violations => "Metric.Architecture_Violations",
                NumericAttributeNames.Clone => "Metric.Clone",
                NumericAttributeNames.Cycle => "Metric.Cycle",
                NumericAttributeNames.Dead_Code => "Metric.Dead_Code",
                NumericAttributeNames.Metric => "Metric.Metric",
                NumericAttributeNames.Style => "Metric.Style",
                NumericAttributeNames.Universal => "Metric.Universal",
                NumericAttributeNames.IssuesTotal => "Metric.IssuesTotal",
                _ => throw new System.Exception("Unknown attribute name " + me)
            };
        }
    }
}