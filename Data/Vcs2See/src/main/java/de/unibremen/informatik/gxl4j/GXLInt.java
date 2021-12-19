/*
 * @(#)GXLInt.java	0.9 2003-12-08
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

/** Represents the GXL <code>int</code> element. 
 *	<p>
 *	The <code>int</code> element represents a integer value.
 *	<p>
 *	<b>Children</b><br>
 *	The <code>int</code> element may not contain any children.
 *	<p>
 *	<b>Attributes</b><br>
 *	The <code>int</code> element may not contain any attributes.
 */
public class GXLInt extends GXLAtomicValue {
	int intValue;

	//
	// CONSTRUCTORS
	//

	/** Creates a new <code>GXLInt</code> element. 
	 *	@param booleanValue The value encapsulated by the new <code>GXLInt</code>.
	 */
	public GXLInt(int intValue) {
		super(GXL.INT, String.valueOf(intValue));
		this.intValue = intValue;
	}

	/** Creates a new GXLInt element (used when reading a document). */
	GXLInt(Element element) {
		super(GXL.INT, element);
		// The constructor of GXLAtomicValue has initialized our value member
		intValue = new Integer(value).intValue();	
		createChildren(element);
	}

	//
	// VALUE ACCESSORS
	//

	/** Returns the encapsulated value of this <code>GXLInt</code> as an int. 
	 *	@return The encapsulated integer value.
	 *	@see GXLAtomicValue#getValue()
	 */
	public int getIntValue() {
		return intValue;
	}

	/** Sets the encapsulated integer value. 
	 *	@param intValue The new value.
	 *	@see GXLAtomicValue#setValue(String value)
	 */
	public void setIntValue(int intValue) {
		setValue(String.valueOf(intValue));
	}

	//
	// PUBLIC METHODS
	//
	
	/** Compares this <code>GXLInt</code> to another object. 
	 *	@param o The other object to compare this to.
	 *	@return Whether this <code>GXLInt</code> is considered equal to the compared object.
	 *	@throws ClassCastException If the compared object is not an instance of <code>GXLInt</code>.
	 */
	public boolean equals(Object o) {
		GXLInt i = (GXLInt) o;
		return i.intValue == intValue;
	}
}
