/*
 * @(#)GXLElement.java	0.9 2003-12-08
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

import java.util.Hashtable;
import java.util.Vector;
import java.util.Iterator;
import javax.swing.undo.StateEditable;
import javax.swing.undo.StateEdit;
import org.w3c.dom.Attr;
import org.w3c.dom.Document;
import org.w3c.dom.Element;
import org.w3c.dom.NamedNodeMap;
import org.w3c.dom.Node;
import org.w3c.dom.NodeList;

/** Superclass of all GXL elements. 
 *	<p>
 *	Provides the basic functionality of all the elements, that is <br>
 *	(1) attribute modification,<br>
 *	(2) insertion of new elements and<br>
 *	(3) removal of elements. <br>
 *	The only other kind of modification that exist is (4) modifying the value of
 *	a subclass of <code>GXLAtomicValue</code> and (5) the <code>GXLAttr.setValue</code> method. 
 *	All other modifications are in reality
 *	just shorthand, and will all call one of these basic operations. In the same sense
 *	this class provides the only accessors needed as well, all the accessing methods
 *	in subclasses is just for convenience, simplifying some tasks.
 *
 *	@see GXLDocument
 *	@see GXLAtomicValue
 */
public abstract class GXLElement {
	// The GXL Document that this gxl element belongs to
	GXLDocument gxlDocument;
	// The parent of this element
	GXLElement parent;
	// The attributes of this element
	Hashtable attributes;
	// The gxl element children of this gxl element
	Vector children;
	// The name of this element
	String elementName;
	// The index of this element in the GXL.ELEMENTS array;
	int elementIndex;

	//
	// CONSTRUCTORS
	//

	/** Creates a new GXLElement. MUST be called from subclasses. */
	GXLElement(String elementName) {
		this(elementName, null);
	}

	/** Creates a new GXLElement. MUST be called from subclasses. */
	GXLElement(String elementName, Element element) {
		this.elementName = elementName;
		elementIndex = GXL.indexOf(GXL.ELEMENTS, elementName);
		// Create the child vector
		children = new Vector();
		// Create the attribute map
		attributes = new Hashtable();
	}

	/** Creates the children out of the DOM structure, MUST be called by the non-abstract "outer" GXLElements. 
	 *	This MUST be done lastly in their constructors, after they've been properly initialized. */
	void createChildren(Element element) {
		// Extra validation to make sure that the top element is named "gxl"
		if ((this instanceof GXLGXL) && !element.getNodeName().equals(GXL.GXL))
			throw new GXLValidationException(GXLValidationException.INVALID_ELEMENT_TYPE);

		// Get any attributes from the element
		NamedNodeMap elementAttributes = element.getAttributes();
		for (int i = 0; i < elementAttributes.getLength(); i++) {
			Attr a = (Attr) elementAttributes.item(i);
			if (a.getSpecified())
				setAttribute(a.getName(), a.getValue());
		}

		// Traverse the element children 
		NodeList nodes = element.getChildNodes();
		for (int i = 0; i < nodes.getLength(); i++) {
			Node node = nodes.item(i);
			// If the child is an element, add it
			if (node instanceof Element) {
				String elementName = node.getNodeName();
				GXLElement gxlElement = null;
				// Create the appropriate type (gxl is omitted since it is read by GXLDocument
				if (elementName.equals(GXL.TYPE))
					gxlElement = new GXLType((Element) node);
				else if (elementName.equals(GXL.GRAPH))
					gxlElement = new GXLGraph((Element) node);
				else if (elementName.equals(GXL.NODE))
					gxlElement = new GXLNode((Element) node);
				else if (elementName.equals(GXL.EDGE))
					gxlElement = new GXLEdge((Element) node);
				else if (elementName.equals(GXL.REL))
					gxlElement = new GXLRel((Element) node);
				else if (elementName.equals(GXL.RELEND))
					gxlElement = new GXLRelend((Element) node);
				else if (elementName.equals(GXL.ATTR))
					gxlElement = new GXLAttr((Element) node);
				else if (elementName.equals(GXL.LOCATOR))
					gxlElement = new GXLLocator((Element) node);
				else if (elementName.equals(GXL.BOOL))
					gxlElement = new GXLBool((Element) node);
				else if (elementName.equals(GXL.INT))
					gxlElement = new GXLInt((Element) node);
				else if (elementName.equals(GXL.FLOAT))
					gxlElement = new GXLFloat((Element) node);
				else if (elementName.equals(GXL.STRING))
					gxlElement = new GXLString((Element) node);
				else if (elementName.equals(GXL.ENUM))
					gxlElement = new GXLEnum((Element) node);
				else if (elementName.equals(GXL.SEQ))
					gxlElement = new GXLSeq((Element) node);
				else if (elementName.equals(GXL.SET))
					gxlElement = new GXLSet((Element) node);
				else if (elementName.equals(GXL.BAG))
					gxlElement = new GXLBag((Element) node);
				else if (elementName.equals(GXL.TUP))
					gxlElement = new GXLTup((Element) node);
				else
					throw new GXLValidationException(GXLValidationException.INVALID_ELEMENT_TYPE);

				// Add the new element to our child list
				add(gxlElement);
			}
		}
	}


	//
	// PUBLIC METHODS
	//

	/** Returns the <code>GXLDocument</code> that this element is contained in, 
	 *	or null if this element isn't contained in a <code>GXLDocument</code>
	 *	@return The <code>GXLDocument</code> that this element is contained in, 
	 *	or null if this element isn't contained in a <code>GXLDocument</code>
	 */
	public GXLDocument getDocument() {
		return gxlDocument;
	}

	/** Returns the parent of this element, or null if this element doesn't have a parent.
	 *	@return The parent of this element, or null if this element doesn't have a parent.
	 */
	public GXLElement getParent() {
		return parent;
	}

	/** Returns the index of the specified child, or -1 if the specified element isn't a child of this element. 
	 *	@param child The element to look for.
	 *	@return The index of the specified child, or -1 if the specified element isn't a child of this element. 
	 */
	public int indexOf(GXLElement child) {
		return children.indexOf(child);
	}

	/** Adds the specified child to this element.
	 *	This method is a shorthand for <code>insert(child, children.size())</code>
	 *	@param child The element to add.
	 *	@throws GXLValidationException If this action would result in an invalid GXL document.
	 */
	public void add(GXLElement child) {
		insert(child, children.size());
	}

	/** Inserts the specified child into this element's child array at the specified index. 
	 *	If the index is invalid due to implicit ordering of different child types, the index
	 *	will be automatically corrected to the closest valid value.
	 *	Note that an exception will be raised if child already has a parent. If this is the case,
	 *	remove it from it's old parent first.
	 *	@param child The child to insert.
	 *	@param index The index in this element's child array where the child should be inserted.
	 *	@throws GXLValidationException If this action would result in an invalid GXL document.
	 */
	public void insert(GXLElement child, int index) {
		// Validate this insertion (throws on error)
		GXLValidator.validateInsertion(gxlDocument, this, child);
		// All checks finished, make the insert
		GXLTreeModification mod = new GXLTreeModification(child, null, this, -1, index);
		mod.execute();
	}

	/** Returns the number of children this element has. 
	 *	@return The number of children this element has. 
	 */
	public int getChildCount() {
		return children.size();
	}

	/** Returns the specified child. 
	 *	@param i The index of the child to return.
	 *	@return The specified child.
	 */
	public GXLElement getChildAt(int i) {
		return (GXLElement) children.elementAt(i);
	}

	/** Removes the specified child from this element's child vector. 
	 *	@param child The child to remove.
	 *	@throws GXLValidationException If this action would result in an invalid GXL document.
	 */
	public void remove(GXLElement child) {
		// Don't do anything if this child is not a children of this element
		if (child.getParent() == this) {
			// Validate this removal (throws on error)
			GXLValidator.validateRemoval(gxlDocument, child);
			// All checks finished, make the removal
			GXLTreeModification mod = new GXLTreeModification(child, this, null, indexOf(child), -1);
			mod.execute();
		}
	}

	/** Returns the value of the specified attribute, or null if the attribute isn't set. 
	 *	@param name The name of the attribute to return.
	 *	@return The value of the specified attribute, or null if the attribute isn't set.
	 */
	public String getAttribute(String name) {
		// First, see if the attribute is set
		String result = (String) attributes.get(name);
		// Second, see if there is a default
		if (result == null) {
			int index = GXL.indexOf(GXL.ATTRIBUTES, name);
			if (index >= 0)
				result = GXL.ATTRIBUTE_DEFAULTS[index];
		}
		return result;
	}

	/** Sets the specified attribute. 
	 *	To remove an attribute, specify the value as <code>null</code>.
	 *	See the specific element class to see which attributes are valid.
	 *	@param name The name of the attribute to set.
	 *	@param value The new value of the attribute.
	 *	@throws GXLValidationException If this action would result in an invalid GXL document.
	 */
	public void setAttribute(String name, String value) {
		// if the attribute is of an enumerated type, swap value with the GXL.xxx counterpart
		// This will enable String == String comparison for equality (instead of String.equals(String))
		int attributeIndex = GXL.indexOf(GXL.ATTRIBUTES, name);
		if (attributeIndex != -1) {
			if (GXL.ATTRIBUTE_VALUES[attributeIndex] != null) {
				int valueIndex = GXL.indexOf(GXL.ATTRIBUTE_VALUES[attributeIndex], value);
				if (valueIndex != -1)
					value = GXL.ATTRIBUTE_VALUES[attributeIndex][valueIndex];
			}
		}
		// Validate this attribute change (throws on error)
		GXLValidator.validateAttributeModification(gxlDocument, this, name, value);
		// All checks finished, make the change
		GXLAttributeModification mod = new GXLAttributeModification(this, name, (String) attributes.get(name), value);
		mod.execute();
	}

	//
	// PACKAGE METHODS
	//

	/** Builds a new DOM document recursively. */
	void buildDOM(Document document, Element parent) {
		// Create our element
		Element element = document.createElement(getElementName());
		// Add any attributes
		Iterator it = attributes.keySet().iterator();
		while (it.hasNext()) {
			String key = (String) it.next();
			String value = (String) attributes.get(key);
			element.setAttribute(key, value);
		}
		// Append it to parent
		if (parent != null)
			parent.appendChild(element);
		else
			document.appendChild(element);
		// build children
		for (int i = 0; i < children.size(); i++)
			((GXLElement) children.elementAt(i)).buildDOM(document, element);
	}

	/** Returns the name of this element, ie "graph", "node" etc. */
	String getElementName() {
		return elementName;
	}

	/** Returns the index of this element in the GXL class arrays. */
	int getElementIndex() {
		return elementIndex;
	}
}
