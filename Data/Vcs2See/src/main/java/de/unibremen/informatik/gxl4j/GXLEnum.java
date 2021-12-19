/*
 * @(#)GXLEnum.java	0.9 2003-12-08
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

/** Represents the GXL <code>enum</code> element. 
 *	<p>
 *	The <code>enum</code> element represents a enumerated string value.
 *	To specify which values an enumerated value can take, a GXL Schema
 *	should be used.
 *	<p>
 *	<b>Children</b><br>
 *	The <code>enum</code> element may not contain any children.
 *	<p>
 *	<b>Attributes</b><br>
 *	The <code>enum</code> element may not contain any attributes.
 */
public class GXLEnum extends GXLAtomicValue {
	//
	// CONSTRUCTORS
	//

	/** Creates a new <code>GXLEnum</code> element. 
	 *	@param value The value encapsulated by the new <code>GXLEnum</code>.
	 */
	public GXLEnum(String value) {
		super(GXL.ENUM, value);
	}

	/** Creates a new GXLEnum element (used when reading a document). */
	GXLEnum(Element element) {
		super(GXL.ENUM, element);
		createChildren(element);
	}

	//
	// PUBLIC METHODS
	//
	
	/** Compares this <code>GXLEnum</code> to another object. 
	 *	@param o The other object to compare this to.
	 *	@return Whether this <code>GXLEnum</code> is considered equal to the compared object.
	 *	@throws ClassCastException If the compared object is not an instance of <code>GXLEnum</code>.
	 */
	public boolean equals(Object o) {
		GXLEnum e = (GXLEnum) o;
		return e.value.equals(value);
	}
}
