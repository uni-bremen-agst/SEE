/*
 * @(#)GXLValidator.java	0.8 2003-10-05
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

import java.net.URI;
import java.net.URISyntaxException;
import java.util.Hashtable;
import java.util.Iterator;
import java.util.Map;
import java.util.Set;
import java.util.TreeSet;
import java.util.Vector;

/** Validates GXLDocument and GXLElements for conformance to DTD Requirements and Additional Constraints. 
 */
class GXLValidator {
	//
	// PACKAGE METHODS
	//

	//
	// INSERTION
	//

	/** Should be called before the insertion of child, throws if the insertion
	 *	will cause the GXLDocument to be invalid (other than an invalid reference). */
	static void validateInsertion(GXLDocument gxlDocument, GXLElement parent, GXLElement child) {
		// VALIDATIONS FOR THIS CHILD ONLY (NO RECURSION)
		// Validate that child isn't null
		if (child == null)
			throw new NullPointerException("Cannot add a null GXLElement");
		// Validate (for clean usage) that the child doesn't have a parent (no moves allowed, remove first)
		if (child.getParent() != null)
			throw new IllegalArgumentException("This child already has a parent");
		// Validate INVALID_CHILD_TYPE
		if (! GXL.contains(GXL.ALLOWED_CHILDREN[parent.getElementIndex()], child.getElementName()))
			throw new GXLValidationException(GXLValidationException.INVALID_CHILD_TYPE);
		// Validate DUPLICATE_TYPE
		if (child instanceof GXLType)
			if (parent.getChildCount() > 0 && (parent.getChildAt(0) instanceof GXLType))
				throw new GXLValidationException(GXLValidationException.DUPLICATE_TYPE);
		// Validate DUPLICATE_VALUE
		if (parent instanceof GXLAttr)
			if (child instanceof GXLValue)
				if (((GXLAttr) parent).getValue() != null)
					throw new GXLValidationException(GXLValidationException.DUPLICATE_VALUE);
		// Validate REQUIRED_VALUE_MISSING
		if (child instanceof GXLAttr)
			if (((GXLAttr) child).getValue() == null)
				throw new GXLValidationException(GXLValidationException.REQUIRED_VALUE_MISSING);
		// Validate REQUIRED_ATTRIBUTE_MISSING
		for (int i = 0; i < GXL.REQUIRED_ATTRIBUTES[child.getElementIndex()].length; i++)
			if (GXL.REQUIRED_ATTRIBUTES[child.getElementIndex()][i])
				if (! child.attributes.containsKey(GXL.ALLOWED_ATTRIBUTES[child.getElementIndex()][i]))
					throw new GXLValidationException(GXLValidationException.REQUIRED_ATTRIBUTE_MISSING);
		// Validate DUPLICATE_NAME
		if (child instanceof GXLAttr) {
			String name = ((GXLAttr) child).getName();
			GXLAttributedElement attrParent = (GXLAttributedElement) parent;
			for (int i = 0; i < attrParent.getAttrCount(); i++)
				if (name.equals(attrParent.getAttrAt(i).getName()) && child != attrParent.getAttrAt(i))
					throw new GXLValidationException(GXLValidationException.DUPLICATE_NAME);
		}
		// Validate PROHIBITED_HYPERGRAPH
		if (child instanceof GXLRel) {
			if (!((GXLGraph) parent).getAllowsHyperGraphs())
				throw new GXLValidationException(GXLValidationException.PROHIBITED_HYPERGRAPH);
		}
		// Validate PROHIBITED_DIRECTION_OVERRIDE
		if (child instanceof GXLLocalConnection) {
			String edgemode = ((GXLGraph) parent).getEdgeMode();
			if (edgemode.equals(GXL.DIRECTED) || edgemode.equals(GXL.UNDIRECTED))
				if (((GXLLocalConnection) child).hasKnownDirection())
					if (edgemode.equals(GXL.DIRECTED) != ((GXLLocalConnection) child).isDirected())
						throw new GXLValidationException(GXLValidationException.PROHIBITED_DIRECTION_OVERRIDE);
		}
		// Validate DUPLICATE_ORDER
		if (child instanceof GXLRelend) {
			GXLRelend relend = (GXLRelend) child;
			GXLRel rel = (GXLRel) parent;
			boolean isOrdered = relend.hasRelIncidenceOrder();
			int order = relend.getRelIncidenceOrder();
			for (int i = 0; i < rel.getRelendCount(); i++) {
				boolean otherIsOrdered = rel.getRelendAt(i).hasRelIncidenceOrder();
				if (isOrdered && otherIsOrdered && order == rel.getRelendAt(i).getRelIncidenceOrder())
					throw new GXLValidationException(GXLValidationException.DUPLICATE_ORDER);
			}
		}
		// Validate INVALID_URI
		if ((child instanceof GXLType) || (child instanceof GXLLocator)) {
			try { new URI(child.getAttribute(GXL.XLINK_HREF)); }
			catch (URISyntaxException use) {
				throw new GXLValidationException(GXLValidationException.INVALID_URI);
			}
		}
		// Validate DUPLICATE_VALUE_TYPE
		if (parent instanceof GXLCompositeValue) {
			if (!(parent instanceof GXLTup)) {
				GXLCompositeValue comp = (GXLCompositeValue) parent;
				for (int i = 0; i < comp.getValueCount(); i++)
					if (child.getElementIndex() != comp.getValueAt(i).getElementIndex())
						throw new GXLValidationException(GXLValidationException.DUPLICATE_VALUE_TYPE);
			}
		}


		// VALIDATIONS NEEDING RECURSION
		// Create a temporary storage to hold new IDs
		Hashtable newIDMap = new Hashtable();
		// Create a temporary storage to hold new edges (will store tentacles)
		Vector newTentacles = new Vector();
		// Route the call to our recursive counterpart
		validateInsertion(gxlDocument, parent, child, newIDMap, newTentacles);

		// VALIDATIONS NEEDED INFORMATION FROM THE RECURSION
		// Validate INVALID_CONNECTION
		// Validate INVALID_INTERGRAPH_CONNECTION
		if (gxlDocument != null) {
			// Create a map from GraphElement to (a vector of) connecting tentacles (for later ordering validation)
			Map geConnections = new Hashtable();
			// Add danglingTentacles (as potential new edges)
			newTentacles.addAll(gxlDocument.danglingTentacles);

			for (int i = 0; i < newTentacles.size(); i++) {
				GXLLocalConnectionTentacle tentacle = (GXLLocalConnectionTentacle) newTentacles.elementAt(i);
				String targetID = tentacle.getTargetID();
				GXLElement connectedElement = newIDMap.containsKey(targetID) ? (GXLElement) newIDMap.get(targetID) : gxlDocument.getElement(targetID);
				if (connectedElement != null) {
					// Make sure it points to a GraphElement
					if (!(connectedElement instanceof GXLGraphElement))
						throw new GXLValidationException(GXLValidationException.INVALID_CONNECTION);
					// Must have the same top graph as edge
					GXLGraph edgeTopGraph = getTopGraph(tentacle.getLocalConnection(), parent, child);
					GXLGraph elementTopGraph = getTopGraph(connectedElement, parent, child);
					if (edgeTopGraph != null && elementTopGraph != null && edgeTopGraph != elementTopGraph)
						throw new GXLValidationException(GXLValidationException.INVALID_INTERGRAPH_CONNECTION);
					// Add this GraphElement to geConnections for later validation
					if (geConnections.containsKey(connectedElement))
						((Vector) geConnections.get(connectedElement)).add(tentacle);
					else {
						Vector v = new Vector();
						v.add(tentacle);
						geConnections.put(connectedElement, v);
					}
				}
			}

			// Validate DUPLICATE_ORDER
			Iterator geIter = geConnections.keySet().iterator();
			while (geIter.hasNext()) {
				GXLGraphElement graphElement = (GXLGraphElement) geIter.next();
				Vector tentacles = (Vector) geConnections.get(graphElement);
				// Add non-dangling tentacles to our vector as well
				for (int i = 0; i < graphElement.getConnectionCount(); i++)
					tentacles.add(graphElement.getConnectionAt(i));
				// Remove any duplicate connection entries (so that they don't count twice, causing DUPLICATE_ORDER)
				for (int i = 0; i < tentacles.size(); i++)
					for (int j = tentacles.size()-1; j > i; j--)
						if (tentacles.elementAt(i) == tentacles.elementAt(j))
							tentacles.removeElementAt(j);
				// Allright, tentacles should now contain all connections to graphElement
				Set orderSet = new TreeSet();
				String id = graphElement.getID();
				for (int i = 0; i < tentacles.size(); i++) {
					GXLLocalConnectionTentacle tentacle = (GXLLocalConnectionTentacle) tentacles.elementAt(i);
					if (tentacle.hasTargetIncidenceOrder()) {
						Integer order = new Integer(tentacle.getTargetIncidenceOrder());
						if (orderSet.contains(order))
							throw new GXLValidationException(GXLValidationException.DUPLICATE_ORDER);
						orderSet.add(order);
					}
				}
			}
		}
	}

	/** Returns the top graph of the specified element. */
	private static GXLGraph getTopGraph(GXLElement gxlElement) { return getTopGraph(gxlElement, null, null); }
	private static GXLGraph getTopGraph(GXLElement gxlElement, GXLElement iparent, GXLElement ichild) {
		GXLElement result = gxlElement;
		GXLElement parent = result == null ? null : result.getParent() != null ? result.getParent() : result == ichild ? iparent : null;
		while (result != null && !(parent instanceof GXLGXL)) {
			result = parent;
			parent = result == null ? null : result.getParent() != null ? result.getParent() : result == ichild ? iparent : null;
		}
		return (GXLGraph) result;
	}

	// THE NEEDED RECURSIVE VALIDATION
	private static void validateInsertion(GXLDocument gxlDocument, GXLElement parent, GXLElement child, Map newIDMap, Vector newTentacles) {
		if (gxlDocument != null) {
			// Validate DUPLICATE_ID
			{
				String id = (String) child.attributes.get(GXL.ID);
				if (id != null && (gxlDocument.idMap.containsKey(id) || newIDMap.containsKey(id)))
					throw new GXLValidationException(GXLValidationException.DUPLICATE_ID);
				if (id != null)
					newIDMap.put(id, child);
			}
			// Validate INVALID_CONNECTION
			// ie that a formerly dangling edge will start to reference a freshly introduced non-GraphElement
			if (!(child instanceof GXLGraphElement)) {
				String id = (String) child.attributes.get(GXL.ID);
				if (id != null) {
					for (int i = 0; i < gxlDocument.getDanglingTentacleCount(); i++)
						if (id.equals(gxlDocument.getDanglingTentacleAt(i).getTargetID()))
							throw new GXLValidationException(GXLValidationException.INVALID_CONNECTION);
				}
			}
		}
		// Validate REQUIRED_RELEND_DIRECTION_MISSING
		// Note: this requires recursion since Rel.isDirected may be inherited from Rels Graph parent
		if (child instanceof GXLRelend)
			if (((GXLRel) parent).hasKnownDirection())
				if (((GXLRel) parent).isDirected())
					if (((GXLRelend) child).getDirection().equals(GXL.NONE))
						throw new GXLValidationException(GXLValidationException.REQUIRED_RELEND_DIRECTION_MISSING);

		// If this is a edge, enter it into the newTentacles vector for later checkup
		if (child instanceof GXLEdge) {
			GXLEdge edge = (GXLEdge) child;
			newTentacles.add(edge.fromTentacle);
			newTentacles.add(edge.toTentacle);
		}
		if (child instanceof GXLRelend)
			newTentacles.add(child);

		// Recruse children
		for (int i = 0; i < child.getChildCount(); i++)
			validateInsertion(gxlDocument, child, child.getChildAt(i), newIDMap, newTentacles);
	}

	//
	// REMOVAL
	//

	/** Should be called before the removal of child, throws if the removal
	 *	will cause the GXLDocument to be invalid (other than an invalid reference). */
	static void validateRemoval(GXLDocument gxlDocument, GXLElement child) {
		// Validate REQUIRED_VALUE_MISSING
		if (child instanceof GXLValue)
			if (child.getParent() instanceof GXLAttr)
				throw new GXLValidationException(GXLValidationException.REQUIRED_VALUE_MISSING);
	}

	//
	// ATTRIBUTE MODIFICATION
	//

	/** Should be called before the specified attribute modification, throws if the modification
	 *	will cause the GXLDocument to be invalid (other than an invalid reference). */
	static void validateAttributeModification(GXLDocument gxlDocument, GXLElement element, String name, String value) {
		// Get the attributeIndex in the GXL class arrays
		int attributeIndex = GXL.indexOf(GXL.ATTRIBUTES, name);
		int elementAttributeIndex = GXL.indexOf(GXL.ALLOWED_ATTRIBUTES[element.getElementIndex()], name);
		// Validate INVALID_ATTRIBUTE
		if (attributeIndex < 0 || elementAttributeIndex < 0)
			throw new GXLValidationException(GXLValidationException.INVALID_ATTRIBUTE);
		// Validate REQUIRED_ATTRIBUTE_MISSING
		if (value == null && GXL.REQUIRED_ATTRIBUTES[element.getElementIndex()][elementAttributeIndex])
			throw new GXLValidationException(GXLValidationException.REQUIRED_ATTRIBUTE_MISSING);
		// Validate INVALID_ATTRIBUTE_VALUE
		String[] values = GXL.ATTRIBUTE_VALUES[attributeIndex];
		if (value != null && values != null && !GXL.contains(values, value))
			throw new GXLValidationException(GXLValidationException.INVALID_ATTRIBUTE_VALUE);
		// Validate DUPLICATE_ID
		if (name.equals(GXL.ID) && value != null) {
			if (gxlDocument != null) {
				String id = (String) element.attributes.get(GXL.ID);
				if (gxlDocument.idMap.containsKey(value) && (id == null || ! id.equals(value)))
					throw new GXLValidationException(GXLValidationException.DUPLICATE_ID);
			}
		}
		// Validate INVALID_CONNECTION
		if (GXL.ATTRIBUTE_TYPES[attributeIndex] == GXL.TYPE_IDREF) {
			if (gxlDocument != null) {
				GXLElement referencee = (GXLElement) gxlDocument.idMap.get(value);
				if (referencee != null && !(referencee instanceof GXLGraphElement))
					throw new GXLValidationException(GXLValidationException.INVALID_CONNECTION);
			}
		}
		else if (name.equals(GXL.ID) && value != null) {
			if (gxlDocument != null)
				if (!(element instanceof GXLGraphElement))
					for (int i = 0; i < gxlDocument.getDanglingTentacleCount(); i++)
						if (value.equals(gxlDocument.getDanglingTentacleAt(i).getTargetID()))
							throw new GXLValidationException(GXLValidationException.INVALID_CONNECTION);
		}
		// Validate DUPLICATE_NAME
		if (name.equals(GXL.NAME)) {
			if (element.getParent() != null) {
				GXLAttributedElement attrParent = (GXLAttributedElement) element.getParent();
				for (int i = 0; i < attrParent.getAttrCount(); i++)
					if (value.equals(attrParent.getAttrAt(i).getName()) && element != attrParent.getAttrAt(i))
						throw new GXLValidationException(GXLValidationException.DUPLICATE_NAME);
			}
		}
		// Validate PROHIBITED_HYPERGRAPH
		if (name.equals(GXL.HYPERGRAPH))
			if (value.equals(GXL.FALSE))
				for (int i = 0; i < element.getChildCount(); i++)
					if (element.getChildAt(i) instanceof GXLRel)
						throw new GXLValidationException(GXLValidationException.PROHIBITED_HYPERGRAPH);
		// Validate PROHIBITED_DIRECTION_OVERRIDE
		if (name.equals(GXL.EDGEMODE)) {
			if (value == null || value.equals(GXL.DIRECTED) || value.equals(GXL.UNDIRECTED)) {
				boolean graphDirected = value == null || value.equals(GXL.DIRECTED);
				for (int i = 0; i < element.getChildCount(); i++) {
					if (element.getChildAt(i) instanceof GXLLocalConnection) {
						GXLLocalConnection localConnection = (GXLLocalConnection) element.getChildAt(i);
						if (localConnection.getAttribute(GXL.ISDIRECTED) != null && localConnection.isDirected() != graphDirected)
							throw new GXLValidationException(GXLValidationException.PROHIBITED_DIRECTION_OVERRIDE);
					}
				}
			}
		}
		else if (name.equals(GXL.ISDIRECTED)) {
			if (value != null) {
				boolean edgeDirected = value.equals(GXL.TRUE);
				if (element.getParent() != null) {
					String edgemode = element.getParent().getAttribute(GXL.EDGEMODE);
					if (edgemode.equals(GXL.DIRECTED) || edgemode.equals(GXL.UNDIRECTED))
						if (edgeDirected != edgemode.equals(GXL.DIRECTED))
							throw new GXLValidationException(GXLValidationException.PROHIBITED_DIRECTION_OVERRIDE);
				}
			}
		}
		// Validate REQUIRED_RELEND_DIRECTION_MISSING
		if (name.equals(GXL.DIRECTION)) {
			if (element.getParent() != null) {
				GXLRel rel = (GXLRel) element.getParent();
				if (rel.hasKnownDirection() && rel.isDirected())
					if (value == null || value.equals(GXL.NONE))
						throw new GXLValidationException(GXLValidationException.REQUIRED_RELEND_DIRECTION_MISSING);
			}
		}
		else if (name.equals(GXL.ISDIRECTED)) {
			if (element instanceof GXLRel)
				if (value != null && value.equals(GXL.TRUE))
					for (int i = 0; i < element.getChildCount(); i++)
						if (element.getChildAt(i) instanceof GXLRelend)
							if (element.getChildAt(i).getAttribute(GXL.DIRECTION) == null || element.getChildAt(i).getAttribute(GXL.DIRECTION).equals(GXL.NONE))
								throw new GXLValidationException(GXLValidationException.REQUIRED_RELEND_DIRECTION_MISSING);
		}
		else if (name.equals(GXL.EDGEMODE)) {
			boolean graphDirected = value == null || value.equals(GXL.DIRECTED) || value.equals(GXL.DEFAULTDIRECTED);
			if (graphDirected) {
				for (int k = 0; k < element.getChildCount(); k++) {
					if (element.getChildAt(k) instanceof GXLRel) {
						GXLRel rel = (GXLRel) element.getChildAt(k);
						if (rel.getAttribute(GXL.ISDIRECTED) == null || rel.getAttribute(GXL.ISDIRECTED).equals(GXL.TRUE))
							for (int j = 0; j < rel.getRelendCount(); j++)
								if (rel.getRelendAt(j).getAttribute(GXL.DIRECTION) == null || rel.getRelendAt(j).getAttribute(GXL.DIRECTION).equals(GXL.NONE))
									throw new GXLValidationException(GXLValidationException.REQUIRED_RELEND_DIRECTION_MISSING);
					}
				}
			}
		}
		// Validate INVALID_ORDER
		// Validate DUPLICATE_ORDER
		if (name.equals(GXL.FROMORDER) || name.equals(GXL.TOORDER) || name.equals(GXL.STARTORDER) || name.equals(GXL.ENDORDER)) {
			try {
				if (value != null) {
					int order = Integer.parseInt(value);
					if (name.equals(GXL.STARTORDER)) {
						GXLRel rel = (GXLRel) element.getParent();
						if (rel != null) 
							for (int i = 0; i < rel.getRelendCount(); i++)
								if (rel.getRelendAt(i).hasRelIncidenceOrder())
									if (rel.getRelendAt(i).getRelIncidenceOrder() == order && rel.getRelendAt(i) != element)
										throw new GXLValidationException(GXLValidationException.DUPLICATE_ORDER);
					}
					else {
						GXLGraphElement graphElement = 
							name.equals(GXL.FROMORDER) ? ((GXLEdge) element).getSource() :
							name.equals(GXL.TOORDER) ? ((GXLEdge) element).getTarget() :
							((GXLRelend) element).getTarget();
						if (graphElement != null)
							for (int i = 0; i < graphElement.getConnectionCount(); i++)
								if (graphElement.getConnectionAt(i).hasTargetIncidenceOrder())
									if (graphElement.getConnectionAt(i).getTargetIncidenceOrder() == order)
										if (graphElement.getConnectionAt(i) != element && graphElement.getConnectionAt(i).getLocalConnection() != element)
											throw new GXLValidationException(GXLValidationException.DUPLICATE_ORDER);
					}
				}
			}
			catch (NumberFormatException nfe) {
				throw new GXLValidationException(GXLValidationException.INVALID_ORDER);
			}
		}
		// Validate INVALID_URI
		if (name.equals(GXL.XLINK_HREF)) {
			try { new URI(value); }
			catch (URISyntaxException use) {
				throw new GXLValidationException(GXLValidationException.INVALID_URI);
			}
		}
		// Validate INVALID_INTERGRAPH_CONNECTION
		if (gxlDocument != null) {
			if (name.equals(GXL.FROM) || name.equals(GXL.TO) || name.equals(GXL.TARGET)) {
				GXLGraphElement target = (GXLGraphElement) gxlDocument.idMap.get(value);
				if (target != null) {
					GXLGraph edgeTopGraph = getTopGraph(element);
					GXLGraph targetTopGraph = getTopGraph(target);
					if (targetTopGraph != edgeTopGraph)
						throw new GXLValidationException(GXLValidationException.INVALID_INTERGRAPH_CONNECTION);
				}
			}
			else if (name.equals(GXL.ID) && value != null) {
				if (element instanceof GXLGraphElement) {
					GXLGraph topGraph = getTopGraph(element);
					for (int i = 0; i < gxlDocument.getDanglingTentacleCount(); i++)
						if (value.equals(gxlDocument.getDanglingTentacleAt(i).getTargetID()))
							if (topGraph != getTopGraph(gxlDocument.getDanglingTentacleAt(i).getLocalConnection()))
								throw new GXLValidationException(GXLValidationException.INVALID_INTERGRAPH_CONNECTION);
				}
			}
		}
		// Validate INVALID_SELF_REFERENCE
		if (name.equals(GXL.ID)) {
			if (element instanceof GXLLocalConnection) {
				if (value != null) {
					if (element instanceof GXLEdge) {
						if (value.equals(element.getAttribute(GXL.FROM)) || value.equals(element.getAttribute(GXL.TO)))
							throw new GXLValidationException(GXLValidationException.INVALID_SELF_REFERENCE);
					}
					else if (element instanceof GXLRel) {
						GXLRel rel = (GXLRel) element;
						for (int i = 0; i < rel.getRelendCount(); i++)
							if (value.equals(rel.getRelendAt(i).getAttribute(GXL.TARGET)))
								throw new GXLValidationException(GXLValidationException.INVALID_SELF_REFERENCE);
					}
				}
			}
		}
		else if (name.equals(GXL.FROM) || name.equals(GXL.TO) || name.equals(GXL.TARGET)) {
			if (element instanceof GXLEdge) {
				if (value.equals(element.getAttribute(GXL.ID)))
					throw new GXLValidationException(GXLValidationException.INVALID_SELF_REFERENCE);
			}
			else if (element instanceof GXLRelend)
				if (element.getParent() != null)
					if (value.equals(element.getParent().getAttribute(GXL.ID)))
						throw new GXLValidationException(GXLValidationException.INVALID_SELF_REFERENCE);
		}
	}

	//
	// VALUE MODIFICATION
	//

	/** Should be called before the specified value modification, throws if the modification
	 *	will cause the GXLDocument to be invalid . */
	static void validateValueModification(GXLDocument gxlDocument, GXLAtomicValue gxlElement, String value) {
		// Check for null
		if (value == null)
			throw new NullPointerException();
		// Validate INVALID_VALUE
		try {
			if (gxlElement instanceof GXLInt)
				new Integer(value);
			else if (gxlElement instanceof GXLFloat)
				new Float(value);
		}
		catch (NumberFormatException nfe) {
			throw new GXLValidationException(GXLValidationException.INVALID_VALUE);
		}
		if (gxlElement instanceof GXLBool)
			if (!value.equals(GXL.TRUE) && !value.equals(GXL.FALSE))
				throw new GXLValidationException(GXLValidationException.INVALID_VALUE);
	}

	//
	// ATTR VALUE MODIFICATION
	//

	/** Should be called before the specified value modification, throws if the modification
	 *	will cause the GXLDocument to be invalid . */
	static void validateAttrValueModification(GXLDocument gxlDocument, GXLAttr gxlAttr, GXLValue gxlValue) {
		// Check for null
		if (gxlValue == null)
			throw new NullPointerException();
		// Validate (for clean usage) that the child doesn't have a parent (no moves allowed, remove first)
		if (gxlValue.getParent() != null)
			throw new IllegalArgumentException("This child already has a parent");
	}
}
