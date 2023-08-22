using System.Reflection;
using System.Text.RegularExpressions;

namespace profiling2gxl
{
    /// <summary>
    /// Parser for GNU gprof output.
    /// </summary>
    internal class GprofParser : Parser
    {
        /// <summary>
        /// The regular expression of the call graph headers.
        /// </summary>
        private readonly Regex _headerRegex = new(
            @"^\s+called/total\s+parents\s*$|" +
            @"^index\s+%time\s+self\s+descendents\s+called\+self\s+name\s+index\s*$|" +
            @"^\s+called/total\s+children\s*$|" +
            @"^index\s+%\s+time\s+self\s+children\s+called\s+name\s*$");

        /// <summary>
        /// The regular expression of an unknown function in the call graph entry.
        /// </summary>
        private readonly Regex _unknownRegex = new(@"^\s+<spontaneous>\s*$|" + @"^.*\((\d+)\)$", RegexOptions.Compiled);

        /// <summary>
        /// The regular expression of the primary function in the call graph entry.
        /// </summary>
        private readonly Regex _primaryRegex = new(
            @"^\[(?<id>\d+)\]?\s+(?<percentage_time>\d+\.\d+)\s+(?<self>\d+\.\d+)\s+(?<descendants>\d+\.\d+)\s+(?:(?<called>\d+)(?:\+(?<called_self>\d+))?)?\s+((?<module>[a-zA-Z\s]*)(::){1})?(?<name>\S.*?)?(?:\s+<cycle\s(?<cycle>\d+)>)?\s\[(\d+)\]$",
            RegexOptions.Compiled
        );

        /// <summary>
        /// The regular expression of the parent function in the call graph entry that calls the primary function.
        /// </summary>
        private readonly static Regex _parentRegex = new(
            @"^\s+(?<self>\d+\.\d+)?\s+(?<descendants>\d+\.\d+)?\s+(?<called>\d+)(?:\/(?<called_total>\d+))?\s+((?<module>[a-zA-Z\s]*)(::){1})?(?<name>\S.*?)?(?:\s+<cycle\s(?<cycle>\d+)>)?\s\[(?<id>\d+)\]$",
            RegexOptions.Compiled
        );

        /// <summary>
        /// The regular expression of the children functions in the call graph entry that are called by the primary function.
        /// </summary>
        private readonly Regex _childRegex = _parentRegex;

        /// <summary>
        /// The regular expression to identify "cycle as a whole" entries.
        /// </summary>
        private readonly Regex _cycleAsAWholeRegex = new(@"^\[(\d+)\]?\s+(\d+\.\d+)\s+(\d+\.\d+)\s+(\d+\.\d+)\s+((\d+)\+(\d+))\s+(<cycle\s(\d+)\sas\sa\swhole>)\s\[(\d+)\]$", RegexOptions.Compiled);

        /// <summary>
        /// The regular expression of the call graph entries separator.
        /// </summary>
        private readonly Regex _sepRegex = new Regex(@"^--+$");

        /// <summary>
        /// The constructor of the GNU gprof output parser.
        /// </summary>
        /// <param name="sr">The stream reader of the file to parse.</param>
        public GprofParser(StreamReader sr) : base(sr) { }

        /// <summary>
        /// Reads a line of characters from the stream reader, removes trailing \r and \n and returns the data as a string.
        /// </summary>
        /// <returns>The next line from the stream reader, or  <see langword="null"/> if the end of the stream reader is reached.</returns>
        /// <exception cref="OutOfMemoryException"></exception>
        /// <exception cref="IOException"></exception>
        private string ReadLine()
        {
            var line = Sr.ReadLine() ?? throw new Exception("Error: Unexpected end of file");
            char[] charsToRemove = { '\r', '\n' };
            line = line.TrimEnd(charsToRemove);
            return line;
        }

        /// <summary>
        /// Extracts a structure from a match object, while translating the types in the process.
        /// </summary>
        /// <param name="mo">The match object to translate</param>
        /// <returns>The converted function of the given match object</returns>
        private Function Translate(Match mo)
        {
            var groupdict = mo.Groups.Cast<Group>().ToDictionary(g => g.Name, g => g.Value);

            var func = new Function();

            foreach (var pair in groupdict)
            {
                var value = pair.Value;

                var name = Helper.SnakeToPascalCase(pair.Key);

                PropertyInfo? property = func.GetType().GetProperty(name);
                if (property != null && property.CanWrite && !String.IsNullOrEmpty(value))
                {
                    object changed_type_value = Convert.ChangeType(value, property.PropertyType);
                    if (property.PropertyType == typeof(float))
                    {
                        changed_type_value = Convert.ChangeType(value.Replace(".", ","), property.PropertyType);
                    }
                    property.SetValue(func, changed_type_value, null);
                }
            }
            if (string.IsNullOrEmpty(func.Module)) {
                func.Module = "UnknownClass";
            }
            return func;
        }

        private void ParseFunctionEntry(List<string> lines)
        {
            List<Function> parents = new();
            List<string> children = new();
            string line;
            while (true)
            {
                if (lines.Count == 0)
                {
                    Console.Error.Write("warning: unexpected end of entry\n");
                }
                line = lines[0];
                lines.RemoveAt(0);
                // Ensure first function line doesn't start with index -> most likely a cycle
                if (line.StartsWith("["))
                {
                    break;
                }

                Match mo = _parentRegex.Match(line);
                if (mo.Success)
                {
                    if (mo.Groups.TryGetValue("id", out var idGroup))
                    {
                        var func = Functions.Find(f => f.Id == idGroup.Value);
                        if (func != null)
                        {
                            if (mo.Groups.TryGetValue("called_total", out var ctGroup))
                            {
                                func.Called += int.Parse(ctGroup.Value);
                            }
                        }
                        else
                        {
                            Function parent = Translate(mo);
                            parents.Add(parent);
                            if (!Functions.Contains(parent))
                            {
                                Functions.Add(parent);
                            }
                        }
                    }
                    
                }
                else
                {
                    if (_unknownRegex.Match(line).Success)
                    {
                        var func = Functions.Find(f => f.Name == "Unknown");
                        func ??= new()
                            {
                                Id = "UnknownClass:Unknown",
                                Name = "Unknown",
                                Module = "UnknownClass",
                                Called = 1,
                            };
                        parents.Add(func);
                        if (!Functions.Contains(func))
                        {
                            Functions.Add(func);
                        }
                    }
                    else
                    {
                    Console.Error.Write("warning: unrecognized call graph entry: {0}\n", line);
                    }
                }
            }

            Match primaryMatch = _primaryRegex.Match(line);
            Function function;
            if (primaryMatch.Success && primaryMatch.Groups.TryGetValue("id", out var primaryIdGroup))
            {
                function = Functions.Find(f => f.Id == primaryIdGroup.Value) ?? Translate(primaryMatch);
            }
            else
            {
                Console.Error.Write("warning: unrecognized call graph entry: {0}\n", line);
                return;
            }

            while (lines.Count > 0)
            {
                line = lines[0];
                lines.RemoveAt(0);

                Match mo = _childRegex.Match(line);
                if (!mo.Success)
                {
                    if (_unknownRegex.Match(line).Success)
                    {
                        continue;
                    }
                    Console.Error.Write("warning: unrecognized call graph entry: {0}\n", line);
                }
                else
                {

                    if (mo.Groups.TryGetValue("id", out var idGroup))
                    {
                        var child = Functions.Find(f => f.Id == idGroup.Value);
                        if (child != null)
                        {
                            if (mo.Groups.TryGetValue("called_total", out var ctGroup))
                            {
                                child.Called += int.Parse(ctGroup.Value);
                            }
                        }
                        else
                        {
                            child = Translate(mo);
                        }
                        
                        children.Add($"{child.Module}:{child.Name}");
                        if (!Functions.Contains(child))
                        {
                            Functions.Add(child);
                        }
                    }
                }
            }

            function.Parents = parents;
            function.Children = children;

            if (Functions.Find(f => f.Id == function.Id) == null)
            {
                Functions.Add(function);
            }

            parents.ForEach(parent =>
            {
                if (parent.Children.Find(id => id == $"{function.Module}:{function.Name}") == null)
                {
                    parent.Children.Add(new($"{function.Module}:{function.Name}"));
                }
            });
        }

        private void ParseCgEntry(List<string> lines)
        {
            if (lines.Exists(l => _cycleAsAWholeRegex.Match(l).Success))
            {
                return; //Ignore cycle entry
            }
            else
            {
                ParseFunctionEntry(lines);
            }
        }

        private void HarmoniseIds(List<Function> functions)
        {
            foreach (var f in functions)
            {
                f.Id = $"{f.Module}:{f.Name}";
                HarmoniseIds(f.Parents);
            }
        }

        //Parse the call graph
        private void ParseCg()
        {
            // skip call graph header
            while (!_headerRegex.Match(ReadLine()).Success)
            {
                // do nothing
            }
            string line = ReadLine();
            while (_headerRegex.Match(line).Success)
            {
                line = ReadLine();
            }

            // process call graph entries
            List<string> entry_lines = new List<string>();
            while (line != "\f") // form feed
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    if (_sepRegex.Match(line).Success)
                    {
                        ParseCgEntry(entry_lines);
                        entry_lines = new List<string>();
                    }
                    else
                    {
                        entry_lines.Add(line);
                    }
                }
                line = ReadLine();
            }
            HarmoniseIds(Functions);
        }


        public override List<Function> parse()
        {
            ParseCg();
            Sr.Close();
            return Functions;
        }
    }
}
