using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Xml;
using Cysharp.Threading.Tasks;
using SEE.Utils;

namespace SEE.DataModel.DG.IO
{
    /// <summary>
    /// Class responsible for parsing GXL files.
    /// </summary>
    public class GXLParser : GraphIO, IDisposable
    {
        /// <summary>
        /// Creates a new GXL Parser.
        /// </summary>
        /// <param name="logger">Logger to use for log messages.</param>
        protected GXLParser(ILogger logger = null)
        {
            Logger = logger;
        }

        /// <summary>
        /// State the parser can be in.
        /// </summary>
        protected enum State
        {
            Undefined = 0,
            InGXL = 1,
            InGraph = 2,
            InNode = 3,
            InEdge = 4,
            InType = 5,
            InAttr = 6,
            InString = 7,
            InFloat = 8,
            InInt = 9,
            InEnum = 10 // toggle attributes are represented as <enum/> and nothing else
        }

        /// <summary>
        /// Converts the given <paramref name="state"/> to a string.
        /// </summary>
        /// <param name="state">State to convert to a string.</param>
        /// <returns>Name of the given state.</returns>
        protected static string ToString(State state)
        {
            return state switch
            {
                State.Undefined => "undefined",
                State.InGXL => "gxl",
                State.InGraph => "graph",
                State.InNode => "node",
                State.InEdge => "edge",
                State.InType => "type",
                State.InAttr => "attr",
                State.InString => "string",
                State.InFloat => "float",
                State.InInt => "int",
                State.InEnum => "enum",
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
                "gxl" => State.InGXL,
                "graph" => State.InGraph,
                "node" => State.InNode,
                "edge" => State.InEdge,
                "type" => State.InType,
                "attr" => State.InAttr,
                "string" => State.InString,
                "int" => State.InInt,
                "float" => State.InFloat,
                "enum" => State.InEnum,
                _ => State.Undefined
            };
        }

        /// <summary>
        /// Stack storing the context of the parser.
        /// </summary>
        protected Stack<State> Context = new();

        /// <summary>
        /// Name of the GXL file.
        /// </summary>
        protected string Name;

        /// <summary>
        /// Reader responsible for parsing XML.
        /// </summary>
        protected XmlReader Reader;

        /// <summary>
        /// Logger responsible for log messages.
        /// </summary>
        protected ILogger Logger;

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
        /// Logs the given <paramref name="message"/> as a debug message using <see cref="Logger"/>.
        /// </summary>
        /// <param name="message">The message to log.</param>
        protected virtual void LogDebug(string message)
        {
            Logger?.LogDebug(message);
        }

        /// <summary>
        /// Logs the given <paramref name="message"/> as an error message using <see cref="Logger"/>.
        /// </summary>
        /// <param name="message">The message to log.</param>
        protected virtual void LogError(string message)
        {
            if (Logger != null)
            {
                IXmlLineInfo xmlInfo = (IXmlLineInfo)Reader;
                int lineNumber = xmlInfo.LineNumber - 1;
                Logger.LogError($"{Name}:{lineNumber}: {message}\n");
            }
        }

        /// <summary>
        /// Ensures the actual closing tag corresponds to the expected closing tag.
        /// </summary>
        /// <exception cref="SyntaxError">If the opening and closing tags are mismatched.</exception>
        private void EnsureExpectedEndTag()
        {
            State actual = ToState(Reader.Name);
            State expected = Context.Pop();

            if (actual != expected)
            {
                // TODO: Is logging this really necessary, as we throw an exception already?
                LogError($"syntax error: </{ToString(expected)}> expected. Actual: {Reader.Name}");
                throw new SyntaxError($"Mismatched Tags: </{ToString(expected)}> expected. Actual: {Reader.Name}");
            }
        }

        /// <summary>
        /// Processes the GXL data provided in the <paramref name="gxl"/> stream.
        /// </summary>
        /// <param name="gxl">Stream containing GXL data that shall be processed.</param>
        /// <param name="name">Name of the GXL data stream. Only used for display purposes in log messages.</param>
        /// <param name="changePercentage">To report progress.</param>
        /// <param name="token">Token with which the loading can be cancelled.</param>
        public virtual async UniTask LoadAsync(Stream gxl, string name,
                                               Action<float> changePercentage = null,
                                               CancellationToken token = default)
        {
            Name = name;

            XmlReaderSettings settings = new()
            {
                CloseInput = true,
                IgnoreWhitespace = true,
                IgnoreComments = true,
                DtdProcessing = DtdProcessing.Ignore,
                Async = true
            };
            Reader = XmlReader.Create(gxl, settings);

            // Preserves the last text content of an XML node seen,
            // e.g., "mystring" in <string>mystring</string>.
            // Defined only at the EndElement, e.g. </string> here.
            string lastText = string.Empty;

            bool firstEdgeRead = false;

            try
            {
                await UniTask.SwitchToThreadPool();
                while (await Reader.ReadAsync())
                {
                    token.ThrowIfCancellationRequested();

                    // LogDebug("XML processing: name=" + reader.Name + " nodetype=" + reader.NodeType + " value=" + reader.Value + "\n");

                    // See https://docs.microsoft.com/de-de/dotnet/api/system.xml.xmlnodetype?view=netframework-4.8
                    // for information on the XML reader.
                    switch (Reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            // An element(for example, <item> ).
                        {
                            State state = ToState(Reader.Name);
                            if (!Reader.IsEmptyElement)
                            {
                                // This is not a self-closing (empty) element, e.g., <item/>.
                                // Note: A corresponding EndElement node is not generated for empty elements.
                                // That is why we must push an expected EndElement onto the context stack
                                // only if the element is not self-closing.
                                Context.Push(state);
                            }

                            switch (state)
                            {
                                case State.Undefined:
                                    StartUndefined();
                                    break;
                                case State.InGXL:
                                    StartGXL();
                                    break;
                                case State.InGraph:
                                    StartGraph();
                                    break;
                                case State.InNode:
                                    StartNode();
                                    break;
                                case State.InEdge:
                                    if (!firstEdgeRead)
                                    {
                                       firstEdgeRead = true;
                                       changePercentage?.Invoke(0.5f);
                                    }
                                    StartEdge();
                                    break;
                                case State.InType:
                                    StartType();
                                    break;
                                case State.InAttr:
                                    StartAttr();
                                    break;
                                case State.InString:
                                    lastText = string.Empty;
                                    StartString();
                                    break;
                                case State.InFloat:
                                    lastText = string.Empty;
                                    StartFloat();
                                    break;
                                case State.InInt:
                                    lastText = string.Empty;
                                    StartInt();
                                    break;
                                case State.InEnum:
                                    lastText = string.Empty;
                                    StartEnum();
                                    break;
                                default:
                                    throw new NotImplementedException();
                            }
                        }
                            break;
                        case XmlNodeType.Text:
                            // The text content of a node. (e.g., "this text" in <item>this text</item>
                            lastText = Reader.Value;
                            break;
                        case XmlNodeType.EndElement:
                            // An end element tag (for example, </item> ).
                            EnsureExpectedEndTag();
                            switch (ToState(Reader.Name))
                            {
                                case State.Undefined:
                                    EndUndefined();
                                    break;
                                case State.InGXL:
                                    EndGXL();
                                    break;
                                case State.InGraph:
                                    EndGraph();
                                    break;
                                case State.InNode:
                                    EndNode();
                                    break;
                                case State.InEdge:
                                    EndEdge();
                                    break;
                                case State.InType:
                                    EndType();
                                    break;
                                case State.InAttr:
                                    EndAttr();
                                    break;
                                case State.InString:
                                    EndString(lastText);
                                    break;
                                case State.InFloat:
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
                                case State.InInt:
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
                                case State.InEnum:
                                    EndEnum();
                                    break;
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
                Reader.Close();
                await UniTask.SwitchToMainThread();
                changePercentage?.Invoke(1.0f);
            }

            if (Context.Count > 0)
            {
                LogError($"XML parser is still expecting input in state {Context.Peek()}");
                throw new SyntaxError($"missing closing {ToString(Context.Peek())} tag");
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
                    if (Reader != null)
                    {
                        Reader.Dispose();
                        Reader = null;
                    }
                }

                // release unmanaged resources (required to override the Finalizer below)
                // there are none here

                // set larger attributes to null
                Context = null;
                Logger = null;
                Name = null;

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
