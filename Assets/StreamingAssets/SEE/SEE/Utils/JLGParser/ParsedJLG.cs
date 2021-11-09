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
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Utils
{
    /// <summary>
    /// Thrown if the input contains a malformed statement.
    /// </summary>
    public class MalformedStatement : Exception
    {
        public MalformedStatement(string message) : base(message)
        {
        }
    }

    [Serializable]
    public class ParsedJLG
    {
        /// <summary>
        /// Contains a list with all paths to all java classes of the logged programm.
        /// </summary>
        [SerializeField]
        private IList<string> filesOfProject;

        /// <summary>
        /// Look-up table for the location of a JavaStatement. The number saved in the location of a 
        /// JavaStatement equals the index in this table of its location.
        /// </summary>   
        [SerializeField]
        private IList<string> locationLookupTable;

        /// <summary>
        /// Look-up table for the names of all fields in the parsed javalog. The number used to identify 
        /// a field is the index of its name in the lookuptable.
        /// </summary>
        [SerializeField]
        private IList<string> fieldLookupTable;

        /// <summary>
        /// List containing all parsed JavaStatements.
        /// </summary>
        [SerializeField]
        private List<JavaStatement> allStatements;

        /// <summary>
        /// This Stack contains all return Values, until the current Point in the Visualization. It is 
        /// filled and used, when a ParsedJLG object is being visualized by a JLGVisualizer script.
        /// </summary>
        [SerializeField]
        private Stack<String> returnValues = new Stack<string>();

        /// <summary>
        /// Constructs a new ParsedJLG.
        /// </summary>
        /// <param name="filesOfProject">the source files of the project</param>
        /// <param name="locationLookupTable">look-up table for the location of a JavaStatement</param>
        /// <param name="fieldLookupTable">look-up table for the names of all fields</param>
        /// <param name="allStatements">all parsed JavaStatements</param>
        public ParsedJLG(List<string> filesOfProject, IList<string> locationLookupTable, IList<string> fieldLookupTable, List<JavaStatement> allStatements)
        {
            this.filesOfProject = filesOfProject;
            this.locationLookupTable = locationLookupTable;
            this.fieldLookupTable = fieldLookupTable;
            this.allStatements = allStatements;
            DumpStatistics();
        }

        private void DumpStatistics()
        {
            Debug.Log($"[JLG] Number of files: {filesOfProject.Count}\n");
            Debug.Log($"[JLG] Number of fields: {fieldLookupTable.Count}\n");
            Debug.Log($"[JLG] Number of static statements: {locationLookupTable.Count}\n");
            Debug.Log($"[JLG] Number of executed statements: {allStatements.Count}\n");
        }

        /// <summary>
        /// This method returns the location for a given <paramref name="stmtIndex"/> in the list.
        /// </summary>
        /// <param name="stmtIndex">index of the Java statement in list <see cref="allStatements"/></param>
        /// <returns>The location string from <see cref="locationLookupTable"/></returns>
        /// <exception cref="MalformedStatement">thrown if a malformed statement is encountered</exception>
        public string GetStatementLocationString(int stmtIndex)
        {
            string location = locationLookupTable[int.Parse(allStatements[stmtIndex].Location)];
            int l = location.IndexOf('(');
            if (l < 0)
            {
                throw new MalformedStatement($"Malformed statement '{locationLookupTable[int.Parse(allStatements[stmtIndex].Location)]}' at statement index {stmtIndex}: '(' expected.");
            }
            location = location.Substring(0, l + 1);
            l = location.LastIndexOf('.');
            if (l < 0)
            {
                throw new MalformedStatement($"Malformed statement '{locationLookupTable[int.Parse(allStatements[stmtIndex].Location)]}' at statement index {stmtIndex}: '.' expected.");
            }
            location = location.Substring(0, l);
            return location;
        }

        /// <summary>
        /// This method looks up a coded Fieldname in the Fieldlookuptable
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        private string LookupFieldLocation(string s)
        {
            int i = s.IndexOf('=');
            return fieldLookupTable[int.Parse(s.Substring(0, i))] + s.Substring(i);
        }

        /// <summary>
        /// getter for the fields of a ParsedJLG
        /// </summary>
        public IList<string> FilesOfProject { get => filesOfProject; }
        public IList<string> LocationLookupTable { get => locationLookupTable; }
        public IList<string> FieldLookupTable { get => fieldLookupTable; }
        public List<JavaStatement> AllStatements { get => allStatements; }
        public Stack<string> ReturnValues { get => returnValues; set => returnValues = value; }

        /// <summary>
        /// This Methode creates the string, that visualizes the runtime data in the small text window.
        /// </summary>
        /// <param name="statementCounter"></param>
        /// <param name="AddReturnValueToStack">This should be true when the Visualization is running forward. Only then the return value is put on the stack.</param>
        /// <returns></returns>
        internal string CreateStatementInfoString(int statementCounter, Boolean AddReturnValueToStack)
        {
            JavaStatement js = allStatements[statementCounter];
            string info = "Line " + js.Line + Environment.NewLine;

            //Add local variables to info string
            if (js.LocalVariables.Count != 0)
            {
                info = info + "Local variables accessible at this line:";
                foreach (string s in js.LocalVariables)
                {
                    info = info + Environment.NewLine + s;
                }
            }

            //Add field changes to info string
            if (js.FieldChanges.Count != 0)
            {
                info = info + Environment.NewLine + "Field Changes at this line:";
                foreach (string s in js.FieldChanges)
                {
                    info = info + Environment.NewLine + LookupFieldLocation(s);
                }
            }

            //Add last return Value
            if (returnValues.Count != 0)
            {
                info = info + Environment.NewLine + "Last Return: " + returnValues.Peek();
            }

            //Add the return value of this statement (and its method) to the info string
            if (js.ReturnValue != null)
            {
                info = info + Environment.NewLine + locationLookupTable[int.Parse(js.Location)] + " returns: " + js.ReturnValue;
                if (AddReturnValueToStack)
                {
                    returnValues.Push(locationLookupTable[int.Parse(js.Location)] + " returned " + js.ReturnValue);
                }
            }
            return info;
        }
    }
}
