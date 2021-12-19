/*
 * @(#)GXLTreeModificationEvent.java	0.9 2003-12-20
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

/** The event that is passed to <code>GXLDocumentListener</code>s when an element has been inserted or removed. 
 *
 *	@see GXLDocumentListener
 *	@see GXLElement#add(GXLElement child)
 *	@see GXLElement#remove(GXLElement child)
 */
public class GXLTreeModificationEvent extends EventObject {
	GXLElement parent, child;

	/** Creates a new GXLTreeModificationEvent. */
	GXLTreeModificationEvent(GXLElement parent, GXLElement child) {
		super(parent.getDocument());
		this.parent = parent;
		this.child = child;
	}

	/** Returns the parent that the element was inserted into or removed from. 
	 *	@return The parent that the element was inserted into or removed from. 
	 */
	public GXLElement getParent() {
		return parent;
	}

	/** Returns the element that was inserted or removed. 
	 *	@return The element that was inserted or removed. 
	 */
	public GXLElement getElement() {
		return child;
	}
}
