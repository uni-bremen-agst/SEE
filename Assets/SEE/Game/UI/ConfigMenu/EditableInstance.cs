namespace SEE.Game.UI.ConfigMenu
{
    /// <summary>
    /// Enumerates the list of SEECity instances the config menu can manipulate.
    /// </summary>
    public class EditableInstance
    {
        private EditableInstance(string displayValue, string gameObjectName)
        {
            DisplayValue = displayValue;
            GameObjectName = gameObjectName;
        }

        /// <summary>
        /// The name that should be used to display this instance inside the menu.
        /// </summary>
        public string DisplayValue { get; }

        /// <summary>
        /// The name of the GameObject the SEECity is attached to.
        /// </summary>
        public string GameObjectName { get; }

        /// <summary>
        /// References the architecture SEECity.
        /// </summary>
        public static EditableInstance Architecture => new EditableInstance("Arch", "Architecture");

        /// <summary>
        /// References the implementation SEECity.
        /// </summary>
        public static EditableInstance Implementation =>
            new EditableInstance("Impl", "Implementation");

        protected bool Equals(EditableInstance other)
        {
            return GameObjectName == other.GameObjectName;
        }
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != GetType())
                return false;
            return Equals((EditableInstance)obj);
        }
        public override int GetHashCode()
        {
            return (GameObjectName != null ? GameObjectName.GetHashCode() : 0);
        }
    }
}
