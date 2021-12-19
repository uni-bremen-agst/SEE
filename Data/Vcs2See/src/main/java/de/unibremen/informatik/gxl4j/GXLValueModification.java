/*
 * @(#)GXLValueModification.java	0.9 2003-09-25
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

import javax.swing.undo.AbstractUndoableEdit;

/** Encapsulates a value modification, providing the implementation of it and undo support. */
class GXLValueModification extends GXLDocumentModification {
	/// The element to change value on
	GXLAtomicValue gxlElement;
	/// The old value
	String oldValue;
	/// The new value
	String newValue;

	//
	// CONSTRUCTORS
	//

	/** Creates a new GXLValueModification. */
	GXLValueModification(GXLAtomicValue gxlElement, String oldValue, String newValue) {
		this.gxlElement = gxlElement;
		this.oldValue = oldValue;
		this.newValue = newValue;
	}

	//
	// PUBLIC METHODS
	//

	/** Restores the old value. */
	public void undo() {
		super.undo();
		handle(newValue, oldValue);
	}
	/** Reexecutes the value change. */
	public void redo() {
		super.redo();
		handle(oldValue, newValue);
	}

	//
	// PACKAGE METHODS
	//

	/** Executes the value change. */
	void execute() {
		handle(oldValue, newValue);
		// Notify any document
		if (gxlElement.gxlDocument != null)
			gxlElement.gxlDocument.postEdit(this);
	}

	//
	// PRIVATE METHODS
	//

	/** Does the actual work. */
	private void handle(String oldValue, String newValue) {
		// Change the value
		gxlElement.value = newValue;
		// Change cashed converted value
		if (gxlElement instanceof GXLBool)
			((GXLBool) gxlElement).booleanValue = new Boolean(newValue).booleanValue();
		else if (gxlElement instanceof GXLFloat)
			((GXLFloat) gxlElement).floatValue = new Float(newValue).floatValue();
		else if (gxlElement instanceof GXLInt)
			((GXLInt) gxlElement).intValue = new Integer(newValue).intValue();

		// Notify any listeners
		if (gxlElement.gxlDocument != null && gxlElement.gxlDocument.gxlDocumentListeners.size() > 0)
			fireValueModificationEvent(new GXLValueModificationEvent(gxlElement, newValue));
	}

	/** Notifies listeners of the change. */
	private void fireValueModificationEvent(GXLValueModificationEvent e) {
		for (int i = 0; i < gxlElement.gxlDocument.gxlDocumentListeners.size(); i++)
			((GXLDocumentListener) gxlElement.gxlDocument.gxlDocumentListeners.elementAt(i)).gxlValueChanged(e);
	}
}
