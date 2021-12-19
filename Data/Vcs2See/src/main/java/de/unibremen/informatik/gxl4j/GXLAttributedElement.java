/*
 * @(#)GXLAttributedElement.java	0.9 2003-12-07
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

import org.w3c.dom.Element;

/** Superclass for all <code>GXLElement</code>s that can be attributed using <code>attr</code> elements. 
 *	<p>
 *	Attributes on GXL elements are set by adding <code>attr</code> elements to the attributed element.
 *	Thus to give a <code>node</code> the attribute <i>color</i> with a value of <i>blue</i> you create
 *	a <code>GXLAttr</code> element, specifying it's name as <i>color</i> and it's value as a <code>GXLString</code>
 *	containing the string <i>blue</i>.
 *	<p>
 *	The UML definition of GXL defines that all attributed elements may contain the attribute <code>id</code> resulting
 *	in the methods <code>getID</code> and <code>setID</code> being in this class. It should however be noted that the
 *	element <code>relend</code> is an exception to this and trying to set the id of a <code>relend</code> will result
 *	in an exception.
 *
 *	@see GXLAttr
 *	@see GXLValue
 */
public abstract class GXLAttributedElement extends GXLElement {
	//
	// CONSTRUCTORS
	//

	/** Creates a new GXLAttributedElement. MUST be called by subclasses. */
	GXLAttributedElement(String elementName) {
		super(elementName);
	}

	/** Creates a new GXLAttributedElement. MUST be called by subclasses. */
	GXLAttributedElement(String elementName, Element element) {
		super(elementName, element);
	}

	//
	// PUBLIC METHODS
	//
	
	/** Returns the <code>attr</code> child element with the specified name.
	 *	@param name The name of the attribute to return.
	 *	@return The specified <code>attr</code> element, or null if no such element exists.
	 *	@see #getAttrAt(int i)
	 *	@see #setAttr(String name, GXLValue value)
	 *	@since 0.91
	 */
	public GXLAttr getAttr(String name) {
		GXLAttr result = null;
		for (int i = 0; i < getAttrCount() && result == null; i++)
			if (getAttrAt(i).getName().equals(name))
				result = getAttrAt(i);
		return result;
	}

	/** Sets the value of the specified attribute, creating it if necessary.
	 *	If the specified attribute, it's value will be changed using the <code>GXLAttr.setValue</code> method,
	 *	otherwise a new GXLAttr will be created with the specified value.
	 *	@param name The name of the attribute to set.
	 *	@param value The new value of the attribute.
	 *	@see #getAttr(String name)
	 *	@see GXLAttr#setValue(GXLValue value)
	 *	@since 0.91
	 */
	public void setAttr(String name, GXLValue value) {
		GXLAttr a = getAttr(name);
		if (a != null)
			a.setValue(value);
		else {
			a = new GXLAttr(name, value);
			add(a);
		}
	}

	//
	// CHILD TYPE ACCESSORS
	//

	/** Returns the number of <code>attr</code> elements contained in this element.
	 *	@return The number of <code>attr</code> elements contained in this element.
	 *	@see #getChildCount()
	 */
	public int getAttrCount() {
		int start = 0;
		while (start < getChildCount() && !(getChildAt(start) instanceof GXLAttr))
			start++;
		int result = 0;
		while (start+result < getChildCount() && (getChildAt(start+result) instanceof GXLAttr))
			result++;
		return result;
	}

	/** Returns the specified <code>attr</code> element. 
	 *	Note that these element can also be accessed via the <code>getChildAt</code> method.
	 *	@param i The index of the specified <code>attr</code> element.
	 *	@return The specified <code>attr</code> element.
	 *	@see #getChildAt(int)
	 */
	public GXLAttr getAttrAt(int i) {
		int start = 0;
		while (start < getChildCount() && !(getChildAt(start) instanceof GXLAttr))
			start++;
		return (GXLAttr) getChildAt(start+i);
	}

	//
	// ATTRIBUTE ACCESSORS
	//

	/** Sets the value of the <code>id</code> attribute. 
	 *	This simply calls <code>setAttribute(GXL.KIND, kind)</code>.
	 *	@param name The new value.
	 *	@throws GXLValidationException If this action would result in an invalid GXL document.
	 *	@see #setAttribute(String, String)
	 */
	public void setID(String id) {
		setAttribute(GXL.ID, id);
	}

	/** Returns the value of the <code>id</code> attribute or <code>null</code> if the attribute isn't set.
	 *	This simply calls <code>getAttribute(GXL.ID)</code>.
	 *	@return The value of the <code>id</code> attribute.
	 *	@see #getAttribute(String)
	 */
	public String getID() {
		return getAttribute(GXL.ID);
	}
}
