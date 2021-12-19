/*
 * @(#)GXLAtomicValue.java	0.9 2003-12-07
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

import org.w3c.dom.Document;
import org.w3c.dom.Element;
import org.w3c.dom.Text;

/** Superclass of all atomic values. */
public abstract class GXLAtomicValue extends GXLValue {
	/// The encapsulated value.
	String value;

	//
	// CONSTRUCTORS
	//

	/** Creates a new GXLAtomicValue. MUST be called from subclasses.
	 *	@param value The value to encapsulate. 
	 */
	GXLAtomicValue(String elementName, String value) {
		super(elementName);
		setValue(value);
	}

	/** Creates a new GXLAtomicValue. MUST be called from subclasses.
	 *	@param element The DOM element to copy information from. 
	 */
	GXLAtomicValue(String elementName, Element element) {
		super(elementName, element);
		// Get the value
		Text text = (Text) element.getFirstChild();
		if (text != null)
			setValue(text.getData());
		else
			setValue("");
	}

	//
	// VALUE ACCESSORS
	//

	/** Returns the encapsulated value as a <code>String</code>. 
	 *	@return The encapsulated value. 
	 */
	public String getValue() {
		return value;
	}

	/** Sets the encapsulated value. 
	 *	Note that subclasses may override this method to perform a conversion (as for example <code>GXLInt</code>).
	 *	@param value The new value.
	 *	@throws GXLValidationException If this action would result in an invalid GXL document.
	 */
	public void setValue(String value) {
		// Validate this value modification (throws on error)
		GXLValidator.validateValueModification(gxlDocument, this, value);
		// All checks finished, make the change
		GXLValueModification mod = new GXLValueModification(this, this.value, value);
		mod.execute();
	}

	//
	// PACKAGE METHODS
	//

	/** Builds a new DOM document recursively. 
	 *	@param document The DOM document that is being built.
	 *	@param parent The (already built, DOM element) parent of the element that this method will build. 
	 */
	void buildDOM(Document document, Element parent) {
		// Let GXLElement do it's work
		super.buildDOM(document, parent);
		// Now we need to add our value
		// To do that, get parents last child (should be the one representing this element)
		Element element = (Element) parent.getLastChild();
		// Create a Text node
		Text text = document.createTextNode(value);
		// append it to our element
		element.appendChild(text);
	}
}
