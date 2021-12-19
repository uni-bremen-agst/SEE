/*
 * @(#)GXLTreeModification.java	0.9 2003-09-25
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

/** Encapsulates an tree modification, providing the implementation of it and undo support. */
class GXLTreeModification extends GXLDocumentModification {
	GXLElement child;
	GXLElement oldParent;
	GXLElement newParent;
	int oldIndex;
	int newIndex;

	//
	// CONSTRUCTORS
	//

	/** Creates a new TreeModification. Note that either oldParent or newParent MUST be null (no moves allowed) */
	GXLTreeModification(GXLElement child, GXLElement oldParent, GXLElement newParent, int oldIndex, int newIndex) {
		this.child = child;
		this.oldParent = oldParent;
		this.newParent = newParent;
		this.oldIndex = oldIndex;
		this.newIndex = newIndex;
		reorderIndex();
	}

	//
	// PUBLIC METHODS
	//

	/** Undoes the change. */
	public void undo() {
		super.undo();
		handle(newParent, oldParent, newIndex, oldIndex);
	}

	/** Redoes the change. */
	public void redo() {
		super.redo();
		handle(oldParent, newParent, oldIndex, newIndex);
	}

	//
	// PACKAGE METHODS
	//

	/** Executes the change. */
	void execute() {
		handle(oldParent, newParent, oldIndex, newIndex);
		// Find the gxl document and notify it
		if (newParent != null && newParent.gxlDocument != null)
			newParent.gxlDocument.postEdit(this);
		else if (oldParent != null && oldParent.gxlDocument != null)
			oldParent.gxlDocument.postEdit(this);
	}

	//
	// PRIVATE METHODS
	//

	/** Does the actual work. */
	private void handle(GXLElement oldParent, GXLElement newParent, int oldIndex, int newIndex) {
		// Note that some of this code might be obsolete now that we don't allow moves
		// Start by removing ourselves from old parent
		if (oldParent != null) {
			if (oldParent.gxlDocument != null) {
				removeReferences(child);
				removeFromDocument(child, oldParent.gxlDocument);
			}
			// Might need to update rel.tentacles
			if (child instanceof GXLRelend)
				((GXLRel) oldParent).tentacles.remove(child);
			// Do the removing
			oldParent.children.remove(child);
		}
		// Insert into new parent
		child.parent = newParent;
		if (newParent != null) {
			// Insert it
			newParent.children.insertElementAt(child, newIndex);
			// Might need to update rel.tentacles
			if (child instanceof GXLRelend)
				insertRelendOrderly((GXLRel) newParent, (GXLRelend) child);
			// Check references and so
			if (newParent.gxlDocument != null) {
				addToDocument(child, newParent.gxlDocument);
				// Check if any danglingTentacles has been remedied
				for (int i = newParent.gxlDocument.getDanglingTentacleCount()-1; i >= 0; i--) {
					GXLLocalConnectionTentacle tentacle = newParent.gxlDocument.getDanglingTentacleAt(i);
					if (!tentacle.isDangling())
						newParent.gxlDocument.danglingTentacles.remove(tentacle);
				}
				// Add new references from introduced edges
				addReferences(child);
			}
		}

		// Notify any listeners
		if (oldParent != null && oldParent.gxlDocument != null && oldParent.gxlDocument.gxlDocumentListeners.size() > 0)
			fireRemovalEvent(new GXLTreeModificationEvent(oldParent, child));
		else if (newParent != null && newParent.gxlDocument != null && newParent.gxlDocument.gxlDocumentListeners.size() > 0)
			fireInsertionEvent(new GXLTreeModificationEvent(newParent, child));
	}

	/** Notifies listeners of the change. */
	private void fireRemovalEvent(GXLTreeModificationEvent e) {
		for (int i = 0; i < e.getParent().gxlDocument.gxlDocumentListeners.size(); i++)
			((GXLDocumentListener) e.getParent().gxlDocument.gxlDocumentListeners.elementAt(i)).gxlElementRemoved(e);
	}

	/** Notifies listeners of the change. */
	private void fireInsertionEvent(GXLTreeModificationEvent e) {
		for (int i = 0; i < e.getParent().gxlDocument.gxlDocumentListeners.size(); i++)
			((GXLDocumentListener) e.getParent().gxlDocument.gxlDocumentListeners.elementAt(i)).gxlElementInserted(e);
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

	/** Recursively looks for GraphElements and edges to update their references. */
	private void addReferences(GXLElement gxlElement) {
		// Check for LocalConnections and update their references
		if (gxlElement instanceof GXLLocalConnection) {
			GXLLocalConnection localConnection = (GXLLocalConnection) gxlElement;
			for (int i = 0; i < localConnection.getTentacleCount(); i++) {
				GXLLocalConnectionTentacle tentacle = localConnection.getTentacleAt(i);
				if (tentacle.isDangling())
					gxlElement.gxlDocument.danglingTentacles.add(tentacle);
				else
					insertReferenceOrderly(tentacle.getTarget(), tentacle);
			}
		}
		// Recurse children
		for (int i = 0; i < gxlElement.getChildCount(); i++)
			addReferences(gxlElement.getChildAt(i));
	}

	/** Recursively looks for GraphElements and edges to remove their references prior to removal from gxlDocument. */
	private void removeReferences(GXLElement gxlElement) {
		// Check for LocalConnections to remove references from
		if (gxlElement instanceof GXLLocalConnection) {
			GXLLocalConnection localConnection = (GXLLocalConnection) gxlElement;
			// Remove all connections from GraphElements
			// Remove any dangling tentacles from the document (we're about to be removed from it)
			for (int i = 0; i < localConnection.getTentacleCount(); i++) {
				GXLLocalConnectionTentacle tentacle = localConnection.getTentacleAt(i);
				if (tentacle.isDangling())
					gxlElement.gxlDocument.danglingTentacles.remove(tentacle);
				else
					tentacle.getTarget().connections.remove(tentacle);
			}
		}
		// Check for GraphElements to remove references to
		if (gxlElement instanceof GXLGraphElement) {
			GXLGraphElement graphElement = (GXLGraphElement) gxlElement;
			// Remove all referencing from this graphElement (sending all referencing edges to the danglingConnections list)
			while (graphElement.getConnectionCount() > 0) {
				gxlElement.gxlDocument.danglingTentacles.add(graphElement.getConnectionAt(0));
				graphElement.connections.removeElementAt(0);
			}
		}
		// Recurse children
		for (int i = 0; i < gxlElement.getChildCount(); i++)
			removeReferences(gxlElement.getChildAt(i));
	}
	
	/** Reorders the new index if it brakes the imposed child ordering. */
	private void reorderIndex() {
		if (newParent != null) {
			// Get the ordering for the parent
			String[][] order = GXL.CHILD_ORDER[newParent.getElementIndex()];
			// No ordering unless more than one element set
			if (order.length > 1) {
				// Which set does our child belong to?
				int set = -1;
				for (int i = 0; set == -1 && i < order.length; i++)
					if (GXL.contains(order[i], child.getElementName()))
						set = i;
				// Make sure index isn't too small
				if (newIndex < newParent.getChildCount()) {
					// Get the element at insertion point (should be of same or higher set than child)
					GXLElement current = (GXLElement) newParent.getChildAt(newIndex);
					int minSet = 0;
					// As long as minSet is less than set and index is in the array, examine if we must increase index
					while (minSet < set && newIndex < newParent.getChildCount()) {
						// Adjust minSet to current elements set
						while (minSet < set && !GXL.contains(order[minSet], current.getElementName()))
							minSet++;
						// If minSet still is less than childs set, increase index
						if (minSet < set) {
							newIndex++;
							current = (GXLElement) newParent.getChildAt(newIndex);
						}
					}
				}
				// Make sure index isn't too big
				if (newIndex > 0) {
					// Get the element below the insertion point (should be of same or lower set than child)
					GXLElement current = (GXLElement) newParent.getChildAt(newIndex-1);
					int maxSet = order.length-1;
					// As long as maxSet is higher than set and index is in the array, examine if we must decrease index
					while (maxSet > set && newIndex > 0) {
						// Adjust maxSet to current elements set
						while (maxSet > set && !GXL.contains(order[maxSet], current.getElementName()))
							maxSet--;
						// If maxSet still is higher than childs set, decrease index
						if (maxSet > set) {
							newIndex--;
							if (newIndex > 0)
								current = (GXLElement) newParent.getChildAt(newIndex-1);
						}
					}
				}
			}
		}
	}
}
