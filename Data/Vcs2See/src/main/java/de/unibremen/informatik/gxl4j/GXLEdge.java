/*
 * @(#)GXLEdge.java	0.9 2003-12-08
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

import org.w3c.dom.Element;

/** Represents the GXL <code>edge</code> element. 
 *	<p>
 *	The <code>edge</code> element represents a binary edge between two graph elements.
 *	<p>
 *	<b>Children</b><br>
 *	<code>GXLType</code> - see {@link GXLType}.<br>
 *	<code>GXLAttr</code> - Attributes describing this edge.<br>
 *	<code>GXLGraph</code> - <code>edge</code> elements may contain <code>graph</code> elements, creating a hierarchical graph.<br>
 *	<p>
 *	<b>Attributes</b><br>
 *	<code>id</code> - Optional id of this <code>edge</code> element.<br>
 *	<code>from</code> - Required id referencing the source graph element of this <code>edge</code>.<br>
 *	<code>to</code> - Required id referencing the target graph element of this <code>edge</code>.<br>
 *	<code>fromorder</code> - Optional integer that describes an ordering of the incident tentacles of the source element.<br>
 *	<code>toorder</code> - Optional integer that describes an ordering of the incident tentacles of the target element.<br>
 *	<code>isdirected</code> - Optional boolean defining if this edge is directed. Note that this attribute overrides
 *	the <code>edgemode</code> attribute of the wrapping <code>graph</code> element (see {@link GXLGraph}).<br>
 *
 *	@see GXLLocalConnection
 *	@see GXLRel
 *	@see GXLGraphElement
 */
public class GXLEdge extends GXLLocalConnection {
	GXLEdgeTentacle fromTentacle, toTentacle;

	//
	// CONSTRUCTORS
	//

	/** Creates a new <code>GXLEdge</code> element with the specified source and target. 
	 *	Note that only the ids of the graph elements are stored and that changing these ids
	 *	prior to adding the elements to a <code>GXLDocument</code> will result in this being
	 *	a dangling edge (since it can't find the source or target). Once both the edge, source
	 *	and target is part of a document, any id changes will ripple through to the edge.
	 *	Note also, from the same reason, that both the source and the target MUST have their ids set.
	 *	@param source The graph element that is the source of this edge.
	 *	@param target The graph element that is the target of this edge.
	 */
	public GXLEdge(GXLGraphElement source, GXLGraphElement target) {
		this(source.getID(), target.getID());
	}

	/** Creates a new <code>GXLEdge</code> element with the specified source and target. 
	 *	@param sourceID The id of the graph element that is the source of this edge.
	 *	@param targetID The id of the graph element that is the target of this edge.
	 */
	public GXLEdge(String sourceID, String targetID) {
		super(GXL.EDGE);
		fromTentacle = new GXLEdgeTentacle(this, GXL.FROM);
		toTentacle = new GXLEdgeTentacle(this, GXL.TO);
		setAttribute(GXL.FROM, sourceID);
		setAttribute(GXL.TO, targetID);
		tentacles.add(fromTentacle);
		tentacles.add(toTentacle);
	}

	/** Creates a new GXLEdge element (used when reading a document). */
	GXLEdge(Element element) {
		super(GXL.EDGE, element);
		fromTentacle = new GXLEdgeTentacle(this, GXL.FROM);
		toTentacle = new GXLEdgeTentacle(this, GXL.TO);
		tentacles.add(fromTentacle);
		tentacles.add(toTentacle);
		createChildren(element);
	}

	//
	// ATTRIBUTE ACCESSORS
	//

	/** Returns the graph element that is the source of this edge, or null if this is a dangling edge.
	 *	Note that all edges that is not a part of a <code>GXLDocument</code> is automatically dangling,
	 *	otherwise a null result of this method means that the <code>GXLDocument</code> that this edge belongs
	 *	to doesn't contain an element with the id specified by this edge as it's source.
	 *	@return The graph element that is the source of this edge, or null if this is a dangling edge.
	 *	@see #getSourceID()
	 */
	public GXLGraphElement getSource() {
		return gxlDocument != null ? (GXLGraphElement) gxlDocument.getElement(getAttribute(GXL.FROM)) : null;
	}

	/** Sets the source of this edge. Note that source MUST have it's id set.
	 *	This method is a shorthand for <code>setAttribute(GXL.FROM, (String) source.getAttribute(GXL.ID))</code>
	 *	@param source the graph element that is to be the source of this edge.
	 *	@throws GXLValidationException If this action would result in an invalid GXL document.
	 *	@see #setSourceID(String id)
	 */
	public void setSource(GXLGraphElement source) {
		setAttribute(GXL.FROM, (String) source.getAttribute(GXL.ID));
	}

	/** Returns the graph element that is the target of this edge, or null if this is a dangling edge.
	 *	Note that all edges that is not a part of a <code>GXLDocument</code> is automatically dangling,
	 *	otherwise a null result of this method means that the <code>GXLDocument</code> that this edge belongs
	 *	to doesn't contain an element with the id specified by this edge as it's target.
	 *	@return The graph element that is the target of this edge, or null if this is a dangling edge.
	 *	@see #getTargetID()
	 */
	public GXLGraphElement getTarget() {
		return gxlDocument != null ? (GXLGraphElement) gxlDocument.idMap.get(attributes.get(GXL.TO)) : null;
	}

	/** Sets the target of this edge. Note that target MUST have it's id set.
	 *	This method is a shorthand for <code>setAttribute(GXL.TO, (String) target.getAttribute(GXL.ID))</code>
	 *	@param target the graph element that is to be the target of this edge.
	 *	@throws GXLValidationException If this action would result in an invalid GXL document.
	 *	@see #setTargetID(String id)
	 */
	public void setTarget(GXLGraphElement target) {
		setAttribute(GXL.TO, (String) target.getAttribute(GXL.ID));
	}

	/** Returns the id of the graph element that is the source of this edge.
	 *	@return The id of the graph element that is the source of this edge.
	 *	@see #getSource()
	 */
	public String getSourceID() {
		return getAttribute(GXL.FROM);
	}

	/** Sets the source of this edge.
	 *	This method is a shorthand for <code>setAttribute(GXL.FROM, id)</code>.
	 *	@param id The id of the graph element that is to be the source of this edge.
	 *	@throws GXLValidationException If this action would result in an invalid GXL document.
	 *	@see #setSource(GXLGraphElement source)
	 */
	public void setSourceID(String id) {
		setAttribute(GXL.FROM, id);
	}

	/** Returns the id of the graph element that is the target of this edge.
	 *	@return The id of the graph element that is the target of this edge.
	 *	@see #getTarget()
	 */
	public String getTargetID() {
		return getAttribute(GXL.TO);
	}

	/** Sets the target of this edge.
	 *	This method is a shorthand for <code>setAttribute(GXL.TO, id)</code>.
	 *	@param id The id of the graph element that is to be the target of this edge.
	 *	@throws GXLValidationException If this action would result in an invalid GXL document.
	 *	@see #setTarget(GXLGraphElement target)
	 */
	public void setTargetID(String id) {
		setAttribute(GXL.TO, id);
	}

	/** Returns the tentacle connecting this edge to it's source. 
	 *	@return The tentacle connecting this edge to it's source. 
	 */
	public GXLLocalConnectionTentacle getSourceTentacle() {
		return fromTentacle;
	}

	/** Returns the tentacle connecting this edge to it's target. 
	 *	@return The tentacle connecting this edge to it's target. 
	 */
	public GXLLocalConnectionTentacle getTargetTentacle() {
		return toTentacle;
	}

	/** Returns whether this edge's source tentacle is ordered relative to the source. 
	 *	This method is a shorthand for <code>getAttribute(GXL.FROMORDER) != null</code>.
	 *	@return Whether this edge's source tentacle is ordered relative to the source. 
	 */
	public boolean hasSourceIncidenceOrder() {
		return getAttribute(GXL.FROMORDER) != null;
	}

	/** Returns the integer used to order this edge's source tentacle relative to the source's other
	 *	incident tentacles. Note that the value returned from this method is only valid if the method
	 *	<code>hasSourceIncidenceOrder()</code> returns true.
	 *	This method is mostly a shorthand for <code>getAttribute(GXL.FROMORDER)</code> albeit the <code>String</code>
	 *	is converted to an integer first.
	 *	@return The integer used to order this edge's source tentacle relative to the source's other
	 *	incident tentacles.
	 */
	public int getSourceIncidenceOrder() {
		String order = getAttribute(GXL.FROMORDER);
		return order != null ? new Integer(order).intValue() : -1;
	}

	/** Sets the integer used to order this edge's source tentacle relative to the source's other
	 *	incident tentacles. Note that the value of order must be unique to all the tentacles incident to the source.
	 *	This method is a shorthand for <code>setAttribute(GXL.FROMORDER, String.valueOf(order))</code>
	 *	@param order The integer to be used to order this edge's source tentacle relative to the source's other
	 *	incident tentacles.
	 *	@throws GXLValidationException If this action would result in an invalid GXL document.
	 */
	public void setSourceIncidenceOrder(int order) {
		setAttribute(GXL.FROMORDER, String.valueOf(order));
	}

	/** Returns whether this edge's target tentacle is ordered relative to the target. 
	 *	This method is a shorthand for <code>getAttribute(GXL.TOORDER) != null</code>.
	 *	@return Whether this edge's target tentacle is ordered relative to the target. 
	 */
	public boolean hasTargetIncidenceOrder() {
		return getAttribute(GXL.TOORDER) != null;
	}

	/** Returns the integer used to order this edge's target tentacle relative to the target's other
	 *	incident tentacles. Note that the value returned from this method is only valid if the method
	 *	<code>hasTargetIncidenceOrder()</code> returns true.
	 *	This method is mostly a shorthand for <code>getAttribute(GXL.TOORDER)</code> albeit the <code>String</code>
	 *	is converted to an integer first.
	 *	@return The integer used to order this edge's target tentacle relative to the target's other
	 *	incident tentacles.
	 */
	public int getTargetIncidenceOrder() {
		String order = getAttribute(GXL.TOORDER);
		return order != null ? new Integer(order).intValue() : -1;
	}

	/** Sets the integer used to order this edge's target tentacle relative to the target's other
	 *	incident tentacles. Note that the value of order must be unique to all the tentacles incident to the target.
	 *	This method is a shorthand for <code>setAttribute(GXL.TOORDER, String.valueOf(order))</code>
	 *	@param order The integer to be used to order this edge's target tentacle relative to the target's other
	 *	incident tentacles.
	 *	@throws GXLValidationException If this action would result in an invalid GXL document.
	 */
	public void setTargetIncidenceOrder(int order) {
		setAttribute(GXL.TOORDER, String.valueOf(order));
	}

	//
	// GXLLocalConnectionPort class
	//
	class GXLEdgeTentacle implements GXLLocalConnectionTentacle {
		/** The edge that this tentacle belongs to */
		GXLEdge edge;
		/** Whether this tentacle is the source or target of the edge. */
		boolean isSource;

		/** Creates a new GXLEdgeTentacle. */
		GXLEdgeTentacle(GXLEdge edge, String direction) {
			this.edge = edge;
			this.isSource = direction.equals(GXL.FROM);
		}

		/** Returns the LocalConnection that this tentacle connects from. */
		public GXLLocalConnection getLocalConnection() {
			return edge;
		}

		/** Returns the GraphElement that this tentacle connects to. */
		public GXLGraphElement getTarget() {
			return isSource ? edge.getSource() : edge.getTarget();
		}

		/** Returns the id of the GraphElement that this tentacle connects to. */
		public String getTargetID() {
			return isSource ? edge.getSourceID() : edge.getTargetID();
		}

		public void setTarget(GXLGraphElement graphElement) {
			if (isSource)
				edge.setSource(graphElement);
			else
				edge.setTarget(graphElement);
		}

		public void setTargetID(String id) {
			if (isSource)
				edge.setSourceID(id);
			else
				edge.setTargetID(id);
		}

		/** Whether this tentacle is ordered using the attributes fromorder/toorder/endorder. */
		public boolean hasTargetIncidenceOrder() {
			return isSource ? edge.hasSourceIncidenceOrder() : edge.hasTargetIncidenceOrder();
		}

		/** Returns the order of this tentacle or -1 if isOrdered() == false. */
		public int getTargetIncidenceOrder() {
			return isSource ? edge.getSourceIncidenceOrder() : edge.getTargetIncidenceOrder();
		}

		/** Sets the order of this tentacle. */
		public void setTargetIncidenceOrder(int order) {
			if (isSource)
				edge.setSourceIncidenceOrder(order);
			else
				edge.setTargetIncidenceOrder(order);
		}

		/** Returns whether this tentacle is dangling. */
		public boolean isDangling() {
			String id = isSource ? edge.getSourceID() : edge.getTargetID();
			return edge.getDocument() == null || !edge.getDocument().containsID(id);
		}

		/** The direction of this tentacle, either GXL.NONE, GXL.IN or GXL.OUT. */
		public String getDirection() {
			return edge.isDirected() ? isSource ? GXL.IN : GXL.OUT : GXL.NONE;
		}
	}
}
