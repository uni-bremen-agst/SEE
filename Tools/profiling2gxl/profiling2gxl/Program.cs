using Microsoft.Extensions.Configuration;

namespace profiling2gxl
{
    /// <summary>
    /// Command line application to parse profiler data into gxl format.
    /// </summary>
    class Program
    {
        /// <summary>
        /// A dictionary of all supported formats and their parser implementations.
        /// </summary>
        private readonly static Dictionary<string, Func<StreamReader, Parser>> formats = new Dictionary<string, Func<StreamReader, Parser>> {
            {"gprof", content => new GprofParser(content) },
            {"perf", content => new PerfParser(content) },
            {"hprof", content => new HprofParser(content) },
            {"jprofile", content => new JProfilerParser(content) }
        };

        static void Main(string[] args)
        {

            IConfigurationBuilder builder = new ConfigurationBuilder()
            .AddCommandLine(args);

            IConfigurationRoot? config = builder.Build();

            string? file = config["file"];
            string? format = config["format"];
            string? gxlOutput = config["output"];
            string? jlgOutput = config["jlg"];

            if (string.IsNullOrEmpty(file) || string.IsNullOrEmpty(format) || config["help"] != null)
            {
                Console.WriteLine("Converts profiler data into gxl and optional jlg format.");
                Console.WriteLine("Usage: profiling2gxl --file <file> --format <format>");
                Console.WriteLine("Options:");
                Console.WriteLine("--file <file>        The file containing profiler data.");
                Console.WriteLine($"--format <format>    The format of the given profiler data [Supports {string.Join(",", formats.Keys)}].");
                Console.WriteLine("--output <output>    The name of the gxl file.");
                Console.WriteLine("--jlg <output>       The name of the jlg file.");
                Console.WriteLine("--help               Display this help text.");
            }
            else
            {
                if (formats.ContainsKey(format))
                {
                    try
                    {
                        using StreamReader sr = new(file);
                        Parser parser = formats[format](sr);
                        List<Function> functions = parser.parse();
                        string gxlFilename = file + ".gxl";
                        if (!string.IsNullOrEmpty(gxlOutput))
                        {
                            gxlFilename = gxlOutput.EndsWith(".gxl") ? gxlOutput : gxlOutput + ".gxl";
                        }
                        GXLWriter.Save(gxlFilename, functions, "custom");

                        if (!string.IsNullOrEmpty(jlgOutput))
                        {
                            string jlgFilename = jlgOutput.EndsWith(".jlg") ? jlgOutput : jlgOutput + ".jlg";
                            JLGWriter.Save(jlgFilename, functions);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Error reading the file: {0}\nInner exception: {1}", e.Message, e.InnerException);
                    }
                } else
                {
                    Console.WriteLine("The format '{0}' is not supported yet!\nSupported formats are:\n{1}", format, string.Join(", ", formats.Keys));
                }
            }
        }
    }
}
