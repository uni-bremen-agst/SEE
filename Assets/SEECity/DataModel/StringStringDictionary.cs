/// <summary>
/// A serializable Dictionary<string, string>. Serializable dictionaries are 
/// needed for values that are created in the editor and need to preserved
/// and available during the game. For such values, Unity must serialize
/// the objects and restore them when the game is started.
/// </summary>
[System.Serializable]
public class StringStringDictionary : SerializableDictionary<string, string> {}

