namespace SEE.DataModel
{
    /// <summary>
    /// Specifies attributable objects with named toggle, int, float, and string attributes.
    /// </summary>
    public interface IAttributable
    {
        float GetFloat(string attributeName);
        void SetFloat(string attributeName, float value);
        bool TryGetFloat(string attributeName, out float value);

        int GetInt(string attributeName);
        void SetInt(string attributeName, int value);
        bool TryGetInt(string attributeName, out int value);

        bool TryGetNumeric(string attributeName, out float value);

        string GetString(string attributeName);
        void SetString(string attributeName, string value);
        bool TryGetString(string attributeName, out string value);

        bool HasToggle(string attributeName);
        void SetToggle(string attributeName);


        string ToString();
    }
}