/// <summary>
/// A serializable Dictionary<string, INode>. Serializable dictionaries are 
/// needed for values that are created in the editor and need to be preserved
/// and available during the game. For such values, Unity must serialize
/// the objects and restore them when the game is started.
/// </summary>

namespace SEE.DataModel
{
    [System.Serializable]
    public class StringNodeDictionary : SerializableDictionary<string, INode> { }
}
