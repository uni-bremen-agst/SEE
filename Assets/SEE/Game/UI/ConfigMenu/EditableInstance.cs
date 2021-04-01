using System;
namespace SEE.Game.UI.ConfigMenu
{
    public class EditableInstance
    {
        private EditableInstance(string displayValue, string gameObjectName)
        {
            DisplayValue = displayValue;
            GameObjectName = gameObjectName;
        }
        public string DisplayValue { get; }
        public string GameObjectName { get; }

        public static EditableInstance Architecture => new EditableInstance("Arch", "Architecture");
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
