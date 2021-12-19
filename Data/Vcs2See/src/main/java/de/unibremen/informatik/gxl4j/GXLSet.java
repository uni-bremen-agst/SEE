/*
 * @(#)GXLSet.java	0.8 2003-09-25
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

/** Represents the GXL <code>set</code> element. 
 *	<p>
 *	The <code>set</code> element is a composite value element to describe a set, unordered and without duplicates.
 *	That being said, it is up to the application to ensure that no duplicates exist and treat the values unorderly.
 *	<p>
 *	Note that the <code>set</code> element requires all it's value elements to be of the same value type (that is, it must be
 *	a set of <code>int</code>s or a set of <code>string</code>s etc).
 *	<p>
 *	<b>Children</b><br>
 *	<code>GXLValue</code> - a <code>set</code> can contain zero or more values<br>
 *	<p>
 *	<b>Attributes</b><br>
 *	The <code>set</code> element may not contain any attributes.
 *
 *	@see GXLValue
 */
public class GXLSet extends GXLCompositeValue {
	//
	// CONSTRUCTORS
	//

	/** Creates a new (empty) <code>GXLSet</code> element. */
	public GXLSet() {
		super(GXL.SET);
	}

	/** Creates a new GXLSet element (used when reading a document). */
	GXLSet(Element element) {
		super(GXL.SET, element);
		createChildren(element);
	}

	//
	// VALUE ACCESSORS
	//
	
	/** Returns whether this set contains the specified element.
	 *	Comparation is done with the equals method. To find out which elements
	 *	are contained in this set, use the <code>getChildAt(int i)</code> method.
	 *	@param element The element to search for.
	 *	@return Whether this set contains the specified element.
	 *	@see #getChildAt(int)
	 */
	public boolean contains(GXLValue element) {
		boolean result = false;
		for (int i = 0; !result && i < getValueCount(); i++)
			result = element.equals(getValueAt(i));
		return result;
	}

	//
	// PUBLIC METHODS
	//
	
	/** Compares this set to another object. 
	 *	Equality is defined as containing the same elements regardless of order.
	 *	@param o The other object to compare this to.
	 *	@return Whether this set is considered equal to the compared object.
	 *	@throws ClassCastException If the compared object is not an instance of <code>GXLSet</code>.
	 *	@see #contains(GXLValue)
	 */
	public boolean equals(Object o) {
		GXLSet set = (GXLSet) o;
		boolean result = getValueCount() == set.getValueCount();
		for (int i = 0; result && i < getValueCount(); i++)
			result = set.contains(getValueAt(i));
		return result;
	}
}
