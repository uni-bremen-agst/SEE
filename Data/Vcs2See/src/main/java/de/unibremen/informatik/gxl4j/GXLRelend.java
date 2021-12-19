/*
 * @(#)GXLRelend.java	0.9 2003-12-21
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

/** Represents the GXL <code>relend</code> element. 
 *	<p>
 *	The <code>relend</code> element represents a tentacle of a relation, defining the graph elements
 *	that are contained in the relation.
 *	<p>
 *	Observe that the <code>relend</code> element DOES NOT allow the <code>id</code> attribute even
 *	though it's a subclass of <code>GXLAttributedElement</code> which defines a method for setting
 *	the id. This is an ambiguity between the gxl graph schema (which states that all attributed elements
 *	contains id attributes) and the gxl dtd which contradicts this.
 *	<p>
 *	<b>Children</b><br>
 *	<code>GXLAttr</code> - Attributes describing this relation tentacle.<br>
 *	<p>
 *	<b>Attributes</b><br>
 *	<code>target</code> - Required id referencing the target graph element of this <code>relend</code>.<br>
 *	<code>role</code> - Optional string describing the function of this <code>relend</code>.<br>
 *	<code>direction</code> - Optional enumeration describing the direction of this tentacle.
 *	Possible values are <code>in</code> (INto the relation), <code>out</code> and <code>none</code>.
 *	Note that if the surrounding relation is directed, this attribute MUST be set to either <code>in</code> or <code>out</code>.
 *	<code>startorder</code> - Optional integer that describes an ordering of the incident <code>relend</code>s
 *	of the surrounding relation.<br>
 *	<code>endorder</code> - Optional integer that describes an ordering of the incident tentacles of the target element.<br>
 *
 *	@see GXLLocalConnectionTentacle
 *	@see GXLRel
 *	@see GXLGraphElement
 */
public class GXLRelend extends GXLAttributedElement implements GXLLocalConnectionTentacle {
	//
	// CONSTRUCTORS
	//

	/** Creates a new <code>GXLRelend</code> element with the specified target.
	 *	@param target The target of this tentacle.
	 */
	public GXLRelend(GXLGraphElement target) {
		this(target.getID());
	}

	/** Creates a new <code>GXLRelend</code> element with the specified target.
	 *	@param targetID The id of the target of this tentacle.
	 */
	public GXLRelend(String targetID) {
		super(GXL.RELEND);
		setAttribute(GXL.TARGET, targetID);
	}

	/** Creates a new GXLRelend element (used when reading a document). */
	GXLRelend(Element element) {
		super(GXL.RELEND, element);
		createChildren(element);
	}

	//
	// ATTRIBUTE ACCESSORS
	//

	/** Returns the <code>GXLGraphElement</code> that this <code>relend</code> connects to. 
	 *	Note that this method will return null if this tentacle is dangling.
	 *	@return The <code>GXLGraphElement</code> that this <code>relend</code> connects to. 
	 *	@see #getTargetID()
	 */
	public GXLGraphElement getTarget() {
		return gxlDocument != null ? (GXLGraphElement) gxlDocument.idMap.get(attributes.get(GXL.TARGET)) : null;
	}

	/** Sets the <code>GXLGraphElement</code> that this <code>relend</code> connects to. 
	 *	Note that only the graph elements id is stored, and that changing that id prior to
	 *	insertion into a <code>GXLDocument</code> will cause this tentacle to become dangling.
	 *	@param graphElement The <code>GXLGraphElement</code> that this tentacle should connect to. 
	 *	@throws GXLValidationException If this action would result in an invalid GXL document.
	 *	@see #setTargetID(String id)
	 */
	public void setTarget(GXLGraphElement target) {
		setAttribute(GXL.TARGET, (String) target.attributes.get(GXL.ID));
	}

	/** Returns the value of the <code>target</code> attribute.
	 *	This simply calls <code>getAttribute(GXL.TARGET)</code>.
	 *	@return The value of the <code>target</code> attribute.
	 *	@see #getAttribute(String)
	 */
	public String getTargetID() {
		return getAttribute(GXL.TARGET);
	}

	/** Sets the value of the <code>target</code> attribute.
	 *	This simply calls <code>setAttribute(GXL.TARGET, id)</code>.
	 *	@param id The id of the new target.
	 *	@throws GXLValidationException If this action would result in an invalid GXL document.
	 *	@see #setAttribute(String, String)
	 */
	public void setTargetID(String id) {
		setAttribute(GXL.TARGET, id);
	}

	/** Returns the value of the <code>role</code> attribute or <code>null</code> if the attribute isn't set.
	 *	This simply calls <code>getAttribute(GXL.ROLE)</code>.
	 *	@return The value of the <code>role</code> attribute.
	 *	@see #getAttribute(String)
	 */
	public String getRole() {
		return getAttribute(GXL.ROLE);
	}

	/** Sets the value of the <code>role</code> attribute.
	 *	This simply calls <code>setAttribute(GXL.ROLE, role)</code>.
	 *	@param role The new value.
	 *	@see #setAttribute(String, String)
	 */
	public void setRole(String role) {
		setAttribute(GXL.ROLE, role);
	}

	/** The direction of this tentacle, either GXL.NONE, GXL.IN or GXL.OUT. 
	 *	A convenience method to disclose the direction of this tentacle, either GXL.NONE (undirected),
	 *	GXL.IN (from the target element into the edge) or GXL.OUT (from the edge to the target element).
	 *	@return The direction of this tentacle, either GXL.NONE, GXL.IN or GXL.OUT. 
	 */
	public String getDirection() {
		String result = getAttribute(GXL.DIRECTION);
		if (result == null || (getLocalConnection() != null && !getLocalConnection().isDirected()))
			return GXL.NONE;
		else
			return result;
	}

	/** Sets the value of the <code>direction</code> attribute.
	 *	This simply calls <code>setAttribute(GXL.DIRECTION, direction)</code>.
	 *	@param direction The new value.
	 *	@throws GXLValidationException If this action would result in an invalid GXL document.
	 *	@see #setAttribute(String, String)
	 */
	public void setDirection(String direction) {
		setAttribute(GXL.DIRECTION, direction);
	}

	/** Whether this <code>relend</code> is ordered relative to the surrounding relations other <code>relend</code>s. 
	 *	@return Whether this <code>relend</code> is ordered relative to the surrounding relations other <code>relend</code>s. 
	 */
	public boolean hasRelIncidenceOrder() {
		return getAttribute(GXL.STARTORDER) != null;
	}

	/** Returns the order of this <code>relend</code>. 
	 *	Note that the value returned by this method is only valid
	 *	if <code>hasRelIncidenceOrder()</code> returns true. Also note that the integer returned
	 *	by this method is used relative to the other <code>relend</code>s incident to the relation and shouldn't
	 *	be treated as an absolute value. As an example, negative values are allowed.
	 *	@return The order of this <code>relend</code>.
	 */
	public int getRelIncidenceOrder() {
		String order = getAttribute(GXL.STARTORDER);
		return order != null ? new Integer(order).intValue() : -1;
	}

	/** Sets the order of this <code>relend</code>. 
	 *	Note that (1) that the order value must be unqiue troughtout all <code>relend</code>s of the relation and
	 *	(2) that the order is used relative the other <code>relend</code>s order values.
	 *	@param order The integer that is used relatively to define an order of the relations <code>relend</code>s.
	 *	@throws GXLValidationException If this action would result in an invalid GXL document.
	 */
	public void setRelIncidenceOrder(int order) {
		setAttribute(GXL.STARTORDER, String.valueOf(order));
	}

	//
	// GXLLocalConnectionTentacle
	//

	/** Returns the <code>GXLRel</code> that this <code>relend</code> connects from. 
	 *	@return The <code>GXLRel</code> that this <code>relend</code> connects from. 
	 */
	public GXLLocalConnection getLocalConnection() {
		return (GXLLocalConnection) parent;
	}

	/** Whether this tentacle is ordered relative to the target elements other incident tentacles. 
	 *	@return Whether this tentacle is ordered relative to the target elements other incident tentacles. 
	 */
	public boolean hasTargetIncidenceOrder() {
		return getAttribute(GXL.ENDORDER) != null;
	}

	/** Returns the order of this tentacle. Note that the value returned by this method is only valid
	 *	if <code>hasTargetIncidenceOrder()</code> returns true. Also note that the integer returned
	 *	by this method is used relative to the other tentacles incident to the target element and shouldn't
	 *	be treated as an absolute value. As an example, negative values are allowed.
	 *	@return The order of this tentacle.
	 */
	public int getTargetIncidenceOrder() {
		String order = getAttribute(GXL.ENDORDER);
		return order != null ? new Integer(order).intValue() : -1;
	}

	/** Sets the order of this tentacle. 
	 *	Note that (1) that the order value must be unqiue troughtout all incident tentacles of the target and
	 *	(2) that the order is used relatively the other tentacles order value.
	 *	@param order The integer that is used relatively to define an order of the targets incident tentacles.
	 *	@throws GXLValidationException If this action would result in an invalid GXL document.
	 */
	public void setTargetIncidenceOrder(int order) {
		setAttribute(GXL.ENDORDER, String.valueOf(order));
	}

	/** Returns whether this tentacle is dangling. 
	 *	A tentacle is considered dangling if the target cannot be found because
	 *	of (1) the <code>GXLDocument</code> that this tentacle is contained in doesn't contain
	 *	an element with the specified target id, or (2) if this tentacle isn't contained in
	 *	a <code>GXLDocument</code>.
	 *	@return Whether this tentacle is dangling.
	 */
	public boolean isDangling() {
		return gxlDocument == null || !gxlDocument.containsID(getTargetID());
	}
}
