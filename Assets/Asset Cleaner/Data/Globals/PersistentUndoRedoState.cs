using System;
using System.Collections.Generic;

namespace Asset_Cleaner {
    [Serializable]
    class PersistentUndoRedoState {
        public List<SelectionEntry> History = new List<SelectionEntry>();
        public int Id;

        public void Deconstruct(out List<SelectionEntry> list, out int id) {
            id = Id;
            list = History;
        }
    }
}