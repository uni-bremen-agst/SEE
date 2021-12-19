/*
 * @(#)GXLString.java	0.8 2003-09-25
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

/** Represents the GXL <code>string</code> element. 
 *	<p>
 *	The <code>string</code> element represents a string value.
 *	<p>
 *	<b>Children</b><br>
 *	The <code>string</code> element may not contain any children.
 *	<p>
 *	<b>Attributes</b><br>
 *	The <code>string</code> element may not contain any attributes.
 */
public class GXLString extends GXLAtomicValue {
	//
	// CONSTRUCTORS
	//

	/** Creates a new <code>GXLString</code> element. 
	 *	@param value The value encapsulated by the new <code>GXLString</code>.
	 */
	public GXLString(String value) {
		super(GXL.STRING, value);
	}

	/** Creates a new GXLString element (called when reading a document). */
	GXLString(Element element) {
		super(GXL.STRING, element);
		// GXLAtomicValue constructor reads our value member
		createChildren(element);
	}

	//
	// PUBLIC METHODS
	//
	
	/** Compares this <code>GXLString</code> to another object. 
	 *	Equality is compared by using the java.lang.String.equals method.
	 *	@param o The other object to compare this to.
	 *	@return Whether this <code>GXLString</code> is considered equal to the compared object.
	 *	@throws ClassCastException If the compared object is not an instance of <code>GXLString</code>.
	 */
	public boolean equals(Object o) {
		GXLString s = (GXLString) o;
		return s.value.equals(value);
	}
}
