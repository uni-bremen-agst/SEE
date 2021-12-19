/*
 * @(#)GXLLocalConnectionTentacle.java	0.9 2003-11-16
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

/** Non-GXL entity representing a tentacle between a <code>GXLLocalConnection</code> and a <code>GXLGraphElement</code>.
 *	The intention of this class is to simplify the handling of edges between graph elements. 
 *	<p>
 *	<b>Dangling tentacles</b>
 *	Although dangling edges is illegal in GXL, it is allowed temporarily by this package for simplicity.
 *	All tentacles are considered dangling prior to the insertion into a <code>GXLDocument</code> since
 *	only the ids of connected <code>GXLGraphElement</code>s are stored in the edges.
 *
 *	@see GXLLocalConnection
 *	@see GXLGraphElement
 */
public interface GXLLocalConnectionTentacle {
	/** Returns the <code>GXLLocalConnection</code> that this tentacle connects from. 
	 *	@return The <code>GXLLocalConnection</code> that this tentacle connects from. 
	 */
	public GXLLocalConnection getLocalConnection();

	/** Returns the <code>GXLGraphElement</code> that this tentacle connects to. 
	 *	Note that this method will return null if this tentacle is dangling.
	 *	@return The <code>GXLGraphElement</code> that this tentacle connects to. 
	 *	@see #getTargetID()
	 */
	public GXLGraphElement getTarget();

	/** Returns the id of the <code>GXLGraphElement</code> that this tentacle connects to. 
	 *	@return The id of the <code>GXLGraphElement</code> that this tentacle connects to. 
	 *	@see #getTarget()
	 */
	public String getTargetID();

	/** Sets the <code>GXLGraphElement</code> that this tentacle connects to. 
	 *	Note that only the graph elements id is stored, and that changing that id prior to
	 *	insertion into a <code>GXLDocument</code> will cause this tentacle to become dangling.
	 *	@param graphElement The <code>GXLGraphElement</code> that this tentacle should connect to. 
	 *	@throws GXLValidationException If this action would result in an invalid GXL document.
	 *	@see #setTargetID(String id)
	 */
	public void setTarget(GXLGraphElement graphElement);

	/** Sets the <code>GXLGraphElement</code> that this tentacle connects to. 
	 *	@param id The id of the <code>GXLGraphElement</code> that this tentacle should connect to. 
	 *	@throws GXLValidationException If this action would result in an invalid GXL document.
	 *	@see #setTarget(GXLGraphElement)
	 */
	public void setTargetID(String id);

	/** Whether this tentacle is ordered relative to the target elements other incident tentacles. 
	 *	@return Whether this tentacle is ordered relative to the target elements other incident tentacles. 
	 */
	public boolean hasTargetIncidenceOrder();

	/** Returns the order of this tentacle. Note that the value returned by this method is only valid
	 *	if <code>hasTargetIncidenceOrder()</code> returns true. Also note that the integer returned
	 *	by this method is used relative to the other tentacles incident to the target element and shouldn't
	 *	be treated as an absolute value. As an example, negative values are allowed.
	 *	@return The order of this tentacle.
	 */
	public int getTargetIncidenceOrder();

	/** Sets the order of this tentacle. 
	 *	Note that (1) that the order value must be unqiue troughtout all incident tentacles of the target and
	 *	(2) that the order is used relatively the other tentacles order value.
	 *	@param order The integer that is used relatively to define an order of the targets incident tentacles.
	 *	@throws GXLValidationException If this action would result in an invalid GXL document.
	 */
	public void setTargetIncidenceOrder(int order);

	/** Returns whether this tentacle is dangling. 
	 *	A tentacle is considered dangling if the target cannot be found because
	 *	of (1) the <code>GXLDocument</code> that this tentacle is contained in doesn't contain
	 *	an element with the specified target id, or (2) if this tentacle isn't contained in
	 *	a <code>GXLDocument</code>.
	 *	@return Whether this tentacle is dangling.
	 */
	public boolean isDangling();

	/** The direction of this tentacle, either GXL.NONE, GXL.IN or GXL.OUT. 
	 *	A convenience method to disclose the direction of this tentacle, either GXL.NONE (undirected),
	 *	GXL.IN (from the target element into the edge) or GXL.OUT (from the edge to the target element).
	 *	@return The direction of this tentacle, either GXL.NONE, GXL.IN or GXL.OUT. 
	 */
	public String getDirection();
}
