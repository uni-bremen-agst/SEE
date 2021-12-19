/*
 * @(#)GXLFloat.java	0.9 2003-12-08
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

/** Represents the GXL <code>float</code> element. 
 *	<p>
 *	The <code>float</code> element represents a float value.
 *	<p>
 *	<b>Children</b><br>
 *	The <code>float</code> element may not contain any children.
 *	<p>
 *	<b>Attributes</b><br>
 *	The <code>float</code> element may not contain any attributes.
 */
public class GXLFloat extends GXLAtomicValue {
	float floatValue;

	//
	// CONSTRUCTORS
	//

	/** Creates a new <code>GXLFloat</code> element. 
	 *	@param floatValue The value encapsulated by the new <code>GXLFloat</code>.
	 */
	public GXLFloat(float floatValue) {
		super(GXL.FLOAT, String.valueOf(floatValue));
		this.floatValue = floatValue;
	}

	/** Creates a new GXLFloat element (used when reading a document). */
	GXLFloat(Element element) {
		super(GXL.FLOAT, element);
		// The constructor of GXLAtomicValue has initialized our value member
		floatValue = new Float(value).floatValue();	
		createChildren(element);
	}

	//
	// VALUE ACCESSORS
	//

	/** Returns the encapsulated value of this <code>GXLFloat</code> as a float. 
	 *	@return The encapsulated float value.
	 *	@see GXLAtomicValue#getValue()
	 */
	public float getFloatValue() {
		return floatValue;
	}

	/** Sets the encapsulated float value. 
	 *	@param floatValue The new value.
	 *	@see GXLAtomicValue#setValue(String value)
	 */
	public void setFloatValue(float floatValue) {
		setValue(String.valueOf(floatValue));
	}

	//
	// PUBLIC METHODS
	//
	
	/** Compares this <code>GXLFloat</code> to another object. 
	 *	@param o The other object to compare this to.
	 *	@return Whether this <code>GXLFloat</code> is considered equal to the compared object.
	 *	@throws ClassCastException If the compared object is not an instance of <code>GXLFloat</code>.
	 */
	public boolean equals(Object o) {
		GXLFloat f = (GXLFloat) o;
		return f.floatValue == floatValue;
	}
}
