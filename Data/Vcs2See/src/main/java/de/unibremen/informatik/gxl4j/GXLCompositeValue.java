/*
 * @(#)GXLCompositeValue.java	0.9 2003-12-07
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

/** Superclass for all composite value elements (that is <code>bag, set, seq</code> and <code>tup</code>). 
 *
 *	@see GXLBag
 *	@see GXLSet
 *	@see GXLSeq
 *	@see GXLTup
 */
public abstract class GXLCompositeValue extends GXLValue {
	//
	// CONSTRUCTORS
	//

	/** Creates a new GXLCompositeValue. MUST be called by subclasses. */
	GXLCompositeValue(String elementName) {
		super(elementName);
	}

	/** Creates a new GXLCompositeValue. MUST be called by subclasses. 
	 *	@param element The DOM element to copy information from. 
	 */
	GXLCompositeValue(String elementName, Element element) {
		super(elementName, element);
	}

	//
	// CHILD TYPE ACCESSORS
	//

	/** Returns the number of value elements contained in this composite value. 
	 *	@return The number of value elements contained in this composite value. 
	 *	@see #getChildCount()
	 */
	public int getValueCount() {
		return getChildCount();
	}

	/** Returns the specified value element. 
	 *	Note that these element can also be accessed via the <code>getChildAt</code> method.
	 *	@param i The index of the specified value element.
	 *	@return The specified value element.
	 *	@see #getChildAt(int)
	 */
	public GXLValue getValueAt(int i) {
		return (GXLValue) getChildAt(i);
	}
}
