using System;
using System.Collections.Generic;
using System.Linq;

namespace SEE.Controls.Actions.Architecture
{
    /// <summary>
    /// The type of an architecture action.
    /// Similar to the <see cref="ActionStateType"/>
    /// Implemented using the "Enumeration" (not enum) or "type safe enum" pattern.
    /// The following two pages have been used for reference:
    /// <ul>
    /// <li>https://docs.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/enumeration-classes-over-enum-types#implement-an-enumeration-base-class</li>
    /// <li>https://ardalis.com/enum-alternatives-in-c/</li>
    /// </ul>
    /// </summary>
    public class ArchitectureActionType
    {
        
        /// <summary>
        /// List of all available action state types
        /// </summary>
        public static List<ArchitectureActionType> AllTypes { get; } =  new List<ArchitectureActionType>();
        
        public static ArchitectureActionType Draw { get; } = new ArchitectureActionType("Draw", 0, 
            "Prefabs/Architecture/UI/DrawButton", DrawArchitectureAction.NewInstance);

        public static ArchitectureActionType Move { get; } = new ArchitectureActionType("Move", 1,
            "Prefabs/Architecture/UI/MoveButton", MoveArchitectureAction.NewInstance);

        public static ArchitectureActionType Select { get; } = new ArchitectureActionType("Edit", 2,
            "Prefabs/Architecture/UI/SelectButton", SelectArchitectureAction.NewInstance);

        public static ArchitectureActionType View { get; } = new ArchitectureActionType("View", 3,
            "Prefabs/Architecture/UI/ViewButton", ViewArchitectureAction.NewInstance);

        public static ArchitectureActionType Scale { get; } = new ArchitectureActionType("Scale", 4,
            "Prefabs/Architecture/UI/ScaleButton", ScaleArchitectureAction.NewInstance);

        /// <summary>
        /// The name of this action.
        /// </summary>
        public string Name { get; }
        
        /// <summary>
        /// Numeric value of this action. Must be unique across all entries.
        /// </summary>
        public int Value { get; }
        
        
        /// <summary>
        /// Path to the button prefab for this action type.
        /// </summary>
        public string PrefabPath { get; }
        
        /// <summary>
        /// Delegate that can be called to create a new instance of this kind of action.
        /// </summary>
        public CreateAbstractArchitectureAction CreateAbstractArchitectureAction { get; }
        
        

        /// <summary>
        /// Constructor for new <see cref="ArchitectureActionType"/>.
        /// </summary>
        /// <param name="name">The name of the action type</param>
        /// <param name="value">The id of this action type. Must be unique and increased by one for each new implementation</param>
        /// <param name="prefabPath">The path to the icon material for this action type</param>
        /// <param name="createAbstractArchitectureAction">Delegate to create new instances of this type</param>
        /// <exception cref="ArgumentException">When the given <paramref name="name"/> or <paramref name="value"/>
        /// are not unqiue, or when the <paramref name="value"/> does not fulfill the criteria</exception>
        private ArchitectureActionType(string name, int value, string prefabPath, CreateAbstractArchitectureAction createAbstractArchitectureAction)
        {
            Name = name;
            Value = value;
            PrefabPath = prefabPath;
            CreateAbstractArchitectureAction = createAbstractArchitectureAction;
            //Check for duplicates
            if (AllTypes.Any(x => x.Value == value || x.Name == name))
            {
                throw new ArgumentException("Duplicate ArchitectureActionTypes may not exist!\n");
            }

            if (value != AllTypes.Select(x => x.Value + 1).DefaultIfEmpty(0).Last())
            {
                throw new ArgumentException("ArchitectureActionType IDs must be increasing by one!\n");
            }
            // Add new value to list of all types
            AllTypes.Add(this);
        }


        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            return obj.GetType() == GetType() && ((ArchitectureActionType) obj).Value == Value;
        }

        public override int GetHashCode()
        {
            return Value;
        }
    }
}