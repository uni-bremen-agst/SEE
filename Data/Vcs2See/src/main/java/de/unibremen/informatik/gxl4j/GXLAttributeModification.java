/*
 * @(#)GXLAttributeModification.java	0.9 2003-12-07
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

/** Encapsulates an attribute modification, providing the implementation of it and undo support. */
class GXLAttributeModification extends GXLDocumentModification {
	// The element to change attributes on
	GXLElement gxlElement;
	// The attribute to change
	String attributeName;
	// The old attribute value
	String oldValue;
	// The new attribute value
	String newValue;

	//
	// CONSTRUCTORS
	//

	/** Creates a new GXLAttributeModification. 
	 *	@param gxlElement The element to change attributes on.
	 *	@param attributeName The attribute to change.
	 *	@param oldValue The attribute value before the change.
	 *	@param newValue The attribute value to change to.
	 */
	GXLAttributeModification(GXLElement gxlElement, String attributeName, String oldValue, String newValue) {
		this.gxlElement = gxlElement;
		this.attributeName = attributeName;
		this.oldValue = oldValue;
		this.newValue = newValue;
	}

	//
	// PUBLIC METHODS
	//

	/** Undos the attribite modification. */
	public void undo() {
		super.undo();
		handle(newValue, oldValue);
	}

	/** Redos the attribute modification. */
	public void redo() {
		super.redo();
		handle(oldValue, newValue);
	}

	//
	// PACKAGE METHODS
	//

	/** Executes the attribute change. */
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
		// Handle ID and IDREFs somewhat special
		if (gxlElement.gxlDocument != null) {
			if (attributeName.equals(GXL.ID)) {
				// Remove the old id from gxlDocument and add the new
				if (oldValue != null)
					gxlElement.gxlDocument.idMap.remove(oldValue);
				if (newValue != null)
					gxlElement.gxlDocument.idMap.put(newValue, gxlElement);
				// If we're a GraphElement, we must update any elements referencing us
				if (gxlElement instanceof GXLGraphElement) {
					GXLGraphElement graphElement = (GXLGraphElement) gxlElement;
					for (int i = 0; i < graphElement.getConnectionCount(); i++) {
						GXLLocalConnectionTentacle tentacle = graphElement.getConnectionAt(i);
						if (tentacle instanceof GXLRelend)
							((GXLRelend) tentacle).attributes.put(GXL.TARGET, newValue);
						else {
							GXLEdge edge = (GXLEdge) tentacle.getLocalConnection();
							boolean from = edge.fromTentacle == tentacle;
							edge.attributes.put(from ? GXL.FROM : GXL.TO, newValue);
						}
					}
				}
			}
			// If we're changing the destination of an edge, we must tell the old GraphElement that he's not being referenced anymore
			// We'll do this by removing the referencing of the connection prior to the change
			// and adding them again (after the change) with the new element
			else if (attributeName.equals(GXL.TO) || attributeName.equals(GXL.FROM) || attributeName.equals(GXL.TARGET)) {
				GXLLocalConnectionTentacle tentacle = 
					gxlElement instanceof GXLRelend ? 
						(GXLLocalConnectionTentacle) gxlElement : 
						attributeName.equals(GXL.FROM) ? 
							(GXLLocalConnectionTentacle) ((GXLEdge) gxlElement).fromTentacle : 
							(GXLLocalConnectionTentacle) ((GXLEdge) gxlElement).toTentacle;
				// Remove referencing
				if (! tentacle.isDangling()) {
					tentacle.getTarget().connections.remove(tentacle);
					gxlElement.gxlDocument.danglingTentacles.add(tentacle);
				}
			}
		}

		// Change the attribute
		if (newValue != null)
			gxlElement.attributes.put(attributeName, newValue);
		else
			gxlElement.attributes.remove(attributeName);

		// If we removed references from a changed edge before, we'll add them again here after the change
		if (gxlElement.gxlDocument != null) {
			if (attributeName.equals(GXL.TO) || attributeName.equals(GXL.FROM) || attributeName.equals(GXL.TARGET)) {
				GXLLocalConnectionTentacle tentacle = 
					gxlElement instanceof GXLRelend ? 
						(GXLLocalConnectionTentacle) gxlElement : 
						attributeName.equals(GXL.FROM) ? 
							(GXLLocalConnectionTentacle) ((GXLEdge) gxlElement).fromTentacle : 
							(GXLLocalConnectionTentacle) ((GXLEdge) gxlElement).toTentacle;
				// Add referencing
				if (! tentacle.isDangling()) {
					gxlElement.gxlDocument.danglingTentacles.remove(tentacle);
					insertReferenceOrderly(tentacle.getTarget(), tentacle);
				}
			}
		}
		// If a order attribute was changed, update connections/tentacles vector
		if (attributeName.equals(GXL.FROMORDER) || attributeName.equals(GXL.TOORDER) || attributeName.equals(GXL.ENDORDER)) {
			GXLLocalConnectionTentacle tentacle = 
				gxlElement instanceof GXLRelend ? 
					(GXLLocalConnectionTentacle) gxlElement : 
					attributeName.equals(GXL.FROMORDER) ? 
						(GXLLocalConnectionTentacle) ((GXLEdge) gxlElement).fromTentacle : 
						(GXLLocalConnectionTentacle) ((GXLEdge) gxlElement).toTentacle;
			// Update referencing (order actually)
			if (! tentacle.isDangling()) {
				tentacle.getTarget().connections.remove(tentacle);
				insertReferenceOrderly(tentacle.getTarget(), tentacle);
			}
		}
		else if (attributeName.equals(GXL.STARTORDER)) {
			GXLRelend relend = (GXLRelend) gxlElement;
			GXLRel rel = (GXLRel) relend.getParent();
			if (rel != null) {
				rel.tentacles.remove(relend);
				insertRelendOrderly(rel, relend);
			}
		}

		// Notify any listeners
		if (gxlElement.gxlDocument != null && gxlElement.gxlDocument.gxlDocumentListeners.size() > 0)
			fireAttributeModificationEvent(new GXLAttributeModificationEvent(gxlElement, attributeName, newValue));
	}

	/** Notifies listeners of the change. 
	 *	@param e The event to fire to the listeners.
	 */
	private void fireAttributeModificationEvent(GXLAttributeModificationEvent e) {
		for (int i = 0; i < gxlElement.gxlDocument.gxlDocumentListeners.size(); i++)
			((GXLDocumentListener) gxlElement.gxlDocument.gxlDocumentListeners.elementAt(i)).gxlAttributeChanged(e);
	}
}
