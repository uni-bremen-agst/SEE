/*
 * @(#)GXLSeq.java	0.8 2003-09-25
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

/** Represents the GXL <code>seq</code> element. 
 *	<p>
 *	The <code>seq</code> element is a composite value element to describe a ordered sequence.
 *	<p>
 *	Note that the <code>seq</code> element requires all it's value elements to be of the same value type (that is, it must be
 *	a seq of <code>int</code>s or a seq of <code>string</code>s etc).
 *	<p>
 *	<b>Children</b><br>
 *	<code>GXLValue</code> - a <code>seq</code> can contain zero or more values<br>
 *	<p>
 *	<b>Attributes</b><br>
 *	The <code>seq</code> element may not contain any attributes.
 *
 *	@see GXLValue
 */
public class GXLSeq extends GXLCompositeValue {
	//
	// CONSTRUCTORS
	//

	/** Creates a new (empty) <code>GXLSeq</code> element. */
	public GXLSeq() {
		super(GXL.SEQ);
	}

	/** Creates a new GXLSeq element (used when reading a document). */
	GXLSeq(Element element) {
		super(GXL.SEQ, element);
		createChildren(element);
	}

	//
	// PUBLIC METHODS
	//
	
	/** Compares this sequence to another object. 
	 *	Equality is defined as containing the same elements in the same order.
	 *	@param o The other object to compare this to.
	 *	@return Whether this seq is considered equal to the compared object.
	 *	@throws ClassCastException If the compared object is not an instance of <code>GXLSeq</code>.
	 */
	public boolean equals(Object o) {
		GXLSeq seq = (GXLSeq) o;
		boolean result = getValueCount() == seq.getValueCount();
		for (int i = 0; result && i < getValueCount(); i++)
			result = getValueAt(i).equals(seq.getValueAt(i));
		return result;
	}
}
