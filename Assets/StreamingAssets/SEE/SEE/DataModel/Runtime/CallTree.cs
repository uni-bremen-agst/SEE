using SEE.DataModel.DG;
using SEE.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.DataModel.Runtime
{

    /// <summary>
    /// Represents a category of a call tree.
    /// </summary>
    [Serializable]
    public class CallTreeCategory
    {
        /// <summary>
        /// The name of the category.
        /// </summary>
        [SerializeField]
        private readonly string name;
        public string Name { get => name; }

        /// <summary>
        /// Constructs category of given name.
        /// </summary>
        /// <param name="name">The name of the category.</param>
        public CallTreeCategory(string name)
        {
            this.name = name ?? throw new ArgumentException("'name' must not be null!");
        }

        /// <summary>
        /// Outputs the category as a string in DYN-format.
        /// </summary>
        /// <returns>The category as a string in DYN-format.</returns>
        public override string ToString()
        {
            return '\"' + name + '\"';
        }
    }

    /// <summary>
    /// Contains all categories of a call tree.
    /// </summary>
    [Serializable]
    public class CallTreeCategories
    {
        /// <summary>
        /// Categories of a call tree.
        /// </summary>
        [SerializeField]
        private readonly CallTreeCategory[] categories;
        public CallTreeCategory[] Categories { get => categories; }

        /// <summary>
        /// The count of categories.
        /// </summary>
        public int Count { get { return categories.Length; } }

        /// <summary>
        /// Constructs categories out of given categories.
        /// </summary>
        /// <param name="categories">The categories.</param>
        public CallTreeCategories(CallTreeCategory[] categories)
        {
            this.categories = categories ?? throw new ArgumentException("'categories' must not be null!");
        }

        /// <summary>
        /// Constructs categories. Each of the given categories will be converted to
        /// <see cref="CallTreeCategory"/>.
        /// </summary>
        /// <param name="categories">The categories as strings.</param>
        public CallTreeCategories(string[] categories)
        {
            if (categories == null)
            {
                throw new ArgumentException("'categories' must not be null!");
            }
            this.categories = new CallTreeCategory[categories.Length];
            for (int i = 0; i < categories.Length; i++)
            {
                this.categories[i] = new CallTreeCategory(categories[i]);
            }
        }

        /// <summary>
        /// Returns the category at given index.
        /// </summary>
        /// <param name="index">The index of the category.</param>
        /// <returns>The category at given index or <code>null</code>, if out of bounds.
        /// </returns>
        public CallTreeCategory At(int index)
        {
            if (index < 0 || index >= categories.Length)
            {
                Debug.LogWarning("'index' is out of range!");
                return null;
            }
            return categories[index];
        }

        /// <summary>
        /// Outputs the categories as a string in DYN-format.
        /// </summary>
        /// <returns>The categories as a string in DYN-format.</returns>
        public override string ToString()
        {
            string str = "";
            string delimiter = " ";
            for (int i = 0; i < categories.Length; i++)
            {
                str += delimiter + categories[i].ToString();
            }
            return str;
        }
    }

    /// <summary>
    /// Represents a function call of a call tree.
    /// </summary>
    [Serializable]
    public class CallTreeFunctionCall
    {
        /// <summary>
        /// The categories of the call tree.
        /// </summary>
        [SerializeField]
        private readonly CallTreeCategories categories;

        /// <summary>
        /// The predecessor function call. Can not be serialized due to depth limits of
        /// serialization.
        /// </summary>
        public CallTreeFunctionCall predecessor;

        /// <summary>
        /// The successor function calls. Can not be serialized due to depth limits of
        /// serialization.
        /// </summary>
        public List<CallTreeFunctionCall> successors;

        /// <summary>
        /// The attributes of the function call.
        /// </summary>
        [SerializeField]
        private readonly string[] attributes;

        /// <summary>
        /// The <see cref="GameObject"/>, that represents this function call as a
        /// building.
        /// </summary>
        public GameObject node;

        /// <summary>
        /// The count of the attributes.
        /// </summary>
        public int AttributeCount { get { return attributes.Length; } }

        /// <summary>
        /// Constructs a new function call. The count of function call attributes must be
        /// equivalent to the count of categories.
        /// </summary>
        /// <param name="categories">The categories of the call tree.</param>
        /// <param name="attributes">The attributes of the function call.</param>
        public CallTreeFunctionCall(CallTreeCategories categories, string[] attributes)
        {
            this.categories = categories ?? throw new ArgumentException("'categories' must not be null!");
            this.attributes = attributes ?? throw new ArgumentException("'attributes' must not be null!");
            if (categories.Count != attributes.Length)
            {
                throw new IncorrectAttributeCountException(categories.Count, attributes.Length);
            }
        }

        /// <summary>
        /// Returns the attribute at given index.
        /// </summary>
        /// <param name="index">The index of the attribtue.</param>
        /// <returns>The attribute at given index or <code>null</code>, if out of bounds.
        /// </returns>
        public string GetAttributeAt(int index)
        {
            if (index < 0 || index >= attributes.Length)
            {
                Debug.LogWarning("'index' is out of range!");
                return null;
            }
            return attributes[index];
        }

        /// <summary>
        /// Returns the attribute of given category.
        /// </summary>
        /// <param name="category">The category.</param>
        /// <returns>The attribute of given category or <code>null</code>, if category
        /// doesn't exist.</returns>
        public string GetAttributeForCategory(CallTreeCategory category)
        {
            if (category == null)
            {
                Debug.LogWarning("'category' is null!");
                return null;
            }
            for (int i = 0; i < attributes.Length; i++)
            {
                if (categories.At(i).Equals(category))
                {
                    return attributes[i];
                }
            }
            return null;
        }

        /// <summary>
        /// Returns the attribute of category by given name.
        /// </summary>
        /// <param name="category">The name of category.</param>
        /// <returns>The attribute of category by given name of <code>null</code>, if
        /// category doesn't exist.</returns>
        public string GetAttributeForCategory(string category)
        {
            if (category == null)
            {
                Debug.LogWarning("'category' is null!");
                return null;
            }
            for (int i = 0; i < attributes.Length; i++)
            {
                if (categories.At(i).Name.Equals(category))
                {
                    return attributes[i];
                }
            }
            return null;
        }

        /// <summary>
        /// Outputs the function call as a string in DYN-format.
        /// </summary>
        /// <returns>The function call as a string in DYN-format.</returns>
        public override string ToString()
        {
            string str = "";
            string delimiter = "";
            for (int i = 0; i < attributes.Length; i++)
            {
                str += delimiter + '\"' + attributes[i] + '\"';
                delimiter = " ";
            }
            return str;
        }
    }

    /// <summary>
    /// Represents a call tree.
    /// </summary>
    [Serializable]
    public class CallTree
    {
        /// <summary>
        /// Name of the linkage name attribute label.
        /// </summary>
        public const string LINKAGE_NAME = Node.LinknameAttribute;

        /// <summary>
        /// Name of the level attribute label.
        /// </summary>
        public const string LEVEL = "Level";

        /// <summary>
        /// The categories of the call tree.
        /// </summary>
        [SerializeField]
        private readonly CallTreeCategories categories;
        public CallTreeCategories Categories { get => categories; }

        /// <summary>
        /// The function calls of the call tree.
        /// </summary>
        [SerializeField]
        private readonly List<CallTreeFunctionCall> functionCalls;
        public List<CallTreeFunctionCall> FunctionCalls { get => functionCalls; }

        /// <summary>
        /// The count of the function calls.
        /// </summary>
        public int FunctionCallCount { get { return functionCalls.Count; } }

        /// <summary>
        /// Constructs a call tree with given categories. Categories must not be
        /// <code>null</code>.
        /// </summary>
        /// <param name="categories">The categories of the call tree.</param>
        public CallTree(CallTreeCategories categories)
        {
            this.categories = categories ?? throw new ArgumentException("'categories' must not be null!");
            functionCalls = new List<CallTreeFunctionCall>();
        }

        /// <summary>
        /// Returns the function call of given index.
        /// </summary>
        /// <param name="index">The index of the function call.</param>
        /// <returns>The function call at given index or <code>null</code>, if out of
        /// bounds.</returns>
        public CallTreeFunctionCall GetFunctionCall(int index)
        {
            if (index < 0 || index >= functionCalls.Count)
            {
                Debug.LogWarning("'index' is out of bounds!");
                return null;
            }
            return functionCalls[index];
        }

        /// <summary>
        /// Appends the given function call. Doesn't do anything if
        /// <paramref name="functionCall"/> is <code>null</code>.
        /// </summary>
        /// <param name="functionCall">The function call.</param>
        public void AddFunctionCall(CallTreeFunctionCall functionCall)
        {
            if (functionCall == null)
            {
                Debug.LogWarning("'functionCall' is null!");
                return;
            }
            if (functionCall.AttributeCount != categories.Count)
            {
                throw new IncorrectAttributeCountException(functionCall.AttributeCount, categories.Count);
            }
            functionCalls.Add(functionCall);
        }

        /// <summary>
        /// Initializes predecessor and successors of all function calls of the call
        /// tree to create the 'tree' aspect. Due to limitations of serialization, the
        /// tree-part of the <see cref="CallTree"/> must be initialized at runtime.
        /// </summary>
        public void GenerateTree()
        {
            Stack<KeyValuePair<int, CallTreeFunctionCall>> callStack = new Stack<KeyValuePair<int, CallTreeFunctionCall>>();
            for (int i = 0; i < functionCalls.Count; i++)
            {
                int key = int.Parse(functionCalls[i].GetAttributeForCategory(LEVEL));
                CallTreeFunctionCall value = functionCalls[i];
                KeyValuePair<int, CallTreeFunctionCall> pair = new KeyValuePair<int, CallTreeFunctionCall>(key, value);
                value.successors = new List<CallTreeFunctionCall>();
                while (callStack.Count != 0 && callStack.Peek().Key >= key)
                {
                    callStack.Pop();
                }
                if (callStack.Count != 0)
                {
                    callStack.Peek().Value.successors.Add(value);
                    value.predecessor = callStack.Peek().Value;
                }
                callStack.Push(pair);
            }
        }

        /// <summary>
        /// Maps every <see cref="GameObject"/> to the corresponding function call.
        /// GameObject will be saved in function call.
        /// </summary>
        /// <param name="gameObjects">List containing pairs of linkage name and corresponding
        /// actual GameObjects.</param>
        public void MapGameObjects(Dictionary<string, GameObject> gameObjects)
        {
            for (int i = 0; i < functionCalls.Count; i++)
            {
                CallTreeFunctionCall dynFunctionCall = functionCalls[i];
                string dynLinkageName = dynFunctionCall.GetAttributeForCategory(LINKAGE_NAME);
                FunctionInformation dynFunctionInformation = FunctionInformation.CreateFromDYNLinkageName(dynLinkageName);
                if (dynFunctionInformation == null)
                {
                    continue;
                }

                bool mapped = false;
                foreach (KeyValuePair<string, GameObject> go in gameObjects)
                {
                    string gxlLinkageName = go.Key;
                    FunctionInformation gxlFunctionInformation = FunctionInformation.CreateFromGXLLinkageName(gxlLinkageName);
                    if (gxlFunctionInformation == null)
                    {
                        continue;
                    }

                    if (dynFunctionInformation.Equals(gxlFunctionInformation))
                    {
                        dynFunctionCall.node = go.Value;
                        mapped = true;
                        break;
                    }
                }
                if (!mapped)
                {
                    Debug.LogWarning("Could not map '" + functionCalls[i] + "'!");
                }
            }
        }

        /// <summary>
        /// Outputs the call tree as a string in DYN-format.
        /// </summary>
        /// <returns>The call tree as a string in DYN-format.</returns>
        public override string ToString()
        {
            string str = categories.ToString() + '\n';
            for (int i = 0; i < functionCalls.Count; i++)
            {
                str += functionCalls[i].ToString() + '\n';
            }
            return str;
        }

        /// <summary>
        /// Is used for mapping <see cref="GameObject"/>-Objects onto
        /// <see cref="CallTreeFunctionCall"/>-Objects. Contains information about
        /// function calls, such as linkage name.
        /// </summary>
        private class FunctionInformation
        {
            /// <summary>
            /// Full linkage name of function.
            /// <example>
            /// namespace:file:foo
            /// </example>
            /// </summary>
            public string Name { get; private set; }

            /// <summary>
            /// Parameters of function call as strings.
            /// </summary>
            public string[] Parameters { get; private set; }

            /// <summary>
            /// Return value of function call as string.
            /// </summary>
            public string ReturnValue { get; private set; }

            // string representations of types.
            private const string CHAR = "char";
            private const string CONST = "";
            private const string INT = "int";
            private const string UNSIGNED_INT = "uint";
            private const string UNSIGNED = "u";
            private const string VOID = "";
            private const string VOID_RETURN = "v";
            private const char DELIMITER = ':';

            /// <summary>
            /// Constructs new function information with given name, parameters and
            /// return value. Arguments must not be <code>null</code>.
            /// </summary>
            /// <param name="name">The name of the function.</param>
            /// <param name="parameters">The parameters of the function as strings.
            /// </param>
            /// <param name="returnValue">The return value of the function.</param>
            private FunctionInformation(string name, string[] parameters, string returnValue)
            {
                if (name == null ||
                    parameters == null || !Array.TrueForAll(parameters, p => p != null) ||
                    returnValue == null)
                {
                    throw new ArgumentNullException("An argument is null!");
                }
                Name = name;
                Parameters = parameters;
                ReturnValue = returnValue;
            }

            /// <summary>
            /// Creates a new <see cref="FunctionInformation"/>-object from given linkage
            /// name as formatted in a GXL-file. <paramref name="linkageName"/> must not
            /// be <code>null</code>.
            /// </summary>
            /// <param name="linkageName">The linkage name as formatted in GXL-file.
            /// </param>
            /// <returns>Constructed <see cref="FunctionInformation"/> or
            /// <code>null</code>, if construction failed.</returns>
            public static FunctionInformation CreateFromGXLLinkageName(string linkageName)
            {
                if (linkageName == null)
                {
                    Debug.LogWarning("'linkageName' is null!");
                    return null;
                }

                string temp = linkageName.Substring(2);
                string[] tokens = null;

                string filename;
                string name;
                string[] parameters;
                string returnValue;

                // return value
                tokens = temp.Split(new string[] { "->" }, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length != 2)
                {
                    return null;
                }
                int index = temp.IndexOf(')');
                temp = index == -1 ? tokens[0] : tokens[0].Substring(0, index + 1);
                returnValue = ConvertFromGXLType(tokens[1]);

                // filename
                tokens = temp.Split(':');
                if (tokens.Length < 2)
                {
                    Debug.LogWarning("'linkageName' could not be split properly!");
                    return null;
                }
                temp = tokens[tokens.Length - 1];
                filename = tokens[0];

                // name (namespaces and class name)
                name = "";
                for (int i = 1; i < tokens.Length - 1; i++)
                {
                    name += tokens[i] + DELIMITER;
                }

                // name (function name)
                tokens = temp.Split(new char[] { '(', ',', ')' }, StringSplitOptions.RemoveEmptyEntries);
                name += tokens[0];

                // parameters
                parameters = new string[tokens.Length - 1];
                for (int i = 1; i < tokens.Length; i++)
                {
                    parameters[i - 1] = ConvertFromGXLType(tokens[i]);
                }

                return new FunctionInformation(name, parameters, returnValue);
            }

            /// <summary>
            /// Creates a new <see cref="FunctionInformation"/>-object from given linkage
            /// name as formatted in a DYN-file. <paramref name="linkageName"/> must not
            /// be <code>null</code>.
            /// </summary>
            /// <param name="linkageName">The linkage name as formatted in DYN-file.
            /// </param>
            /// <returns>Constructed <see cref="FunctionInformation"/> or
            /// <code>null</code>, if construction failed.</returns>
            public static FunctionInformation CreateFromDYNLinkageName(string linkageName)
            {
                if (linkageName == null)
                {
                    Debug.LogWarning("'linkageName' is null!");
                    return null;
                }

                string temp = linkageName;
                string[] tokens = null;

                string name;
                string[] parameters;
                string returnValue;

                // return value
                tokens = temp.Split(new char[] { ' ' });
                temp = "";
                returnValue = "";
                string delim = "";
                bool parsingReturnValue = true;
                for (int i = 0; i < tokens.Length; i++)
                {
                    if (tokens[i].Contains("("))
                    {
                        parsingReturnValue = false;
                        delim = "";
                    }
                    if (parsingReturnValue)
                    {
                        returnValue += delim + tokens[i];
                    }
                    else
                    {
                        temp += delim + tokens[i];
                    }
                    delim = " ";
                }
                returnValue = ConvertFromDYNType(returnValue);
                if (returnValue.Length == 0)
                {
                    returnValue = FunctionInformation.VOID_RETURN;
                }

                // name (namespaces and class name)
                tokens = temp.Split(new string[] { "::" }, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length < 1)
                {
                    Debug.LogWarning("'linkageName' could not be split properly!");
                    return null;
                }
                name = "";
                for (int i = 0; i < tokens.Length - 1; i++)
                {
                    name += tokens[i] + DELIMITER;
                }
                temp = tokens[tokens.Length - 1];

                // name (function name)
                tokens = temp.Split(new char[] { '(', ',', ')' }, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length < 2)
                {
                    Debug.LogWarning("'linkageName' could not be split properly!");
                    return null;
                }
                name += tokens[0];

                // parameters
                if (tokens.Length == 2 && tokens[1].Equals("void"))
                {
                    parameters = new string[0];
                }
                else
                {
                    parameters = new string[tokens.Length - 1];
                    for (int i = 1; i < tokens.Length; i++)
                    {
                        parameters[i - 1] = ConvertFromDYNType(tokens[i]);
                    }
                }

                return new FunctionInformation(name, parameters, returnValue);
            }

            /// <summary>
            /// Converts a type as formatted in a GXL-file to the internal type
            /// representation.
            /// </summary>
            /// <param name="type">The type as formatted in a GXL-file.</param>
            /// <returns>The converted type or <code>null</code>, if conversion failed.
            /// </returns>
            private static string ConvertFromGXLType(string type)
            {
                if (type == null)
                {
                    throw new ArgumentNullException("'type' must not be null!");
                }

                string temp = type;
                string addition = "";
                if (type.Contains("*") || type.Contains("&"))
                {
                    int ptrIndex = temp.IndexOf('*');
                    int refIndex = temp.IndexOf('&');
                    int index = ptrIndex;
                    if (refIndex >= 0 && refIndex < ptrIndex)
                    {
                        index = refIndex;
                    }
                    addition = temp.Substring(index);
                    temp = temp.Substring(0, index);
                }

                string convertedType;
                if (temp.Equals("c"))
                {
                    convertedType = CHAR;
                }
                else if (temp.Equals("i"))
                {
                    convertedType = INT;
                }
                else if (temp.Equals("ui"))
                {
                    convertedType = UNSIGNED_INT;
                }
                else if (temp.Equals("v"))
                {
                    convertedType = VOID_RETURN;
                }
                else
                {
                    throw new ArgumentException("'" + temp + "'could not be converted to any type!");
                }

                return convertedType + addition;
            }

            /// <summary>
            /// Converts a type as formatted in a DYN-file to the internal type
            /// representation.
            /// </summary>
            /// <param name="type">The type as formatted in a DYN-file.</param>
            /// <returns>The converted type or <code>null</code>, if conversion failed.
            /// </returns>
            private static string ConvertFromDYNType(string type)
            {
                if (type == null)
                {
                    throw new ArgumentNullException("'type' must not be null!");
                }

                string[] tokens = type.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                string convertedType = "";

                for (int i = 0; i < tokens.Length; i++)
                {
                    string token = tokens[i];
                    string convertedToken = "";

                    if (token.Equals("char"))
                    {
                        convertedToken = CHAR;
                    }
                    else if (token.Equals("const"))
                    {
                        convertedToken = CONST;
                    }
                    else if (token.Equals("int"))
                    {
                        convertedToken = INT;
                    }
                    else if (token.Equals("unsigned"))
                    {
                        convertedToken = UNSIGNED;
                    }
                    else if (token.Equals("void"))
                    {
                        convertedToken = VOID;
                    }
                    else if (token.Contains("*") || token.Contains("&"))
                    {
                        convertedToken = token;
                    }
                    else
                    {
                        throw new ArgumentException("'" + token + "'could not be converted to any type!");
                    }

                    convertedType += convertedToken;
                }

                return convertedType;
            }

            public override bool Equals(object obj)
            {
                if (this == obj)
                {
                    return true;
                }

                if (GetType().Equals(obj.GetType()))
                {
                    FunctionInformation fi = (FunctionInformation)obj;
                    bool nameEqual = Name.Equals(fi.Name);
                    bool parametersEqual = ArrayComparer.Equal(Parameters, fi.Parameters);
                    bool returnValuesEqual = ReturnValue.Equals(fi.ReturnValue);
                    return nameEqual && parametersEqual && returnValuesEqual;
                }
                return false;
            }

            public override int GetHashCode()
            {
                int hashCode = -1534923972;
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
                hashCode = hashCode * -1521134295 + EqualityComparer<string[]>.Default.GetHashCode(Parameters);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(ReturnValue);
                return hashCode;
            }
        }
    }
}
