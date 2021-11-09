// Copyright 2020 Lennart Kipka
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS
// BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR
// IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace SEE.Utils
{
    public class JLGParser
    {
        /// <summary>
        /// Path to File/File to be parsed. Cannot be null!
        /// </summary>
        [SerializeField]
        private String file;

        /// <summary>
        /// List with all parsed javaStatements.
        /// </summary>
        private List<JavaStatement> javaStatements = new List<JavaStatement>();

        /// <summary>
        /// List with paths to all java classes of the origin Project of parsed log.
        /// </summary>
        [SerializeField]
        private List<string> filesOfProject = new List<string>();

        /// <summary>
        /// Explanation in ParsedJLG.
        /// </summary>
        [SerializeField]
        private List<string> locationLookupTable = new List<string>();

        /// <summary>
        /// Explanation in ParsedJLG.
        /// </summary>
        [SerializeField]
        private List<string> fieldLookupTable = new List<string>();

        /// <summary>
        /// The current java Statement, that is being filled with information. Will be added to JavaStatements, when a new statement starts.
        /// </summary>
        private JavaStatement javaStatement;

        /// <summary>
        /// Constructs a new JLGParser.
        /// </summary>
        /// <param name="file">JLG file to be parsed. Cannot be null</param>
        public JLGParser(string file)
        {
            this.file = file ?? throw new ArgumentException("File Path can not be null!");
        }

        /// <summary>
        /// Parses a JLG File. Calls parseNewLine, when a new Line is detected.
        /// </summary>
        /// <returns>a ParsedJLG object that contains all the information of the parsed JLG File.</returns>
        public ParsedJLG Parse()
        {
            foreach (string line in File.ReadLines(file))
            {
                //New Line
                if (line.StartsWith("-/"))
                {
                    ParseNewLine(line, true);
                    javaStatement.SetTypeEntry();
                }
                //normal line
                else if (line.StartsWith("-"))
                {
                    ParseNewLine(line, false);
                    javaStatement.SetTypeNormal();
                }
                //exit line
                else if (line.StartsWith("/-"))
                {
                    ParseNewLine(line, false);
                    javaStatement.SetTypeExit();
                }
                //field change
                else if (line.StartsWith("#"))
                {
                    if (!(javaStatement == null))
                    {
                        javaStatement.FieldChanges.Add(line.TrimStart('#'));
                    }
                }
                //check for return value
                else if (line.StartsWith("=>"))
                {
                    int i = line.IndexOf('>');
                    javaStatement.ReturnValue = line.Substring(i + 1);
                }
                //List with filepaths
                else if (line.StartsWith("$"))
                {
                    //Filepaths can't contain the char ','.
                    string entry = "";
                    for (int i = 2; i < line.Length; i++)
                    {
                        if (line[i] == ',' || line[i] == ']')
                        {
                            filesOfProject.Add(entry);
                            entry = "";
                        }
                        else if (line[i] == ' ')
                        {
                            // do nothing in case of space
                        }
                        else
                        {
                            entry = entry + line[i];
                        }
                    }
                }
                // Lookuptables start with *
                else if (line.StartsWith("*"))
                {
                    string table = line.Substring(1);
                    //Fill LocationLookupTable
                    string locationLookupTableRegex = "-\\d+=([A-Za-z0-9.,\\s()<>\\[\\]]*)";
                    var regex1 = new Regex(locationLookupTableRegex);
                    foreach (Match m in regex1.Matches(table))
                    {
                        locationLookupTable.Add(m.Groups[1].Value);
                    }
                    //Fill FieldLookupTable
                    string fieldLookUpTableRegex = "#\\d+=([A-Za-z0-9]*)";
                    var regex2 = new Regex(fieldLookUpTableRegex);
                    foreach (Match m in regex2.Matches(table))
                    {
                        fieldLookupTable.Add(m.Groups[1].Value);
                    }
                }
                // if line in JLG starts with no special char, its a local variable
                else if (javaStatement != null)
                {
                    javaStatement.LocalVariables.Add(line);
                }
            }
            javaStatements.Add(javaStatement); // make sure the last javastatement is also added
            ParsedJLG parsed = new ParsedJLG(filesOfProject, locationLookupTable, fieldLookupTable, javaStatements);
            return parsed;
        }

        /// <summary>
        /// This method parses a 'new line statement' in a JLG file by matching the Location with a regex and parsing the line number and saving these two in the current
        /// javaStatement.
        /// </summary>
        /// <param name="line">the text line in the JLG</param>
        /// <param name="entry">True, when its the first line of a Method</param>
        public void ParseNewLine(String line, Boolean entry)
        {
            if (!(javaStatement == null))
            {
                javaStatements.Add(javaStatement);
            }
            javaStatement = new JavaStatement();
            if (!entry)
            {
                javaStatement.Location = Regex.Match(line, "-(\\d+)>", 0).Groups[1].Value;
            }
            else
            {
                javaStatement.Location = Regex.Match(line, "-/(\\d+)>", 0).Groups[1].Value;
            }
            int lineNumberStart = line.IndexOf(">");
            javaStatement.Line = line.Substring(lineNumberStart + 1);
        }
    }
}
