/*
 * @(#)GXLDocumentModification.java	0.9 2003-12-08
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
import javax.swing.undo.UndoableEdit;

/** Superclass of all document modifications. */
abstract class GXLDocumentModification extends AbstractUndoableEdit {
	//
	// PACKAGE METHODS
	//

	/** Executes this modification. */
	abstract void execute();

	/** Inserts a GraphElement reference, with proper ordering if stated. */
	void insertReferenceOrderly(GXLGraphElement graphElement, GXLLocalConnectionTentacle tentacle) {
		if (! tentacle.hasTargetIncidenceOrder())
			graphElement.connections.add(tentacle);
		else {
			// Get the order of our tentacle
			int order = tentacle.getTargetIncidenceOrder();
			// Find the insertion spot
			int index = -1;
			for (int i = 0; index == -1 && i < graphElement.getConnectionCount(); i++)
				if (!graphElement.getConnectionAt(i).hasTargetIncidenceOrder() || order < graphElement.getConnectionAt(i).getTargetIncidenceOrder())
					index = i;
			// Insert the tentacle
			if (index != -1)
				graphElement.connections.insertElementAt(tentacle, index);
			else
				graphElement.connections.add(tentacle);
		}
	}

	/** Inserts a Relend tentacle into Rel.tentacles, with proper ordering if stated. */
	void insertRelendOrderly(GXLRel rel, GXLRelend relend) {
		if (! relend.hasRelIncidenceOrder())
			rel.tentacles.add(relend);
		else {
			// Get the order of our tentacle
			int order = relend.getRelIncidenceOrder();
			// Find the insertion spot
			int index = -1;
			for (int i = 0; index == -1 && i < rel.getTentacleCount(); i++)
				if (!((GXLRelend) rel.getTentacleAt(i)).hasRelIncidenceOrder() || order < ((GXLRelend) rel.getTentacleAt(i)).getRelIncidenceOrder())
					index = i;
			// Insert the tentacle
			if (index != -1)
				rel.tentacles.insertElementAt(relend, index);
			else
				rel.tentacles.add(relend);
		}
	}
}