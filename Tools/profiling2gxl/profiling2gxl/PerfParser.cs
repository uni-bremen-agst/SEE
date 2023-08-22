using System.Reflection;
using System.Text.RegularExpressions;

namespace profiling2gxl
{
    /// <summary>
    /// Parser for Linux perf output.
    /// 
    /// Expects file contains combined output of
    ///     perf (mem) record -g
    ///     perf script
    ///     perf report
    ///     
    /// Note that script output should be before report output!
    /// </summary>
    internal class PerfParser : Parser
    {
        /// <summary>
        /// The regular expression for the performance counter profile information.
        /// </summary>
        private readonly Regex _performanceRegex = new(@"^\s+(?<descendants>[0-9]+\.[0-9]+)%\s+(?<self>[0-9]+\.[0-9]+)%\s+(?<command>[a-zA-Z0-9]+)\s+(?<module>.+[^\s])\s+(\[.*\])\s+(?<name>.+)$", RegexOptions.Compiled);

        /// <summary>
        /// The regular expression for the perf trace output.
        /// </summary>
        private readonly Regex _traceEntryRegex = new(@"^\s+(?<address>[0-9a-fA-F]+)\s+(?<name>.*)\s+\((?<module>.*)\)$", RegexOptions.Compiled);

        /// <summary>
        /// The regular expression for the suffix '+0x...' in function name.
        /// </summary>
        private readonly Regex _nameSuffixRegex = new(@"\+0x[0-9a-fA-F]+$", RegexOptions.Compiled);

        /// <summary>
        /// The regular expression for the header of 'perf report' output. With the event match group the parser determines if 'perf' or 'perf mem' was executed.
        /// </summary>
        private readonly Regex _reportHeaderRegex = new(@"^#\s*(Samples:)\s+(?<samples_count>\d+).*'(?<event>.*)'$", RegexOptions.Compiled);

        public PerfParser(StreamReader Sr) : base(Sr) { }

        /// <summary>
        /// Reads a line of characters from the stream reader, removes trailing \r and \n and returns the data as a string.
        /// </summary>
        /// <returns>The next line from the stream reader, or  <see langword="null"/> if the end of the stream reader is reached.</returns>
        /// <exception cref="OutOfMemoryException"></exception>
        /// <exception cref="IOException"></exception>
        private string? ReadLine()
        {
            var line = Sr.ReadLine();
            if (line == null) return line;
            char[] charsToRemove = { '\r', '\n' };
            line = line.TrimEnd(charsToRemove);
            return line;
        }

        private void ParsePerformanceProfile(Match mo, string evnt)
        {
            if (mo.Groups.TryGetValue("name", out var nameGroup) && mo.Groups.TryGetValue("module", out var moduleGroup))
            {
                var name = nameGroup.Value;
                var module = moduleGroup.Value;
                if (module == "[unknown]")
                {
                    name = "Unknown";
                    module = "UnknownClass";
                }

                var func = Functions.Find(f => f.Id == $"{module}:{name}");
                func ??= new()
                {
                    Id = $"{module}:{name}",
                    Name = name,
                    Module = module,
                    Called = 1,
                    Filename = module
                };

                var descendantsGroup = mo.Groups.GetValueOrDefault("descendants");
                if (descendantsGroup != null)
                {
                    func.Descendants += float.Parse(descendantsGroup.Value.Replace(".", ","));
                }

                var selfGroup = mo.Groups.GetValueOrDefault("self");
                if (selfGroup != null)
                {
                    var self = float.Parse(selfGroup.Value.Replace(".", ","));
                    if (string.IsNullOrEmpty(evnt))
                    {
                        func.Self += self;
                    }
                    else
                    {
                        if (evnt.Contains("loads"))
                        {
                            func.MemLoad += self;
                        } else if (evnt.Contains("stores"))
                        {
                            func.MemStore += self;
                        }
                        else
                        {
                            func.Self += self;
                        }
                    }
                }

                if (!Functions.Contains(func))
                {
                    Functions.Add(func);
                }
            }
        }

        private void ParseCg(List<Match> mos)
        {
            string? callee = null;
            foreach (var mo in mos)
            {
                if (mo.Groups.TryGetValue("name", out var name_group) && mo.Groups.TryGetValue("module", out var module_group))
                {
                    var name = _nameSuffixRegex.Replace(name_group.Value, "");
                    if (name == "[unknown]")
                    {
                        name = "Unknown";
                    }

                    var module = Path.GetFileName(module_group.Value);
                    var filename = module;
                    if (module == "[unknown]")
                    {
                        module = "UnknownClass";
                        filename = "";
                    }

                    var path = Path.GetDirectoryName(module_group.Value);
                    if (path == "[unknown]")
                    {
                        path = "";
                    }

                    var func = Functions.Find(f => f.Id == $"{module}:{name}");
                    if (func != null)
                    {
                        func.Called += 1;
                    }
                    else
                    {
                        func = new()
                        {
                            Id = $"{module}:{name}",
                            Name = name,
                            Module = module,
                            Called = 1,
                            Filename = filename,
                            Path = path ?? ""
                        };
                        Functions.Add(func);
                    }

                    if (callee != null && func.Id != callee)
                        {
                        func.Children.Add(callee);
                        var calleeFunc = Functions.Find(f => f.Id == callee);
                        calleeFunc?.Parents.Add(func);
                    }

                    callee = func.Id;
                }
            }
        }

        private void EnsureIsCalled()
        {
            var unknownFunction = Functions.Find(f => f.Id == "UnknownClass:Unknown");
            unknownFunction ??= new()
                {
                    Name = "Unknown",
                    Called = 1,
                    Module = "UnknownClass",
                    Id = "UnknownClass:Unknown"
            };

            // Skip first function because its the start entry
            foreach(var func in Functions.Skip(1))
            {
                if (func.Parents.Count == 0)
                {
                    func.Parents.Add(unknownFunction);
                    unknownFunction.Children.Add($"{func.Module}:{func.Name}");
                }
            }
        }

        public override List<Function> parse()
        {
            var line = ReadLine();
            List<Match> mos = new();
            bool isTraceMatch = false;
            string evnt = "";
            while (line != null)
            {
                Match headerMatch = _reportHeaderRegex.Match(line);
                if (headerMatch.Success)
                {
                    var evntGroup = headerMatch.Groups.GetValueOrDefault("event");
                    if (evntGroup != null && evntGroup.Value.Contains("mem"))
                    {
                        evnt = evntGroup.Value;
                    }
                }
                Match mo = _performanceRegex.Match(line);
                if (mo.Success)
                {
                    ParsePerformanceProfile(mo, evnt);
                }
                mo = _traceEntryRegex.Match(line);
                if (mo.Success)
                {
                    mos.Add(mo);
                    isTraceMatch = true;
                }
                else if (isTraceMatch && mos.Count > 0)
                {
                    ParseCg(mos);
                    mos.Clear();
                    isTraceMatch = false;
                }
                line = ReadLine();
            }
            EnsureIsCalled();
            return Functions;
        }
    }
}
