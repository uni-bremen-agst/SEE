using System;

namespace SEE.Game.UI.RuntimeConfigMenu
{
    public class RuntimeFoldoutAttribute : Attribute
    {
        public string name;

        public RuntimeFoldoutAttribute(string name)
        {
            this.name = name;
        }
    }
}