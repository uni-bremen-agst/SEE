/*
 * @(#)GXLDocument.java	0.92 2004-04-22
 *
 * Copyright (C) 2003 Erik Larsson
 *
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 *
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
 *
 */

package net.sourceforge.gxl;

import java.io.File;
import java.io.InputStream;
import java.io.OutputStream;
import java.io.FileInputStream;
import java.io.FileOutputStream;
import java.io.ByteArrayInputStream;
import java.io.IOException;
import java.net.URL;
import java.util.Map;
import java.util.Hashtable;
import java.util.Iterator;
import java.util.Vector;
import javax.swing.undo.UndoableEditSupport;
import javax.xml.parsers.DocumentBuilder;
import javax.xml.parsers.DocumentBuilderFactory;
import javax.xml.parsers.ParserConfigurationException;
import javax.xml.transform.Result;
import javax.xml.transform.Source;
import javax.xml.transform.Transformer;
import javax.xml.transform.TransformerException;
import javax.xml.transform.TransformerConfigurationException;
import javax.xml.transform.TransformerFactory;
import javax.xml.transform.dom.DOMSource;
import javax.xml.transform.stream.StreamSource;
import javax.xml.transform.stream.StreamResult;
import org.w3c.dom.Document;
import org.w3c.dom.DocumentType;
import org.w3c.dom.Element;
import org.w3c.dom.Node;
import org.w3c.dom.NodeList;
import org.w3c.dom.NamedNodeMap;
import org.w3c.dom.Attr;
import org.w3c.dom.Text;
import org.w3c.dom.DOMException;
import org.xml.sax.SAXException;

/**	Represents a GXL document responsible for reading and writing GXL files.
 *	<p>
 *	The top <code>gxl</code> element is automatically created and cannot be removed. To add
 *	other elements to this document, add them to the <code>gxl</code> element which is accessible through the
 *	<code>getDocumentElement</code> method or any of it's children.
 *	<p>
 *	To create a simple gxl document and write it to file, follow these basic steps:<br>
 *	<p><code>
 *	// Create document and elements<br>
 *	GXLDocument gxlDocument = new GXLDocument();<br>
 *	GXLGraph graph = new GXLGraph("graph1");<br>
 *	GXLNode node1 = new GXLNode("node1");<br>
 *	GXLNode node2 = new GXLNode("node2");<br>
 *	GXLEdge edge1 = new GXLEdge(node1, node2);<br>
 *	// Build the tree structure<br>
 *	graph.add(node1);<br>
 *	graph.add(node2);<br>
 *	graph.add(edge1);<br>
 *	gxlDocument.getDocumentElement().add(graph);<br>
 *	// Write the document to file<br>
 *	try {<br>
 *	&nbsp;gxlDocument.write(new File("MyFirstGXLDocument.gxl"));<br>
 *	}<br>
 *	catch (IOException ioe) {<br>
 *	&nbsp;System.out.println("Error while writing to file: " + ioe);<br>
 *	}<br>
 *	</code>
 *	<p>
 *	<b>Validation</b><br>
 *	The GXL structure is concurrently being validated as the document and elements are being built
 *	and will throw a <code>GXLValidationException</code> if an operation is attempted that would
 *	render the GXL structure invalid. The exception of this is the permission to temporarily create
 *	dangling tentacles as described below. The same validation mechanism is used when reading a document
 *	so as to provide consistent exceptions (apart from invalid XML documents which will be thrown
 *	by the XML parser). For a full description of which types of <code>GXLValidationsException</code>s
 *	that can be thrown and when, see {@link GXLValidationException}.
 *	<p>
 *	<b>Undo Support</b><br>
 *	<code>GXLDocument</code> extends <code>UndoableEditSupport</code> and thusly provides undo support.
 *	This is however only true of changes made that the document is aware of, namely to elements that is
 *	already part of the documents tree structure. In the example above, only the last tree change - adding
 *	the graph to the top <code>gxl</code> element - is undoable.
 *	<p>
 *	<b>Dangling Tentacles</b><br>
 *	To enhance usability the introduction of dangling tentacles is allowed. A dangling tentacle will occur
 *	when an edge or a relation is inserted into a <code>GXLDocument</code> and one of it's tentacles point
 *	to a non-existent node. This is a validation error in GXL and the <code>GXLDocument</code> will throw
 *	if any of the <code>write</code> methods is called. The rationale behind allowing it is because GXL
 *	doesn't place any restricions on the order of appearance of nodes and edges in the file, permitting
 *	an edge to be placed before the referenced node. Besides if it weren't allowed, the example above
 *	would throw an exception if the line adding the edge would be placed above the lines adding the nodes.
 *	In particular any edge or relation that is not part of a <code>GXLDocument</code> will always have
 *	dangling tentacles since the <code>GXLDocument</code> is responsible for matching id references to
 *	specific <code>GXLElement</code>s.
 *	<p>
 *	For more information about the GXL file format and it's uses, see <a href="http://www.gupro.de/GXL">http://www.gupro.de/GXL</a>
 *
 *	@see GXLValidationException
 *	@see UndoableEditSupport
 */
public class GXLDocument extends UndoableEditSupport {
	// Static XML helpers
	private static DocumentBuilder documentBuilder;
	private static TransformerFactory transformerFactory;
	// An id to GXLElement map
	Map idMap = new Hashtable();
	// A list of all dangling tentacles in this document
	Vector danglingTentacles = new Vector();
	// The top "gxl" element
	GXLGXL gxlElement;
	// The systemID read from the gxl file, or null if no systemID was specified (to preserve it)
	String systemID;
	// A list of listeners to this document
	Vector gxlDocumentListeners = new Vector();

	//
	// CONSTRUCTORS
	//

	/** Creates a blank GXLDocument, only containing the top <code>gxl</code> element. */
	public GXLDocument() {
		systemID = GXL.SYSTEM_ID;
		// Create the top gxl element
		gxlElement = new GXLGXL(this);
	}

	/** Parses the specified file and builds the GXL document structure out of it. 
	 *	@param gxlFile The file containing the GXL to parse.
	 *	@throws IOException If the file doesn't exist or cannot be read.
	 *	@throws SAXException If the file doesn't contain a valid XML document.
	 *	@throws GXLValidationException If the file doesn't contain a valid GXL document.
	 */
	public GXLDocument(File gxlFile) throws IOException, SAXException {
		this(new FileInputStream(gxlFile));
	}

	/** Parses the resource specified by the url and builds the GXL document structure out of it. 
	 *	@param url A URL pointing to a GXL resource.
	 *	@throws IOException If the resource doesn't exist or cannot be read.
	 *	@throws SAXException If the resource doesn't contain a valid XML document.
	 *	@throws GXLValidationException If the resource doesn't contain a valid GXL document.
	 */
	public GXLDocument(URL url) throws IOException, SAXException {
		this(url.openStream());
	}

	/** Parses the specified <code>InputStream</code> and builds the GXL document structure out of it. 
	 *	@param is An <code>InputStream</code> containing a GXL document.
	 *	@throws IOException If the resource doesn't exist or cannot be read.
	 *	@throws SAXException If the resource doesn't contain a valid XML document.
	 *	@throws GXLValidationException If the resource doesn't contain a valid GXL document.
	 */
	public GXLDocument(InputStream is) throws IOException, SAXException {
		try {
			// If this is the first GXLDocument, create the documentBuilder
			if (documentBuilder == null) {
				// Create a DocumentBuilderFactory
				DocumentBuilderFactory dbf = DocumentBuilderFactory.newInstance();
				// Create a DocumentBuilder
				documentBuilder = dbf.newDocumentBuilder();
				documentBuilder.setEntityResolver(new GXLEntityResolver()); //AII, 04/07/04
			}

			// Parse the document
			Document document = documentBuilder.parse(is);
			DocumentType documentType = document.getDoctype();
			if (documentType != null)
				systemID = documentType.getSystemId();

			// Every gxl element reads what he wants from the org.w3c.dom.Element
			// ie this call creates all elements recursively
			gxlElement = new GXLGXL(this, (Element) document.getDocumentElement());
		}
		catch (ParserConfigurationException pce) {
			throw new Error("ParserConfigurationException thrown (without any configuration being done?!?)", pce);
		}
	}

	//
	// PUBLIC METHODS
	//

	/** Adds the specified document listener to receive document modification events. 
	 *	@param l The listener to register for event notification.
	 */
	public void addGXLDocumentListener(GXLDocumentListener l) {
		gxlDocumentListeners.add(l);
	}

	/** Removes the specified document listener so that it no longer receives document modification events. 
	 *	@param l The listener to unregister for event notification.
	 */
	public void removeGXLDocumentListener(GXLDocumentListener l) {
		gxlDocumentListeners.remove(l);
	}

	/** Returns the number of dangling tentacles contained in this document.
	 *	A tentacle is considered dangling if it's target (specified by an id) is not
	 *	contained in this document.
	 *	@return The number of dangling tentacles contained in this document.
	 *	@see #getDanglingTentacleAt(int)
	 *	@see GXLLocalConnection#containsDanglingTentacles()
	 */
	public int getDanglingTentacleCount() {
		return danglingTentacles.size();
	}

	/** Returns the specified dangling tentacle.
	 *	A tentacle is considered dangling if it's target (specified by an id) is not
	 *	contained in this document.
	 *	@param i The index of the specified tentacle.
	 *	@return The specified tentacle.
	 *	@see #getDanglingTentacleCount()
	 *	@see GXLLocalConnection#containsDanglingTentacles()
	 */
	public GXLLocalConnectionTentacle getDanglingTentacleAt(int i) {
		return (GXLLocalConnectionTentacle) danglingTentacles.elementAt(i);
	}

	/** Returns the top <code>gxl</code> element. 
	 *	This element is automatically created when a <code>GXLDocument</code> is created and
	 *	always wraps all other elements in a GXL document.
	 *	@return The top <code>gxl</code> element. 
	 */
	public GXLGXL getDocumentElement() {
		return gxlElement;
	}
	
	/** Whether the specified id is used by any of the elements contained in this document. 
	 *	@param id The specified id to search for.
	 *	@return True iff an element is contained in this document that has the specified id set.
	 */
	public boolean containsID(String id) {
		return idMap.containsKey(id);
	}

	/** Returns the element with the specified id, or null if no element contained in this document
	 *	has the specified id set. 
	 *	@param id The specified id to search for.
	 *	@return The element with the specified id, or null if no element contained in this document
	 *	has the specified id set. 
	 */
	public GXLAttributedElement getElement(String id) {
		return (GXLAttributedElement) idMap.get(id);
	}

	/** Writes this document to file. Note that writing a document containing dangling tentacles
	 *	to file is prohibited since this would create an invalid GXL document.
	 *	@param file The file to write this document to.
	 *	@throws IOException If the file couldn't be written to.
	 *	@throws IllegalArgumentException If this document contains dangling tentacles.
	 */
	public void write(File file) throws IOException {
		write(new FileOutputStream(file));
	}

	/** Writes this document to an <code>OutputStream</code>. Note that writing a document containing dangling tentacles
	 *	to file is prohibited since this would create an invalid GXL document.
	 *	@param os The stream to write this document to.
	 *	@throws IOException If the stream couldn't be written to.
	 *	@throws IllegalArgumentException If this document contains dangling tentacles.
	 */
	public void write(OutputStream os) throws IOException {
		if (getDanglingTentacleCount() > 0)
			throw new IllegalArgumentException("Writing a GXL document containing dangling tentacles is not permitted (produces invalid gxl)");
		try {
			// If this is the first time we write to file, create the documentBuilder and/or transformerFactory
			if (documentBuilder == null) {
				// Create a DocumentBuilderFactory
				DocumentBuilderFactory dbf = DocumentBuilderFactory.newInstance();
				// Create a DocumentBuilder
				documentBuilder = dbf.newDocumentBuilder();
				documentBuilder.setEntityResolver(new GXLEntityResolver()); //AII, 04/07/04
			}
			if (transformerFactory == null) {
				// Create a TransformerFactory
				transformerFactory = TransformerFactory.newInstance();
			}

			// Build the transformer (insert DocType if warranted)
			String prettyPrintXSLT = prettyPrintXSLTStart;
			if (systemID != null)
				prettyPrintXSLT += " doctype-system=\"" + systemID + "\"";
			prettyPrintXSLT += prettyPrintXSLTEnd;
			Transformer transformer = transformerFactory.newTransformer(new StreamSource(new ByteArrayInputStream(prettyPrintXSLT.getBytes())));

			// Build the DOM document
			Document document = documentBuilder.newDocument();
			gxlElement.buildDOM(document, null);

			// Write the document to file
			DOMSource source = new DOMSource(document);
			Result result = new StreamResult(os);
			transformer.transform(source, result);
			os.close();
		}
		catch (ParserConfigurationException pce) {
			throw new Error("ParserConfigurationException thrown (without any configuration being done?!?)", pce);
		}
		catch (TransformerConfigurationException tce) {
			throw new Error("TransformerConfigurationException thrown (without any configuration being done?!?)", tce);
		}
		catch (TransformerException te) {
			throw new Error("TransformerException thrown (internal error somewhere?)", te);
		}
	}

	//
	// PRIVATE METHODS
	//

	/** Indents the XML output. Optional DocType goes between Start and End. */
	private static final String prettyPrintXSLTStart = 
		"<xsl:stylesheet version=\"1.0\" xmlns:xsl=\"http://www.w3.org/1999/XSL/Transform\">" +
		"	<xsl:output method=\"xml\"";
	private static final String prettyPrintXSLTEnd = 
		"/>" + 
		"	<xsl:param name=\"indent-increment\" select=\"'&#x9;'\" />" + 
		"	<xsl:template match=\"*\">" + 
		"		<xsl:param name=\"indent\" select=\"'&#xA;'\"/>" + 
		"		<xsl:value-of select=\"$indent\"/>" + 
		"		<xsl:copy>" + 
		"			<xsl:copy-of select=\"@*\" />" + 
		"			<xsl:apply-templates>" + 
		"				<xsl:with-param name=\"indent\" select=\"concat($indent, $indent-increment)\"/>" + 
		"			</xsl:apply-templates>" + 
		"			<xsl:if test=\"*\">" + 
		"				<xsl:value-of select=\"$indent\"/>" + 
		"			</xsl:if>" + 
		"		</xsl:copy>" + 
		"	</xsl:template>" + 
		"	<xsl:template match=\"comment()|processing-instruction()\">" + 
		"		<xsl:copy />" + 
		"	</xsl:template>" + 
		"	<!-- WARNING: this is dangerous. Handle with care -->" + 
		"	<xsl:template match=\"text()[normalize-space(.)='']\"/>" + 
		"</xsl:stylesheet>";
}
