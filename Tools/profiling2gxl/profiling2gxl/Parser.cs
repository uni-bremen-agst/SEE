namespace profiling2gxl
{
    /// <summary>
    /// The base class of a parser. A parser implementation should always implement this.
    /// </summary>
    internal abstract class Parser
    {
        /// <summary>
        /// The stream reader of the file that will be parsed into gxl format.
        /// </summary>
        protected StreamReader Sr { get; }

        /// <summary>
        /// The dictionary of detected functions where the function itself is the value and the key is the function index.
        /// </summary>
        protected List<Function> Functions { get; }

        /// <summary>
        /// Parser constructor
        /// </summary>
        /// <param name="Sr">The stream reader of the file that will be parsed into gxl format.</param>
        public Parser(StreamReader Sr)
        {
            this.Sr = Sr;
            this.Functions = new List<Function>();
        }

        /// <summary>
        /// Parses the given file with the parser related to the given format.
        /// </summary>
        /// <returns> The list of functions called</returns>
        public abstract List<Function> parse();
    }
}
