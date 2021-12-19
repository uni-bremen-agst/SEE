/*
 * @(#)GXLAttributeModificationEvent.java	0.9 2003-12-07
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

/** The event that is passed to <code>GXLDocumentListener</code>s when an attribute has been modified. 
 *
 *	@see GXLDocumentListener
 *	@see GXLElement#setAttribute(String name, String value)
 */
public class GXLAttributeModificationEvent extends EventObject {
	// The element that has had an attribute modified.
	GXLElement element;
	// The name of the attribute that was modified.
	String name;
	// The new value of the attribute.
	String value;

	/** Creates a new GXLAttributeModificationEvent. 
	 *	@param element The element that has had an attribute modified.
	 *	@param name The name of the attribute that was modified.
	 *	@param value The new value of the attribute.
	 */
	GXLAttributeModificationEvent(GXLElement element, String name, String value) {
		super(element.getDocument());
		this.element = element;
		this.name = name;
		this.value = value;
	}

	/** Returns the element whose attribute has been modified. 
	 *	@return The element whose attribute has been modified.
	 */
	public GXLElement getElement() {
		return element;
	}

	/** Returns the name of the attribute that has been modified. 
	 *	@return The name of the attribute that has been modified.
	 */
	public String getAttributeName() {
		return name;
	}

	/** Returns the new value of the attribute. 
	 *	@return The new value of the attribute. 
	 */
	public String getValue() {
		return value;
	}
}
