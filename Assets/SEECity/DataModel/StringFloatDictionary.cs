/// <summary>
/// A serializable Dictionary<string, float>. Serializable dictionaries are 
/// needed for values that are created in the editor and need to be preserved
/// and available during the game. For such values, Unity must serialize
/// the objects and restore them when the game is started.
/// </summary>
[System.Serializable]
public class StringFloatDictionary : SerializableDictionary<string, float> { }
