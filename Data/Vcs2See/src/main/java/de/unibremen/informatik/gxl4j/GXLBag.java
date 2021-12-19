/*
 * @(#)GXLBag.java	0.9 2003-12-07
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

/** Represents the GXL <code>bag</code> element. 
 *	<p>
 *	The <code>bag</code> element is a composite value element to describe a bag or multiset, a set that permits duplicates if you will.
 *	<p>
 *	Note that the <code>bag</code> element requires all it's value elements to be of the same value type (that is, it must be
 *	a bag of <code>int</code>s or a bag of <code>string</code>s etc).
 *	<p>
 *	<b>Children</b><br>
 *	<code>GXLValue</code> - a <code>bag</code> can contain zero or more values<br>
 *	<p>
 *	<b>Attributes</b><br>
 *	The <code>bag</code> element may not contain any attributes.
 *
 *	@see GXLValue
 */
public class GXLBag extends GXLCompositeValue {
	//
	// CONSTRUCTORS
	//

	/** Creates a new (empty) <code>GXLBag</code> element. */
	public GXLBag() {
		super(GXL.BAG);
	}

	/** Creates a new GXLBag element (used when reading a document). 
	 *	@param element The DOM element to copy information from. 
	 */
	GXLBag(Element element) {
		super(GXL.BAG, element);
		createChildren(element);
	}

	//
	// VALUE ACCESSORS
	//
	
	/** Returns the number of occurences of the specified element that this bag contains (comparation is done with the equals method). 
	 *	@param element The element to search for occurrences of.
	 *	@return The number of occurrences of the specified element.
	 */
	public int getCardinal(GXLValue element) {
		int result = 0;
		for (int i = 0; i < getValueCount(); i++)
			if (element.equals(getValueAt(i)))
				result++;
		return result;
	}

	//
	// PUBLIC METHODS
	//
	
	/** Compares this bag to another object. Equality is defined as containing the same elements and that all
	 *	the elements have the same cardinality.
	 *	@param o The other object to compare this to.
	 *	@return Whether this bag is considered equal to the compared object.
	 *	@throws ClassCastException If the compared object is not an instance of <code>GXLBag</code>.
	 *	@see #getCardinal(GXLValue)
	 */
	public boolean equals(Object o) {
		GXLBag bag = (GXLBag) o;
		boolean result = getValueCount() == bag.getValueCount();
		for (int i = 0; result && i < getValueCount(); i++)
			result = bag.getCardinal(getValueAt(i)) == getCardinal(getValueAt(i));
		return result;
	}
}
