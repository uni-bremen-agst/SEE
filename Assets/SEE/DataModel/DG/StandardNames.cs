namespace SEE.DataModel.DG
{
    /// <summary>
    /// Attribute names for unique identification of nodes.
    /// </summary>
    public static class Linkage
    {
        /// <summary>
        /// The attribute name for unique identifiers (within a graph).
        /// </summary>
        public const string Name = "Linkage.Name";
    }

    /// <summary>
    /// Defines names of node types used in the graph.
    /// </summary>
    public static class NodeTypes
    {
        /// <summary>
        /// Node type for methods.
        /// </summary>
        public const string Method = "Method";

        /// <summary>
        /// Node type for classes.
        /// </summary>
        public const string Class = "Class";

        /// <summary>
        /// Node type for interfaces.
        /// </summary>
        public const string Interface = "Interface";

        /// <summary>
        /// Node type for files
        /// </summary>
        public const string File = "File";

        /// <summary>
        /// Node type for class templates.
        /// </summary>
        public const string ClassTemplate = "Class_Template";

        /// <summary>
        /// Node type for interface templates.
        /// </summary>
        public const string InterfaceTemplate = "Interface_Template";


    }

    /// <summary>
    /// Standard names for LSP concepts.
    /// </summary>
    public static class LSP
    {
        /// <summary>
        /// Name of edge type for LSP references.
        /// </summary>
        public const string Reference = "Reference";
        /// <summary>
        /// Name of edge type for LSP declarations.
        /// </summary>
        public const string Declaration = "Declaration";
        /// <summary>
        /// Name of edge type for LSP definitions.
        /// </summary>
        public const string Definition = "Definition";
        /// <summary>
        /// Name of edge type for LSP of-type relation.
        /// </summary>
        public const string OfType = "Of_Type";
        /// <summary>
        /// Name of edge type for LSP implementation-of relation.
        /// </summary>
        public const string ImplementationOf = "Implementation_Of";
        /// <summary>
        /// Name of edge type for LSP call relation.
        /// </summary>
        public const string Call = "Call";
        /// <summary>
        /// Name of edge type for LSP extend relation.
        /// </summary>
        public const string Extend = "Extend";
    }

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
        LspError,
        LspWarning,
        LspInfo,
        LspHint,
        IssuesTotal
    }

    /// <summary>
    /// Provides a common prefix for all metrics.
    /// </summary>
    public static class Metrics
    {
        /// <summary>
        /// Prefix for all metrics.
        /// </summary>
        public const string Prefix = "Metric.";

        /// <summary>
        /// Prefix for line metrics.
        /// </summary>
        public const string Lines = Prefix + "Lines.";

        /// <summary>
        /// Name of lines of code (LOC) metric.
        /// </summary>
        public const string LOC = Lines + "LOC";

        /// <summary>
        /// Number of comments in the source code.
        /// </summary>
        public const string Comments = Lines + "Comments";

        /// <summary>
        /// Number of tokens in the source code.
        /// </summary>
        public const string NumberOfTokens = Prefix + "Number_of_Tokens";

        /// <summary>
        /// Name of McCabe complexity metric.
        /// </summary>
        public const string McCabe = Prefix + "McCabe_Complexity";
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
                NumericAttributeNames.NumberOfTokens => Metrics.Prefix + "Number_of_Tokens",
                NumericAttributeNames.CloneRate => Metrics.Prefix + "Clone_Rate",
                NumericAttributeNames.LOC => Metrics.Prefix + "Lines.LOC",
                NumericAttributeNames.Complexity => Metrics.Prefix + "Complexity",
                NumericAttributeNames.ArchitectureViolations => Metrics.Prefix + "Architecture_Violations",
                NumericAttributeNames.Clone => Metrics.Prefix + "Clone",
                NumericAttributeNames.Cycle => Metrics.Prefix + "Cycle",
                NumericAttributeNames.DeadCode => Metrics.Prefix + "Dead_Code",
                NumericAttributeNames.Metric => Metrics.Prefix + "Metric",
                NumericAttributeNames.Style => Metrics.Prefix + "Style",
                NumericAttributeNames.Universal => Metrics.Prefix + "Universal",
                NumericAttributeNames.IssuesTotal => Metrics.Prefix + "IssuesTotal",
                NumericAttributeNames.LspError => Metrics.Prefix + "LSP_Error",
                NumericAttributeNames.LspWarning => Metrics.Prefix + "LSP_Warning",
                NumericAttributeNames.LspInfo => Metrics.Prefix + "LSP_Info",
                NumericAttributeNames.LspHint => Metrics.Prefix + "LSP_Hint",
                _ => throw new System.Exception("Unknown attribute name " + numericAttributeName)
            };
        }
    }

    /// <summary>
    /// Provides a common prefix for all Halstead metrics.
    /// </summary>
    public static class Halstead
    {
        /// <summary>
        /// Prefix for all metrics.
        /// </summary>
        public const string Prefix = Metrics.Prefix + "Halstead.";

        public const string DistinctOperators = Prefix + "Distinct_Operators";
        public const string DistinctOperands = Prefix + "Distinct_Operands";
        public const string TotalOperators = Prefix + "Total_Operators";
        public const string TotalOperands = Prefix + "Total_Operands";
        public const string ProgramVocabulary = Prefix + "Program_Vocabulary";
        public const string ProgramLength = Prefix + "Program_Length";
        public const string EstimatedProgramLength = Prefix + "Estimated_Program_Length";
        public const string Volume = Prefix + "Volume";
        public const string Difficulty = Prefix + "Difficulty";
        public const string Effort = Prefix + "Effort";
        public const string TimeRequiredToProgram = Prefix + "Time_Required_To_Program";
        public const string NumberOfDeliveredBugs = Prefix + "Number_Of_Delivered_Bugs";
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
        public const string Prefix = Metrics.Prefix + "JaCoCo.";
        /// <summary>
        /// The number of instructions that were not executed.
        /// </summary>
        public const string InstructionMissed = Prefix + "INSTRUCTION_missed";
        /// <summary>
        /// The number of instructions that were executed.
        /// </summary>
        public const string InstructionCovered = Prefix + "INSTRUCTION_covered";
        /// <summary>
        /// InstructionCovered / (InstructionCovered + InstructionMissed).
        /// </summary>
        public const string PercentageOfInstructionCovered = Prefix + "INSTRUCTION_percentage";

        /// <summary>
        /// The number of branches (conditional statement constitute branches) that were not executed.
        /// </summary>
        public const string BranchMissed = Prefix + "BRANCH_missed";
        /// <summary>
        /// The number of branches (conditional statement constitute branches) that were executed.
        /// </summary>
        public const string BranchCovered = Prefix + "BRANCH_covered";
        /// <summary>
        /// BranchCovered / (BranchMissed + BranchCovered).
        /// </summary>
        public const string PercentageOfBranchCovered = Prefix + "BRANCH_percentage";

        /// <summary>
        /// Cyclomatic complexity is defined for each non-abstract method and is also
        /// aggregated from methods to classes, packages, and groups they are contained.
        /// McCabe1996 cyclomatic complexity is the minimum number of paths that can,
        /// in (linear) combination, generate all possible paths through a method.
        /// This metric counts the missed such paths.
        /// </summary>
        public const string ComplexityMissed = Prefix + "COMPLEXITY_missed";
        /// <summary>
        /// Cyclomatic complexity is defined for each non-abstract method and is also
        /// aggregated from methods to classes, packages, and groups they are contained.
        /// McCabe1996 cyclomatic complexity is the minimum number of paths that can,
        /// in (linear) combination, generate all possible paths through a method.
        /// This metric counts the covered such paths.
        /// </summary>
        public const string ComplexityCovered = Prefix + "COMPLEXITY_covered";
        /// <summary>
        /// ComplexityCovered / (ComplexityMissed + ComplexityCovered).
        /// </summary>
        public const string PercentageOfComplexityCovered = Prefix + "COMPLEXITY_percentage";

        /// <summary>
        /// This metric is defined for all class files that have been compiled with debug information,
        /// such that coverage information for individual lines can be calculated.
        /// A source line is considered executed when at least one instruction that is assigned to
        /// this line has been executed.
        /// This metric counts the number of such lines missed.
        /// </summary>
        public const string LineMissed = Prefix + "LINE_missed";
        /// <summary>
        /// This metric is defined for all class files that have been compiled with debug information,
        /// such that coverage information for individual lines can be calculated.
        /// A source line is considered executed when at least one instruction that is assigned to
        /// this line has been executed
        /// This metric counts the number of such lines covered.
        /// </summary>
        public const string LineCovered = Prefix + "LINE_covered";
        /// <summary>
        /// LineCovered / (LineMissed + LineCovered).
        /// </summary>
        public const string PercentageOfLineCovered = Prefix + "LINE_percentage";

        /// <summary>
        /// A method is considered as executed when at least one of its instructions has been executed.
        /// As JaCoCo works on byte code level also constructors and static initializers are counted as methods.
        /// Some of these methods may not have a direct correspondence in Java source code,
        /// like implicit and thus generated default constructors or initializers for constants.
        /// This metric counts the number of missed methods.
        /// </summary>
        public const string MethodMissed = Prefix + "METHOD_missed";
        /// <summary>
        /// A method is considered as executed when at least one of its instructions has been executed.
        /// As JaCoCo works on byte code level also constructors and static initializers are counted as methods.
        /// Some of these methods may not have a direct correspondence in Java source code,
        /// like implicit and thus generated default constructors or initializers for constants.
        /// This metric counts the number of covered methods.
        /// </summary>
        public const string MethodCovered = Prefix + "METHOD_covered";
        /// <summary>
        /// MethodCovered / (MethodMissed + MethodCovered).
        /// </summary>
        public const string PercentageOfMethodCovered = Prefix + "METHOD_percentage";

        /// <summary>
        /// A class is considered as executed when at least one of its methods has been executed.
        /// Note that JaCoCo considers constructors as well as static initializers as methods.
        /// This metric counts the number of missed classes.
        /// </summary>
        public const string ClassMissed = Prefix + "CLASS_missed";
        /// <summary>
        /// A class is considered as executed when at least one of its methods has been executed.
        /// Note that JaCoCo considers constructors as well as static initializers as methods.
        /// This metric counts the number of covered classes.
        /// </summary>
        public const string ClassCovered = Prefix + "CLASS_covered";
        /// <summary>
        /// ClassCovered / (ClassMissed + ClassCovered).
        /// </summary>
        public const string PercentageOfClassCovered = Prefix + "CLASS_percentage";
    }

    /// <summary>
    /// Defines names of node attributes for VCS metrics.
    /// </summary>
    public static class VCS
    {
        /// <summary>
        /// Prefix for VCS data (metrics and other kinds of VCS related attributes).
        /// </summary>
        public const string VCSPrefix = "VCS.";
        /// <summary>
        /// Prefix for VCS metrics.
        /// </summary>
        public const string Prefix = Metrics.Prefix + VCSPrefix;

        /// <summary>
        /// The number of lines of code added for a file that was changed between two commits.
        /// </summary>
        public const string LinesAdded = Prefix + "Lines_Added";
        /// <summary>
        /// The number of lines of code removed from a file that was changed between two commits.
        /// </summary>
        public const string LinesRemoved = Prefix + "Lines_Removed";
        /// <summary>
        /// The churn of a file, that is, the number of lines added and deleted.
        /// </summary>
        public const string Churn = Prefix + "Churn";
        /// <summary>
        /// The number of unique developers who contributed to a file that was changed between two commits.
        /// </summary>
        public const string NumberOfDevelopers = Prefix + "Number_Of_Developers";
        /// <summary>
        /// The number of commits for a given file that was changed between two commits.
        /// </summary>
        public const string NumberOfCommits = Prefix + "Number_Of_Commits";
        /// <summary>
        /// The truck factor of a file (core-devs metric).
        /// </summary>
        public const string TruckNumber = Prefix + "Truck_Number";

        /// <summary>
        /// String attribute for the list of authors of a file.
        /// </summary>
        /// <remarks>Note that this is not actually a numeric metric but a list of
        /// author names seperated by a comma.</remarks>
        public const string AuthorsAttributeName = "Authors";

        /// <summary>
        /// Name of node type used for files.
        /// </summary>
        public const string FileType = "File";
        /// <summary>
        /// Name of node type used for directories.
        /// </summary>
        public const string DirectoryType = "Directory";
        /// <summary>
        /// Name of node type used for repositories.
        /// </summary>
        public const string RepositoryType = "Repository";
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
}
