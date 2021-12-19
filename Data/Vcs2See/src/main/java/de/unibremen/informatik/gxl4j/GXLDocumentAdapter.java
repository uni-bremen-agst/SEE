/*
 * @(#)GXLDocumentAdapter.java	0.91 2004-03-20
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

/** Adapter for the GXLDocumentListener interface, providing empty method bodies.
 *	@see GXLDocumentListener
 *	@since 0.91
 */
public class GXLDocumentAdapter {
	/** Invoked when a new element has been inserted into the document. 
	 *	@param e The event describing the modification.
	 */
	public void gxlElementInserted(GXLTreeModificationEvent e) {
	}

	/** Invoked when an element has been removed from the document.
	 *	@param e The event describing the modification.
	 */
	public void gxlElementRemoved(GXLTreeModificationEvent e) {
	}

	/** Invoked when an attribute of an element has been modified.
	 *	@param e The event describing the modification.
	 */
	public void gxlAttributeChanged(GXLAttributeModificationEvent e) {
	}

	/** Invoked when an atomic value has been modified.
	 *	@param e The event describing the modification.
	 */
	public void gxlValueChanged(GXLValueModificationEvent e) {
	}

	/** Invoked when an <code>attr</code> elements value has been replaced.
	 *	This has been added to accomodate changing value types, which was rather tedious before.
	 *	@param e The event describing the modification.
	 *	@see #gxlValueChanged(GXLValueModificationEvent)
	 *	@since 0.91
	 */
	public void gxlAttrValueChanged(GXLAttrValueModificationEvent e) {
	}
}
