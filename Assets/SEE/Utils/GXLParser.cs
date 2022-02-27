using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;

namespace SEE.Utils
{
    public class GXLParser : IDisposable
    {
        public GXLParser(string filename, SEE.Utils.ILogger logger = null)
        {
            this.filename = filename;
            this.logger = logger;
            reader = new XmlTextReader(filename)
            {
                WhitespaceHandling = WhitespaceHandling.None
            };
        }

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

        protected static string ToString(State state)
        {
            switch (state)
            {
                case State.undefined: return "undefined";
                case State.inGXL: return "gxl";
                case State.inGraph: return "graph";
                case State.inNode: return "node";
                case State.inEdge: return "edge";
                case State.inType: return "type";
                case State.inAttr: return "attr";
                case State.inString: return "string";
                case State.inFloat: return "float";
                case State.inInt: return "int";
                case State.inEnum: return "enum";
                default: throw new NotImplementedException();
            }
        }

        protected static State ToState(string name)
        {
            if (name == "gxl")
            {
                return State.inGXL;
            }
            else if (name == "graph")
            {
                return State.inGraph;
            }
            else if (name == "node")
            {
                return State.inNode;
            }
            else if (name == "edge")
            {
                return State.inEdge;
            }
            else if (name == "type")
            {
                return State.inType;
            }
            else if (name == "attr")
            {
                return State.inAttr;
            }
            else if (name == "string")
            {
                return State.inString;
            }
            else if (name == "int")
            {
                return State.inInt;
            }
            else if (name == "float")
            {
                return State.inFloat;
            }
            else if (name == "enum")
            {
                return State.inEnum;
            }
            else
            {
                return State.undefined;
            }
        }

        protected Stack<State> context = new Stack<State>();
        protected string filename;
        protected XmlReader reader;
        protected SEE.Utils.ILogger logger = null;

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

        protected virtual void LogDebug(string message)
        {
            if (logger != null)
            {
                logger.LogDebug(message);
            }
        }

        protected virtual void LogError(string message)
        {
            if (logger != null)
            {
                IXmlLineInfo xmlInfo = (IXmlLineInfo) reader;
                int lineNumber = xmlInfo.LineNumber - 1;
                logger.LogError(filename + ":" + lineNumber + ": " + message + "\n");
            }
        }

        private void Expected()
        {
            State actual = ToState(reader.Name);
            State expected = context.Pop();

            if (actual != expected)
            {
                LogError("syntax error: <\\" + ToString(expected) + "> expected. Actual: " + reader.Name);
                throw new SyntaxError("mismatched tags");
            }
        }

        public virtual void Load()
        {
            // Preserves the last text content of an XML node seen,
            // e.g., "mystring" in <string>mystring</string>.
            // Defined only at the EndElement, e.g. </string> here.
            string lastText = "";

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
                                    case State.inString: lastText = ""; StartString(); break;
                                    case State.inFloat: lastText = ""; StartFloat(); break;
                                    case State.inInt: lastText = ""; StartInt(); break;
                                    case State.inEnum: lastText = ""; StartEnum(); break;
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
                            Expected();
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
                                        if (lastText == "")
                                        {
                                            LogError("Float value is expected here.");
                                            throw new SyntaxError("Missing float value.");
                                        }
                                        try
                                        {
                                            float value = float.Parse(lastText, CultureInfo.InvariantCulture.NumberFormat);
                                            EndFloat(value);
                                        }
                                        catch (Exception e)
                                        {
                                            LogError(lastText + " is no float value.");
                                            throw new SyntaxError("'" + lastText + "' is no float value: " + e.ToString());
                                        }
                                    }
                                    break;
                                case State.inInt:
                                    {
                                        if (lastText == "")
                                        {
                                            LogError("Int value is expected here.");
                                            throw new SyntaxError("Missing int value.");
                                        }
                                        try
                                        {
                                            int value = int.Parse(lastText, CultureInfo.InvariantCulture.NumberFormat);
                                            EndInt(value);
                                        }
                                        catch (Exception e)
                                        {
                                            LogError(lastText + " is no int value.");
                                            throw new SyntaxError("'" + lastText + "' is no int value: " + e.ToString());
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
            catch (Exception e)
            {
                LogError($"Problem in parsing {filename}: {e.Message}");
            }
            finally
            {
                reader.Close();
            }
            if (context.Count > 0)
            {
                LogError("XML parser is still expecting input in state " + context.Peek());
                throw new SyntaxError("missing closing " + ToString(context.Peek()) + " tag");
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
        private bool disposedValue = false; // To detect redundant calls of Dispose(bool).

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

                // release unmanaged ressources (requires to override the Finalizer below)
                // there are none here

                // set larger attributes to null
                context = null;
                logger = null;
                filename = null;

                // this object is now considerd disposed
                disposedValue = true;
            }
        }

        // Override the Finalizer only if Dispose(bool) contains code for releasing
        // unmanaged resources
        //~GXLParser()
        //{
        //    // Ändern Sie diesen Code nicht. Fügen Sie Bereinigungscode in Dispose(bool disposing) weiter oben ein.
        //    Dispose(false);
        //}

        // Required to implement the Dispose pattern correctly.
        public void Dispose()
        {
            // All code for releasing ressources should be added to Dispose(bool).
            Dispose(true);
            // Comment out this code when the Finalizer ~GXLParser() is overridden.
            //GC.SuppressFinalize(this);
        }
        #endregion
    }
}