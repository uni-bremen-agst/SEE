/*
 * @(#)GXLAttrValueModificationEvent.java	0.91 2004-03-03
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

import java.util.EventObject;

/** The event that is passed to <code>GXLDocumentListener</code>s when an <code>attr</code> elements
 *	value has been replaced. 
 *
 *	@see GXLDocumentListener
 */
public class GXLAttrValueModificationEvent extends EventObject {
	// The attr that has had its value replaced.
	GXLAttr element;
	// The new value of the attribute.
	GXLValue value;

	/** Creates a new GXLAttrValueModificationEvent. 
	 *	@param element The attr that has had its value replaced.
	 *	@param value The new value of the attribute.
	 */
	GXLAttrValueModificationEvent(GXLAttr element, GXLValue value) {
		super(element.getDocument());
		this.element = element;
		this.value = value;
	}

	/** Returns the <code>attr</code> element whose value has been replaced. 
	 *	@return The <code>attr</code> element whose value has been replaced.
	 */
	public GXLAttr getElement() {
		return element;
	}

	/** Returns the new value of the attribute. 
	 *	@return The new value of the attribute. 
	 */
	public GXLValue getValue() {
		return value;
	}
}
