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
            switch (me)
            {
                case NumericAttributeNames.Number_Of_Tokens: return "Metric.Number_of_Tokens";
                case NumericAttributeNames.Clone_Rate: return "Metric.Clone_Rate";
                case NumericAttributeNames.LOC: return "Metric.LOC";
                case NumericAttributeNames.Complexity: return "Metric.Complexity";
                case NumericAttributeNames.Architecture_Violations: return "Metric.Architecture_Violations";
                case NumericAttributeNames.Clone: return "Metric.Clone";
                case NumericAttributeNames.Cycle: return "Metric.Cycle";
                case NumericAttributeNames.Dead_Code: return "Metric.Dead_Code";
                case NumericAttributeNames.Metric: return "Metric.Metric";
                case NumericAttributeNames.Style: return "Metric.Style";
                case NumericAttributeNames.Universal: return "Metric.Universal";
                case NumericAttributeNames.IssuesTotal: return "Metric.IssuesTotal";
                default:
                    throw new System.Exception("Unknown attribute name " + me);
            }
        }
    }
}