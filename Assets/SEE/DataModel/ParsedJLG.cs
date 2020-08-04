using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.SEE.DataModel
{
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

        /// <summary>
        /// getter for the fields of a ParsedJLG
        /// </summary>
        public List<string> FilesOfProject { get => filesOfProject; }
        public List<string> LocationLookupTable { get => locationLookupTable; }
        public List<string> FieldLookupTable { get => fieldLookupTable; }
        public List<JavaStatement> AllStatements { get => allStatements; }

    }
}
