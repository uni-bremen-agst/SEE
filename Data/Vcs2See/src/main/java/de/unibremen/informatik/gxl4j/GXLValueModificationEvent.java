/*
 * @(#)GXLValueModificationEvent.java	0.9 2003-12-20
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

/** The event that is passed to <code>GXLDocumentListener</code>s when a <code>GXLAtomicValue</code> has been modified. 
 *
 *	@see GXLDocumentListener
 *	@see GXLAtomicValue#setValue(String value)
 */
public class GXLValueModificationEvent extends EventObject {
	GXLAtomicValue element;
	String value;

	/** Creates a new GXLValueModificationEvent. */
	GXLValueModificationEvent(GXLAtomicValue element, String value) {
		super(element.getDocument());
		this.element = element;
		this.value = value;
	}

	/** Returns the <code>GXLAtomicValue</code> element whose value has changed. 
	 *	@return The <code>GXLAtomicValue</code> element whose value has changed. 
	 */
	public GXLAtomicValue getElement() {
		return element;
	}

	/** Returns the new value. 
	 *	@return The new value.
	 */
	public String getValue() {
		return value;
	}
}
