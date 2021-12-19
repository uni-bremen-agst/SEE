/*
 * @(#)GXLBool.java	0.9 2003-12-07
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

/** Represents the GXL <code>bool</code> element. 
 *	<p>
 *	The <code>bool</code> element represents a boolean value, either <code>true</code> or <code>false</code>
 *	<p>
 *	<b>Children</b><br>
 *	The <code>bool</code> element may not contain any children.
 *	<p>
 *	<b>Attributes</b><br>
 *	The <code>bool</code> element may not contain any attributes.
 */
public class GXLBool extends GXLAtomicValue {
	boolean booleanValue;

	//
	// CONSTRUCTORS
	//

	/** Creates a new <code>GXLBool</code> element. 
	 *	@param booleanValue The value encapsulated by the new <code>GXLBool</code>.
	 */
	public GXLBool(boolean booleanValue) {
		super(GXL.BOOL, String.valueOf(booleanValue));
		this.booleanValue = booleanValue;
	}

	/** Creates a new GXLBool element (used when reading a document). 
	 *	@param element The DOM element to copy information from. 
	 */
	GXLBool(Element element) {
		super(GXL.BOOL, element);
		// The constructor of GXLAtomicValue has initialized our value member
		booleanValue = new Boolean(value).booleanValue();
		createChildren(element);
	}

	//
	// VALUE ACCESSORS
	//
	
	/** Returns the encapsulated value of this <code>GXLBool</code> as a boolean. 
	 *	@return The encapsulated boolean value.
	 *	@see GXLAtomicValue#getValue()
	 */
	public boolean getBooleanValue() {
		return booleanValue;
	}

	/** Sets the encapsulated boolean value. 
	 *	@param booleanValue The new value.
	 *	@see GXLAtomicValue#setValue(String value)
	 */
	public void setBooleanValue(boolean booleanValue) {
		setValue(String.valueOf(booleanValue));
	}

	//
	// PUBLIC METHODS
	//
	
	/** Compares this <code>GXLBool</code> to another object. 
	 *	@param o The other object to compare this to.
	 *	@return Whether this <code>GXLBool</code> is considered equal to the compared object.
	 *	@throws ClassCastException If the compared object is not an instance of <code>GXLBool</code>.
	 */
	public boolean equals(Object o) {
		GXLBool b = (GXLBool) o;
		return b.booleanValue == booleanValue;
	}
}
