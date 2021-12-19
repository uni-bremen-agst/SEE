/*
 * @(#)GXLAttrValueModification.java	0.91 2004-02-06
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

/** Encapsulates an attr value change, providing the implementation of it and undo support. */
class GXLAttrValueModification extends GXLDocumentModification {
	GXLAttr gxlAttr;
	GXLValue oldValue;
	GXLValue newValue;

	//
	// CONSTRUCTORS
	//

	/** Creates a new GXLAttrValueModification. */
	GXLAttrValueModification(GXLAttr gxlAttr, GXLValue oldValue, GXLValue newValue) {
		this.gxlAttr = gxlAttr;
		this.oldValue = oldValue;
		this.newValue = newValue;
	}

	//
	// PUBLIC METHODS
	//

	/** Undoes the change. */
	public void undo() {
		super.undo();
		handle(newValue, oldValue);
	}

	/** Redoes the change. */
	public void redo() {
		super.redo();
		handle(oldValue, newValue);
	}

	//
	// PACKAGE METHODS
	//

	/** Executes the change. */
	void execute() {
		handle(oldValue, newValue);
		// Find the gxl document and notify it
		if (gxlAttr.gxlDocument != null)
			gxlAttr.gxlDocument.postEdit(this);
	}

	//
	// PRIVATE METHODS
	//

	/** Does the actual work. */
	private void handle(GXLValue oldValue, GXLValue newValue) {
		// Remove oldValue from the document altogether
		if (gxlAttr.gxlDocument != null)
			removeFromDocument(oldValue, gxlAttr.gxlDocument);
		gxlAttr.children.remove(oldValue);
		oldValue.parent = null;
		// Add newValue to gxlAttr and it's gxlDocument
		gxlAttr.children.add(newValue);
		newValue.parent = gxlAttr;
		if (gxlAttr.gxlDocument != null)
			addToDocument(newValue, gxlAttr.gxlDocument);

		// Notify any listeners
		if (gxlAttr.gxlDocument != null && gxlAttr.gxlDocument.gxlDocumentListeners.size() > 0)
			fireAttrValueModificationEvent(new GXLAttrValueModificationEvent(gxlAttr, newValue));
	}

	/** Notifies listeners of the change. */
	private void fireAttrValueModificationEvent(GXLAttrValueModificationEvent e) {
		for (int i = 0; i < e.getElement().gxlDocument.gxlDocumentListeners.size(); i++)
			((GXLDocumentListener) e.getElement().gxlDocument.gxlDocumentListeners.elementAt(i)).gxlAttrValueChanged(e);
	}

	/** Recursively remove gxlElement and his children from gxlDocument. */
	private void removeFromDocument(GXLElement gxlElement, GXLDocument gxlDocument) {
		// if the element has an ID, remove it from documents idMap
		String oldID = (String) gxlElement.attributes.get(GXL.ID);
		if (oldID != null)
			gxlDocument.idMap.remove(oldID);
		// set the elements gxlDocument to null
		gxlElement.gxlDocument = null;
		// recursively remove children the same way
		for (int i = 0; i < gxlElement.getChildCount(); i++)
			removeFromDocument(gxlElement.getChildAt(i), gxlDocument);
	}

	/** Recursively add gxlElement and his children to gxlDocument. */
	private void addToDocument(GXLElement gxlElement, GXLDocument gxlDocument) {
		// if the element has an ID, add it to documents idMap
		String newID = (String) gxlElement.attributes.get(GXL.ID);
		if (newID != null)
			gxlDocument.idMap.put(newID, gxlElement);
		// set the elements gxlDocument
		gxlElement.gxlDocument = gxlDocument;
		// recursively add children the same way
		for (int i = 0; i < gxlElement.getChildCount(); i++)
			addToDocument(gxlElement.getChildAt(i), gxlDocument);
	}
}
