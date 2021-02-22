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
    [Serializable]
    public class ParsedJLG
    {
        /// <summary>
        /// Contains a list with all paths to all java classes of the logged programm.
        /// </summary>
        [SerializeField]
        private List<string> filesOfProject;

        /// <summary>
        /// Lookuptable for the location of a JavaStatement. The number saved in the location of a JavaStatement equals the index in this table of its location.
        /// </summary>   
        [SerializeField]
        private List<string> locationLookupTable;

        /// <summary>
        /// Lookuptable for the names of all fields in the parsed javalog. The number used to identify a field is the index of its name in the lookuptable.
        /// </summary>
        [SerializeField]
        private List<string> fieldLookupTable;

        /// <summary>
        /// List containing all parsed JavaStatements.
        /// </summary>
        [SerializeField]
        private List<JavaStatement> allStatements;

        /// <summary>
        /// This Stack contains all return Values, until the current Point in the Visualization. It is filled and used, when a ParsedJLG object is being visualized by a JLGVisualizer script.
        /// </summary>
        [SerializeField]
        private Stack<String> returnValues = new Stack<string>();

        /// <summary>
        /// Constructs a new ParsedJLG.
        /// </summary>
        /// <param name="filesOfProject"></param>
        /// <param name="locationLookupTable"></param>
        /// <param name="fieldLookupTable"></param>
        /// <param name="allStatements"></param>
        public ParsedJLG(List<string> filesOfProject, List<string> locationLookupTable, List<string> fieldLookupTable, List<JavaStatement> allStatements)
        {
            this.filesOfProject = filesOfProject;
            this.locationLookupTable = locationLookupTable;
            this.fieldLookupTable = fieldLookupTable;
            this.allStatements = allStatements;
        }

        public ParsedJLG() { }

        /// <summary>
        /// This Method returns the Location for a given Index in the list.
        /// </summary>
        /// <param name="i">index of the Java statement in List allStatements</param>
        /// <returns>The Location String from LocationLookupTable</returns>
        public string GetStatementLocationString(int i) {
            string location = locationLookupTable[int.Parse(allStatements[i].Location)];
            int l = location.IndexOf('(');
            location = location.Substring(0, l + 1);
            l = location.LastIndexOf('.');
            location = location.Substring(0, l);
            return location;
        }

        /// <summary>
        /// This method looks up a coded Fieldname in the Fieldlookuptable
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        private string LookupFieldLocation(string s) {
            int i = s.IndexOf('=');
            return fieldLookupTable[int.Parse(s.Substring(0, i))] + s.Substring(i); 
        }

        /// <summary>
        /// getter for the fields of a ParsedJLG
        /// </summary>
        public List<string> FilesOfProject { get => filesOfProject; }
        public List<string> LocationLookupTable { get => locationLookupTable; }
        public List<string> FieldLookupTable { get => fieldLookupTable; }
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
                info = info + Environment.NewLine+ locationLookupTable[int.Parse(js.Location)] + " returns: "+ js.ReturnValue;
                if (AddReturnValueToStack)
                {
                    returnValues.Push(locationLookupTable[int.Parse(js.Location)] +  " returned "+js.ReturnValue);
                }
            }
            return info;
        }
    }
}
