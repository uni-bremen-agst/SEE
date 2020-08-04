using System;
using System.Collections.Generic;

using UnityEngine;

namespace Assets.SEE.DataModel
{
    [Serializable]
    public class JavaStatement
    {
        /// <summary>
        /// This field will be filled with a number that refers to a Location in a Lookuptable
        /// Location = classname.methodname
        /// </summary>
        [SerializeField]
        private String location;

        /// <summary>
        /// The # codeline of this statement in its location.
        /// </summary>
        [SerializeField]
        private String line;

        /// <summary>
        /// Local Variables available at this statement. Can be empty.
        /// </summary>
        [SerializeField]
        private List<String> localVariables = new List<String>();

        /// <summary>
        /// Classfields changes executed by this statement. Can be empty.
        /// </summary>
        [SerializeField]
        private List<String> fieldChanges = new List<String>();

        /// <summary>
        /// The statement type. It can enter a method, exit a method or just be a basic statement within a method.
        /// </summary>
        [SerializeField]
        private String statementType;

        /// <summary>
        /// Empty Constructor. A new JavaStatement is filled step by step within the JLGParser.
        /// </summary>
        public JavaStatement() { }

        /// <summary>
        /// Sets the Statement Type to entry.
        /// </summary>
        public void setTypeEntry()
        {
            this.StatementType = "entry";
        }

        /// <summary>
        /// Sets the Statement Type to normal.
        /// </summary>
        public void setTypeNormal()
        {
            this.StatementType = "normal";
        }

        /// <summary>
        /// Sets the Statement Type to exit.
        /// </summary>
        public void setTypeExit()
        {
            this.StatementType = "exit";
        }


        ///Getters and setters.
        public string Location { get => location; set => location = value; }

        public string Line { get => line; set => line = value; }

        public List<string> LocalVariables { get => localVariables; set => localVariables = value; }

        public string StatementType { get => statementType; set => statementType = value; }

        public List<string> FieldChanges { get => fieldChanges; set => fieldChanges = value; }
    }
}
