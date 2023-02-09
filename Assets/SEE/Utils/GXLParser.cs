using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;

namespace SEE.Utils
{
    /// <summary>
    /// Class responsible for parsing GXL files.
    /// </summary>
    public class GXLParser : IDisposable
    {
        /// <summary>
        /// Creates a new GXL Parser for the given <paramref name="filename"/>.
        /// </summary>
        /// <param name="filename">Name of the GXL file which shall be parsed</param>
        /// <param name="logger">Logger to use for log messages</param>
        protected GXLParser(string filename, ILogger logger = null)
        {
            this.filename = filename;
            this.logger = logger;
            reader = new XmlTextReader(filename)
            {
                WhitespaceHandling = WhitespaceHandling.None
            };
        }

        /// <summary>
        /// State the parser can be in.
        /// </summary>
        protected enum State
        {
            undefined = 0,
            inGXL = 1,
            inGraph = 2,
            inNode = 3,
            inEdge = 4,
            inType = 5,
            inAttr = 6,
            inString = 7,
            inFloat = 8,
            inInt = 9,
            inEnum = 10 // toggle attributes are represented as <enum/> and nothing else
        }

        /// <summary>
        /// Converts the given <paramref name="state"/> to a string.
        /// </summary>
        /// <param name="state">State to convert to a string</param>
        /// <returns>Name of the given state</returns>
        protected static string ToString(State state)
        {
            return state switch
            {
                State.undefined => "undefined",
                State.inGXL => "gxl",
                State.inGraph => "graph",
                State.inNode => "node",
                State.inEdge => "edge",
                State.inType => "type",
                State.inAttr => "attr",
                State.inString => "string",
                State.inFloat => "float",
                State.inInt => "int",
                State.inEnum => "enum",
                _ => throw new NotImplementedException()
            };
        }

        /// <summary>
        /// Converts the given <paramref name="name"/> to a <see cref="State"/>.
        /// </summary>
        /// <param name="name">Name of the State.</param>
        /// <returns>State corresponding to given state, or <c>undefined</c> if name is unknown.</returns>
        protected static State ToState(string name)
        {
            return name switch
            {
                "gxl" => State.inGXL,
                "graph" => State.inGraph,
                "node" => State.inNode,
                "edge" => State.inEdge,
                "type" => State.inType,
                "attr" => State.inAttr,
                "string" => State.inString,
                "int" => State.inInt,
                "float" => State.inFloat,
                "enum" => State.inEnum,
                _ => State.undefined
            };
        }

        /// <summary>
        /// Stack storing the context of the parser.
        /// </summary>
        protected Stack<State> context = new();
        
        /// <summary>
        /// Name of the GXL file.
        /// </summary>
        protected string filename;
        
        /// <summary>
        /// Reader responsible for parsing XML.
        /// </summary>
        protected XmlReader reader;
        
        /// <summary>
        /// Logger responsible for log messages.
        /// </summary>
        protected ILogger logger;

        /// <summary>
        /// Exception indicating that a syntax error within the GXL file has occurred.
        /// </summary>
        [Serializable]
        public class SyntaxError : Exception
        {
            public SyntaxError()
            {
            }

            public SyntaxError(string message)
                : base(message)
            {
            }

            public SyntaxError(string message, Exception inner)
                : base(message, inner)
            {
            }
        }

        /// <summary>
        /// Logs the given <paramref name="message"/> as a debug message using <see cref="logger"/>.
        /// </summary>
        /// <param name="message">The message to log</param>
        protected virtual void LogDebug(string message)
        {
            logger?.LogDebug(message);
        }

        /// <summary>
        /// Logs the given <paramref name="message"/> as an error message using <see cref="logger"/>.
        /// </summary>
        /// <param name="message">The message to log</param>
        protected virtual void LogError(string message)
        {
            if (logger != null)
            {
                IXmlLineInfo xmlInfo = (IXmlLineInfo) reader;
                int lineNumber = xmlInfo.LineNumber - 1;
                logger.LogError($"{filename}:{lineNumber}: {message}\n");
            }
        }

        /// <summary>
        /// Ensures the actual closing tag corresponds to the expected closing tag.
        /// </summary>
        /// <exception cref="SyntaxError">If the opening and closing tags are mismatched</exception>
        private void EnsureExpectedEndTag()
        {
            State actual = ToState(reader.Name);
            State expected = context.Pop();

            if (actual != expected)
            {
                // TODO: Is logging this really necessary, as we throw an exception already?
                LogError($"syntax error: </{ToString(expected)}> expected. Actual: {reader.Name}");
                throw new SyntaxError($"Mismatched Tags: </{ToString(expected)}> expected. Actual: {reader.Name}");
            }
        }

        /// <summary>
        /// Loads the GXL file and parses it.
        /// </summary>
        public virtual void Load()
        {
            // Preserves the last text content of an XML node seen,
            // e.g., "mystring" in <string>mystring</string>.
            // Defined only at the EndElement, e.g. </string> here.
            string lastText = string.Empty;

            try
            {
                while (reader.Read())
                {
                    // LogDebug("XML processing: name=" + reader.Name + " nodetype=" + reader.NodeType + " value=" + reader.Value + "\n");

                    // See https://docs.microsoft.com/de-de/dotnet/api/system.xml.xmlnodetype?view=netframework-4.8
                    // for information on the XML reader.
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            // An element(for example, <item> ).
                            {
                                State state = ToState(reader.Name);
                                if (!reader.IsEmptyElement)
                                {
                                    // This is not a self-closing (empty) element, e.g., <item/>.
                                    // Note: A corresponding EndElement node is not generated for empty elements.
                                    // That is why we must push an expected EndElement onto the context stack
                                    // only if the element is not self-closing.
                                    context.Push(state);
                                }
                                switch (state)
                                {
                                    case State.undefined: StartUndefined(); break;
                                    case State.inGXL: StartGXL(); break;
                                    case State.inGraph: StartGraph(); break;
                                    case State.inNode: StartNode(); break;
                                    case State.inEdge: StartEdge(); break;
                                    case State.inType: StartType(); break;
                                    case State.inAttr: StartAttr(); break;
                                    case State.inString: lastText = string.Empty; StartString(); break;
                                    case State.inFloat: lastText = string.Empty; StartFloat(); break;
                                    case State.inInt: lastText = string.Empty; StartInt(); break;
                                    case State.inEnum: lastText = string.Empty; StartEnum(); break;
                                    default:
                                        throw new NotImplementedException();
                                }
                            }
                            break;
                        case XmlNodeType.Text:
                            // The text content of a node. (e.g., "this text" in <item>this text</item>
                            lastText = reader.Value;
                            break;
                        case XmlNodeType.EndElement:
                            // An end element tag (for example, </item> ).
                            EnsureExpectedEndTag();
                            switch (ToState(reader.Name))
                            {
                                case State.undefined: EndUndefined(); break;
                                case State.inGXL: EndGXL(); break;
                                case State.inGraph: EndGraph(); break;
                                case State.inNode: EndNode(); break;
                                case State.inEdge: EndEdge(); break;
                                case State.inType: EndType(); break;
                                case State.inAttr: EndAttr(); break;
                                case State.inString: EndString(lastText); break;
                                case State.inFloat:
                                    {
                                        if (lastText == string.Empty)
                                        {
                                            LogError("Float value is expected here.");
                                            throw new SyntaxError("Missing float value.");
                                        }
                                        try
                                        {
                                            float value = float.Parse(lastText, CultureInfo.InvariantCulture.NumberFormat);
                                            EndFloat(value);
                                        }
                                        catch (FormatException e)
                                        {
                                            LogError($"{lastText} is no float value.");
                                            throw new SyntaxError($"'{lastText}' is no float value: {e}");
                                        }
                                    }
                                    break;
                                case State.inInt:
                                    {
                                        if (lastText == string.Empty)
                                        {
                                            LogError("Int value is expected here.");
                                            throw new SyntaxError("Missing int value.");
                                        }
                                        try
                                        {
                                            int value = int.Parse(lastText, CultureInfo.InvariantCulture.NumberFormat);
                                            EndInt(value);
                                        }
                                        catch (FormatException e)
                                        {
                                            LogError($"{lastText} is no int value.");
                                            throw new SyntaxError($"'{lastText}' is no int value: {e}");
                                        }
                                    }
                                    break;
                                case State.inEnum: EndEnum(); break;
                                default:
                                    throw new NotImplementedException();
                            }
                            break;
                        case XmlNodeType.None:
                            // This is returned by the XmlReader if a Read method has not been called.
                            break;
                        case XmlNodeType.Attribute:
                            // An attribute (for example, id='123').
                            break;
                        case XmlNodeType.CDATA:
                            // A CDATA section (for example, <![CDATA[my escaped text]]>).
                            break;
                        case XmlNodeType.EntityReference:
                            // A reference to an entity (for example, &num;).
                            break;
                        case XmlNodeType.Entity:
                            // An entity declaration (for example, <!ENTITY...> ).
                            break;
                        case XmlNodeType.ProcessingInstruction:
                            // A processing instruction (for example, <?pi test?>).
                            break;
                        case XmlNodeType.Comment:
                            //  A comment (for example, <!--my comment--> ).
                            break;
                        case XmlNodeType.Document:
                            // A document object that, as the root of the document tree, provides access to the entire XML document.
                            break;
                        case XmlNodeType.DocumentType:
                            // The document type declaration, indicated by the following tag (for example, <!DOCTYPE...>).
                            break;
                        case XmlNodeType.DocumentFragment:
                            // A document fragment.
                            break;
                        case XmlNodeType.Notation:
                            // A notation in the document type declaration (for example, <!NOTATION...>).
                            break;
                        case XmlNodeType.Whitespace:
                            // White space between markup.
                            break;
                        case XmlNodeType.SignificantWhitespace:
                            // White space between markup in a mixed content model or white space within the xml:space = "preserve" scope.
                            break;
                        case XmlNodeType.EndEntity:
                            // Returned when XmlReader gets to the end of the entity replacement as a result of a call to ResolveEntity().
                            break;
                        case XmlNodeType.XmlDeclaration:
                            // The XML declaration (for example, <?xml version='1.0'?>).
                            break;
                        default:
                            LogDebug("unparsed");
                            break;
                    }
                }
            }
            finally
            {
                reader.Close();
            }
            if (context.Count > 0)
            {
                LogError($"XML parser is still expecting input in state {context.Peek()}");
                throw new SyntaxError($"missing closing {ToString(context.Peek())} tag");
            }
        }

        protected virtual void StartEnum()
        {
        }

        protected virtual void StartInt()
        {
        }

        protected virtual void StartFloat()
        {
        }

        protected virtual void StartString()
        {
        }

        protected virtual void StartAttr()
        {
        }

        protected virtual void StartEdge()
        {
        }

        protected virtual void StartType()
        {
        }

        protected virtual void StartNode()
        {
        }

        protected virtual void StartGraph()
        {
        }

        protected virtual void StartUndefined()
        {
        }

        protected virtual void StartGXL()
        {
        }

        protected virtual void EndEnum()
        {
        }

        protected virtual void EndInt(int value)
        {
        }

        protected virtual void EndFloat(float value)
        {
        }

        protected virtual void EndString(string value)
        {
        }

        protected virtual void EndAttr()
        {
        }

        protected virtual void EndEdge()
        {
        }

        protected virtual void EndType()
        {
        }

        protected virtual void EndNode()
        {
        }

        protected virtual void EndGraph()
        {
        }

        protected virtual void EndUndefined()
        {
        }

        protected virtual void EndGXL()
        {
        }

        #region IDisposable Support
        private bool disposedValue; // To detect redundant calls of Dispose(bool).

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // dispose managed resources
                    if (reader != null)
                    {
                        reader.Dispose();
                        reader = null;
                    }
                }

                // release unmanaged resources (required to override the Finalizer below)
                // there are none here

                // set larger attributes to null
                context = null;
                logger = null;
                filename = null;

                // this object is now considered disposed
                disposedValue = true;
            }
        }

        // Override the Finalizer only if Dispose(bool) contains code for releasing
        // unmanaged resources
        //~GXLParser()
        //{
        //    // Do not change this code. Add clean up code to Dispose(bool disposing) above.
        //    Dispose(false);
        //}

        // Required to implement the Dispose pattern correctly.
        public void Dispose()
        {
            // All code for releasing resources should be added to Dispose(bool).
            Dispose(true);
            // Comment out this code when the Finalizer ~GXLParser() is overridden.
            //GC.SuppressFinalize(this);
        }
        #endregion
    }
}