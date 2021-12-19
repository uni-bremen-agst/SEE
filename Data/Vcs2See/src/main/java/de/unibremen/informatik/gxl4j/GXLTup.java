/*
 * @(#)GXLTup.java	0.8 2003-09-25
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

/** Represents the GXL <code>tup</code> element. 
 *	<p>
 *	The <code>tup</code> element is a composite value element to describe a tuple.
 *	To define the cardinality of the tuple and which types should be present a GXL Schema should be used.
 *	<p>
 *	Note that the <code>tup</code> element is the only composite value element to allow differing value types.
 *	<p>
 *	<b>Children</b><br>
 *	<code>GXLValue</code> - a <code>tup</code> can contain zero or more values<br>
 *	<p>
 *	<b>Attributes</b><br>
 *	The <code>tup</code> element may not contain any attributes.
 *
 *	@see GXLValue
 */
public class GXLTup extends GXLCompositeValue {
	//
	// CONSTRUCTORS
	//

	/** Creates a new (empty) <code>GXLTup</code> element. */
	public GXLTup() {
		super(GXL.TUP);
	}

	/** Creates a new GXLTup element (called when reading a document). */
	GXLTup(Element element) {
		super(GXL.TUP, element);
		createChildren(element);
	}

	//
	// PUBLIC METHODS
	//
	
	/** Compares this tuple to another object. 
	 *	Equality is defined as containing the same elements in the same order.
	 *	@param o The other object to compare this to.
	 *	@return Whether this tuple is considered equal to the compared object.
	 *	@throws ClassCastException If the compared object is not an instance of <code>GXLTup</code>.
	 */
	public boolean equals(Object o) {
		GXLTup tup = (GXLTup) o;
		boolean result = getValueCount() == tup.getValueCount();
		for (int i = 0; result && i < getValueCount(); i++)
			result = getValueAt(i).equals(tup.getValueAt(i));
		return result;
	}
}
